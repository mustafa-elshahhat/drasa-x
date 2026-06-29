using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DerasaX.Tests;

/// <summary>
/// Creates throwaway, uniquely-named users directly via Identity so that the
/// Phase 3 closure tests (self password change, lockout) never mutate the shared
/// seed fixtures and remain idempotent against the live local database.
/// </summary>
public static class TestUsers
{
    public record CreatedUser(string Id, string LoginCode, string Password);

    public static async Task<CreatedUser> CreateSystemAdminAsync(IntegrationFactory factory, string? password = null)
    {
        var pwd = password ?? "Local@Dev123";
        var loginCode = "SYST-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("SystemAdmin"))
            await roleManager.CreateAsync(new IdentityRole("SystemAdmin"));

        var user = new SystemAdmin
        {
            UserName = loginCode.ToLowerInvariant(),
            FullName = "Test Platform Admin",
            LoginCode = loginCode,
            TenantId = null
        };

        var result = await userManager.CreateAsync(user, pwd);
        if (!result.Succeeded)
            throw new Exception("Failed to create SystemAdmin test user: " + string.Join(",", result.Errors));
        await userManager.AddToRoleAsync(user, "SystemAdmin");

        return new CreatedUser(user.Id, loginCode, pwd);
    }

    /// <summary>Creates a tenant student used for lockout testing (lockout is identical across roles).</summary>
    public static async Task<CreatedUser> CreateLockoutStudentAsync(IntegrationFactory factory, string? password = null)
    {
        var pwd = password ?? "Local@Dev123";
        var loginCode = "LOCK-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Student"))
            await roleManager.CreateAsync(new IdentityRole("Student"));

        // tenant-1 / G7-ID are part of the standing Phase 3 fixture set.
        var user = new Student
        {
            UserName = loginCode.ToLowerInvariant(),
            FullName = "Lockout Student",
            LoginCode = loginCode,
            TenantId = "tenant-1",
            GradeId = "G7-ID"
        };

        var result = await userManager.CreateAsync(user, pwd);
        if (!result.Succeeded)
            throw new Exception("Failed to create lockout test user: " + string.Join(",", result.Errors));
        await userManager.AddToRoleAsync(user, "Student");

        return new CreatedUser(user.Id, loginCode, pwd);
    }

    /// <summary>Creates a throwaway tenant-1 SchoolAdmin (used to isolate AI usage assertions per test).</summary>
    public static async Task<CreatedUser> CreateSchoolAdminAsync(IntegrationFactory factory, string? password = null)
    {
        var pwd = password ?? "Local@Dev123";
        var loginCode = "SADM-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("SchoolAdmin"))
            await roleManager.CreateAsync(new IdentityRole("SchoolAdmin"));

        var user = new SchoolAdmin
        {
            UserName = loginCode.ToLowerInvariant(),
            FullName = "Test School Admin",
            LoginCode = loginCode,
            TenantId = "tenant-1"
        };

        var result = await userManager.CreateAsync(user, pwd);
        if (!result.Succeeded)
            throw new Exception("Failed to create SchoolAdmin test user: " + string.Join(",", result.Errors));
        await userManager.AddToRoleAsync(user, "SchoolAdmin");

        return new CreatedUser(user.Id, loginCode, pwd);
    }

    /// <summary>Resets lockout state deterministically (simulates the lockout window elapsing).</summary>
    public static async Task ClearLockoutAsync(IntegrationFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new Exception("User not found: " + userId);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1));
        await userManager.ResetAccessFailedCountAsync(user);
    }
}
