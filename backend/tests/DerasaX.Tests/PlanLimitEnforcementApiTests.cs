using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Subscription-plan limits are enforced at the write-time service boundary, not just
/// displayed. Each test uses a dedicated throwaway tenant (never the shared "tenant-1"
/// fixture) so assigning a low-limit plan cannot interfere with the rest of the parallel
/// suite. User-provisioning is exercised through the real HTTP pipeline; the remaining
/// checks call the real DI-registered <see cref="IPlanLimitEnforcer"/> directly against
/// the real database (no fakes), which avoids depending on a live AI/file-storage backend
/// while still exercising the production code path.
/// </summary>
public class PlanLimitEnforcementApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public PlanLimitEnforcementApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, string? errorCode);
    private sealed record IdRow(string id);
    private sealed record Cred(string userId, string loginCode, string temporaryPassword);

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private async Task<string> InsertPlanAsync(int? maxStudents = null, int? maxClasses = null, int? maxSubjects = null,
        int? maxStorageMb = null, int? maxAiGenerationsPerMonth = null, int? maxAiTokensPerMonth = null)
    {
        var planId = Phase4Db.NewId("plan");
        await using var db = Phase4Db.Platform(_factory);
        db.subscriptionPlanDefinitions.Add(new SubscriptionPlanDefinition
        {
            Id = planId, Code = Phase4Db.NewId("LIMPLAN")[..16], Name = "Limit Test Plan", Tier = SubscriptionPlan.Free,
            BillingPeriod = BillingPeriod.Monthly, Price = 0, Currency = "USD", IsActive = true,
            MaxStudents = maxStudents, MaxClasses = maxClasses, MaxSubjects = maxSubjects, MaxStorageMb = maxStorageMb,
            MaxAiGenerationsPerMonth = maxAiGenerationsPerMonth, MaxAiTokensPerMonth = maxAiTokensPerMonth,
        });
        await db.SaveChangesAsync();
        return planId;
    }

    /// <summary>
    /// Resolves the real DI-registered <see cref="IPlanLimitEnforcer"/> from a fresh scope, with a
    /// fake platform-scope (SystemAdmin, no tenant claim) HttpContext installed on that scope's
    /// <see cref="IHttpContextAccessor"/>. This is required because the underlying repositories
    /// apply a global EF Core query filter driven by <c>ITenantContext</c> (itself backed by the
    /// ambient HttpContext); outside of a real HTTP request there is no HttpContext at all, which
    /// would make every tenant-scoped query see zero rows. Setting a fake platform-scope principal
    /// makes the filter behave exactly as it does for a real authenticated SystemAdmin request.
    /// <see cref="IHttpContextAccessor"/>'s default implementation stores the context in an
    /// AsyncLocal, so this is isolated per test and safe under parallel execution.
    /// </summary>
    private static IPlanLimitEnforcer ResolveEnforcer(IServiceScope scope)
    {
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var identity = new ClaimsIdentity(new[] { new Claim("role", "SystemAdmin"), new Claim(ClaimTypes.Role, "SystemAdmin") }, "TestAuth");
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return scope.ServiceProvider.GetRequiredService<IPlanLimitEnforcer>();
    }

    private async Task<string> SeedGradeAsync(string tenantId)
    {
        var gradeId = Phase4Db.NewId("grade");
        await using var db = Phase4Db.AsTenant(_factory, tenantId);
        db.grades.Add(new Grade { Id = gradeId, TenantId = tenantId, Name = "Grade X" });
        await db.SaveChangesAsync();
        return gradeId;
    }

    private async Task AssignPlanAsync(string tenantId, string planId)
    {
        await using var db = Phase4Db.AsTenant(_factory, tenantId);
        db.tenantSubscriptions.Add(new TenantSubscription
        {
            Id = Phase4Db.NewId("sub"), TenantId = tenantId, PlanDefinitionId = planId,
            Status = SubscriptionStatus.Active, StartsAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task CleanupTenantAsync(string tenantId, string planId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"TenantId\" = {0})", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"schoolClasses\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subjects\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"aiUsageRecords\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"academicYears\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"grades\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenantSubscriptions\" WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenants\" WHERE \"Id\" = {0}", tenantId);
        if (planId != null) await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionPlanDefinitions\" WHERE \"Id\" = {0}", planId);
    }

    [Fact]
    public async Task Student_provisioning_is_blocked_once_the_plan_student_limit_is_reached()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var tenantId = Phase4Db.NewId("t");
        var planId = await InsertPlanAsync(maxStudents: 1);
        try
        {
            Assert.Equal(HttpStatusCode.Created, (await sys.PostAsJsonAsync("/api/v1/tenants", new { id = tenantId, name = "Limit Co", type = 0 })).StatusCode);
            await AssignPlanAsync(tenantId, planId);
            var gradeId = await SeedGradeAsync(tenantId);

            var adminCreate = await sys.PostAsJsonAsync($"/api/v1/system-admin/tenants/{tenantId}/school-admins",
                new { fullName = "Limit Admin", loginCode = Phase4Db.NewId("LADM")[..16] });
            Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);
            var cred = (await Read<Cred>(adminCreate))!.data!;

            var admin = TestClient.NewClient(_factory);
            var (loginStatus, loginBody) = await TestClient.LoginAsync(admin, cred.loginCode, cred.temporaryPassword);
            Assert.Equal((int)HttpStatusCode.OK, loginStatus);
            admin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.token);

            // Under the limit: the first student succeeds.
            var first = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Student One", loginCode = Phase4Db.NewId("STU1")[..16], role = "Student", gradeId });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            // At the limit: the second student is rejected with the specific plan-limit error code.
            var second = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Student Two", loginCode = Phase4Db.NewId("STU2")[..16], role = "Student", gradeId });
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            var raw = await second.Content.ReadAsStringAsync();
            Assert.Contains("PLAN_LIMIT_EXCEEDED", raw);
        }
        finally { await CleanupTenantAsync(tenantId, planId); }
    }

    [Fact]
    public async Task Tenant_with_no_subscription_is_never_limited()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var tenantId = Phase4Db.NewId("t");
        try
        {
            Assert.Equal(HttpStatusCode.Created, (await sys.PostAsJsonAsync("/api/v1/tenants", new { id = tenantId, name = "No Plan Co", type = 0 })).StatusCode);
            // No plan is assigned to this tenant at all.
            var gradeId = await SeedGradeAsync(tenantId);

            var adminCreate = await sys.PostAsJsonAsync($"/api/v1/system-admin/tenants/{tenantId}/school-admins",
                new { fullName = "Unlimited Admin", loginCode = Phase4Db.NewId("UADM")[..16] });
            Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);
            var cred = (await Read<Cred>(adminCreate))!.data!;

            var admin = TestClient.NewClient(_factory);
            var (loginStatus, loginBody) = await TestClient.LoginAsync(admin, cred.loginCode, cred.temporaryPassword);
            Assert.Equal((int)HttpStatusCode.OK, loginStatus);
            admin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.token);

            // Several students in a row all succeed — an unassigned tenant is unlimited.
            for (var i = 0; i < 3; i++)
            {
                var resp = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = $"Student {i}", loginCode = Phase4Db.NewId($"NOLIM{i}")[..18], role = "Student", gradeId });
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            }
        }
        finally { await CleanupTenantAsync(tenantId, null!); }
    }

    [Fact]
    public async Task Class_and_subject_creation_are_blocked_at_the_plan_limit()
    {
        var tenantId = Phase4Db.NewId("t");
        var planId = await InsertPlanAsync(maxClasses: 1, maxSubjects: 1);
        var gradeId = Phase4Db.NewId("grade");
        var yearId = Phase4Db.NewId("year");
        try
        {
            await Phase4Db.EnsureTenantAsync(_factory, tenantId);
            await AssignPlanAsync(tenantId, planId);

            await using (var db = Phase4Db.AsTenant(_factory, tenantId))
            {
                db.grades.Add(new Grade { Id = gradeId, TenantId = tenantId, Name = "Grade X" });
                db.academicYears.Add(new AcademicYear { Id = yearId, TenantId = tenantId, Name = "2099/2100", Code = "2099", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddYears(1), IsCurrent = true });
                db.schoolClasses.Add(new SchoolClass { Id = Phase4Db.NewId("class"), TenantId = tenantId, Name = "Class A", Code = "A", GradeId = gradeId, AcademicYearId = yearId });
                db.subjects.Add(new Subject { Id = Phase4Db.NewId("subj"), TenantId = tenantId, Name = "Subject A", GradeId = gradeId });
                await db.SaveChangesAsync();
            }

            using var scope = _factory.Services.CreateScope();
            var limits = ResolveEnforcer(scope);

            await Assert.ThrowsAsync<PlanLimitExceededException>(() => limits.EnsureCanAddClassAsync(tenantId));
            await Assert.ThrowsAsync<PlanLimitExceededException>(() => limits.EnsureCanAddSubjectAsync(tenantId));
        }
        finally { await CleanupTenantAsync(tenantId, planId); }
    }

    [Fact]
    public async Task Upload_is_blocked_once_the_plan_storage_limit_would_be_exceeded()
    {
        var tenantId = Phase4Db.NewId("t");
        var planId = await InsertPlanAsync(maxStorageMb: 1); // 1 MB ceiling
        try
        {
            await Phase4Db.EnsureTenantAsync(_factory, tenantId);
            await AssignPlanAsync(tenantId, planId);

            await using (var db = Phase4Db.AsTenant(_factory, tenantId))
            {
                db.fileRecords.Add(new FileRecord
                {
                    Id = Phase4Db.NewId("file"), TenantId = tenantId, FileName = "existing.pdf", ContentType = "application/pdf",
                    SizeBytes = 900_000, Type = FileRecordType.Other, StorageKey = "k1", ChecksumSha256 = "x",
                });
                await db.SaveChangesAsync();
            }

            using var scope = _factory.Services.CreateScope();
            var limits = ResolveEnforcer(scope);

            // 900 KB already used + 200 KB more exceeds the 1 MB ceiling.
            await Assert.ThrowsAsync<PlanLimitExceededException>(() => limits.EnsureCanUploadBytesAsync(tenantId, 200_000));
            // A tiny additional upload that stays under the ceiling is allowed.
            await limits.EnsureCanUploadBytesAsync(tenantId, 10_000);
        }
        finally { await CleanupTenantAsync(tenantId, planId); }
    }

    [Fact]
    public async Task Ai_monthly_request_and_token_quotas_are_enforced()
    {
        var tenantId = Phase4Db.NewId("t");
        var planId = await InsertPlanAsync(maxAiGenerationsPerMonth: 1, maxAiTokensPerMonth: 1000);
        try
        {
            await Phase4Db.EnsureTenantAsync(_factory, tenantId);
            await AssignPlanAsync(tenantId, planId);

            await using (var db = Phase4Db.AsTenant(_factory, tenantId))
            {
                db.aiUsageRecords.Add(new AiUsageRecord
                {
                    Id = Phase4Db.NewId("ai"), TenantId = tenantId, Kind = AiUsageKind.Chat, Provider = "groq",
                    TotalTokens = 1500, UsedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
            }

            using var scope = _factory.Services.CreateScope();
            var limits = ResolveEnforcer(scope);

            // One record already this month meets the MaxAiGenerationsPerMonth=1 ceiling.
            await Assert.ThrowsAsync<PlanLimitExceededException>(() => limits.EnsureWithinAiMonthlyQuotaAsync(tenantId));
        }
        finally { await CleanupTenantAsync(tenantId, planId); }
    }
}
