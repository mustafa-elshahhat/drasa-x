using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Dto.SystemAdminDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.SystemAdminPortal;
using DerasaX.Application.Services.Operations;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Interfaces.RepositoriesInterfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.SystemAdminPortal
{
    /// <summary>
    /// Phase 12 — System Admin (platform) Portal service. Adds ONLY the genuinely-missing platform
    /// contracts (aggregate dashboard, platform usage/AI/storage roll-ups, cross-tenant support
    /// inbox, durable platform announcements, create-initial-school-admin, operational status, and
    /// the SAFE non-destructive tenant data export/deletion request). Tenant lifecycle, plans,
    /// platform audit, system settings and feature flags REUSE the existing Phase 5 §14 surface.
    ///
    /// Every read is platform-scope (the DbContext query filter returns all tenants because the caller
    /// is <c>IsPlatformScope</c>). Every sensitive mutation is audited: tenant-attributed actions stage
    /// a tenant AuditLog row via <c>tenantOverride</c> (the platform actor id is preserved in the audit
    /// metadata); platform-owned changes (announcements stored in the SystemSetting store) are audited
    /// by the entity's own IAuditable CreatedBy/UpdatedBy stamping, exactly like platform settings/flags.
    /// No metric is fabricated — an empty platform returns zeros, and deferred infrastructure is reported
    /// honestly rather than faked.
    /// </summary>
    public class SystemAdminPortalService : OperationsServiceBase, ISystemAdminPortalService
    {
        private const string AnnouncementsKey = "platform.announcements";

        private readonly IPlatformRepository<Tenant> _tenants;
        private readonly IPlatformRepository<SubscriptionPlanDefinition> _plans;
        private readonly IPlatformRepository<SystemSetting> _systemSettings;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IAuditQueryService _auditQuery;
        private readonly IPlanLimitEnforcer _limits;

        public SystemAdminPortalService(
            IUnitOfWork unitOfWork,
            ITenantContext tenant,
            IAuditWriter audit,
            IPlatformRepository<Tenant> tenants,
            IPlatformRepository<SubscriptionPlanDefinition> plans,
            IPlatformRepository<SystemSetting> systemSettings,
            UserManager<ApplicationUser> users,
            IAuditQueryService auditQuery,
            IPlanLimitEnforcer limits)
            : base(unitOfWork, tenant, audit)
        {
            _tenants = tenants;
            _plans = plans;
            _systemSettings = systemSettings;
            _users = users;
            _auditQuery = auditQuery;
            _limits = limits;
        }

        // ---------------------------------------------------------------
        // Dashboard — real platform-wide aggregate.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<PlatformDashboardDto>> DashboardAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();

            var tenants = await _tenants.ListAsync(t => true, ct);
            var plans = await _plans.ListAsync(p => true, ct);
            var subs = await Repo<TenantSubscription>().GetAllWithSpecAsync(All<TenantSubscription>());
            var ai = await Repo<AiUsageRecord>().GetAllWithSpecAsync(All<AiUsageRecord>());
            var support = await Repo<SupportRequest>().GetAllWithSpecAsync(All<SupportRequest>());

            var recent = await _auditQuery.QueryAsync(new AuditParameters { PageNumber = 1, PageSize = 10 }, platformScope: true, ct);

            var dto = new PlatformDashboardDto
            {
                TenantsTotal = tenants.Count,
                TenantsActive = tenants.Count(t => t.Status == TenantStatus.Active),
                TenantsSuspended = tenants.Count(t => t.Status == TenantStatus.Suspended),
                TenantsArchived = tenants.Count(t => t.Status == TenantStatus.Archived),

                Students = await _users.Users.CountAsync(u => !u.IsDeleted && u is Student, ct),
                Teachers = await _users.Users.CountAsync(u => !u.IsDeleted && u is Teacher, ct),
                Parents = await _users.Users.CountAsync(u => !u.IsDeleted && u is Parent, ct),
                SchoolAdmins = await _users.Users.CountAsync(u => !u.IsDeleted && u is SchoolAdmin, ct),
                SystemAdmins = await _users.Users.CountAsync(u => !u.IsDeleted && u is SystemAdmin, ct),

                PlansTotal = plans.Count,
                PlansActive = plans.Count(p => p.IsActive),
                SubscriptionsTotal = subs.Count(),
                SubscriptionsActive = subs.Count(s => s.Status == SubscriptionStatus.Active),
                SubscriptionsTrial = subs.Count(s => s.Status == SubscriptionStatus.Trial),

                AiUsageRecords = ai.Count(),
                AiTotalTokens = ai.Sum(a => (long)(a.TotalTokens ?? 0)),
                AiTotalCost = ai.Sum(a => a.Cost ?? 0m),

                SupportTotal = support.Count(),
                SupportOpen = support.Count(s => s.Status == RequestStatus.Pending),

                RecentAuditEvents = recent.TotalCount,
                RecentActivity = (recent.Data ?? new List<AuditLogDto>()).ToList(),

                GeneratedAt = DateTime.UtcNow
            };

            return Ok(dto, 200, "Platform summary retrieved.");
        }

        // ---------------------------------------------------------------
        // Platform usage roll-up (per-tenant rows).
        // ---------------------------------------------------------------
        public async Task<ApiResponse<PlatformUsageDto>> PlatformUsageAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();

            var tenants = (await _tenants.ListAsync(t => true, ct)).OrderBy(t => t.Id).ToList();
            var plans = await _plans.ListAsync(p => true, ct);
            var subs = (await Repo<TenantSubscription>().GetAllWithSpecAsync(All<TenantSubscription>())).ToList();
            var ai = (await Repo<AiUsageRecord>().GetAllWithSpecAsync(All<AiUsageRecord>())).ToList();

            var studentCounts = await _users.Users.Where(u => !u.IsDeleted && u is Student)
                .GroupBy(u => u.TenantId).Select(g => new { TenantId = g.Key, Count = g.Count() }).ToListAsync(ct);
            var teacherCounts = await _users.Users.Where(u => !u.IsDeleted && u is Teacher)
                .GroupBy(u => u.TenantId).Select(g => new { TenantId = g.Key, Count = g.Count() }).ToListAsync(ct);

            int Students(string id) => studentCounts.FirstOrDefault(x => x.TenantId == id)?.Count ?? 0;
            int Teachers(string id) => teacherCounts.FirstOrDefault(x => x.TenantId == id)?.Count ?? 0;

            var rows = new List<PlatformUsageRowDto>();
            foreach (var t in tenants)
            {
                var latest = subs.Where(s => s.TenantId == t.Id).OrderByDescending(s => s.StartsAt).FirstOrDefault();
                var plan = latest is null ? null : plans.FirstOrDefault(p => p.Id == latest.PlanDefinitionId);
                var students = Students(t.Id);
                rows.Add(new PlatformUsageRowDto
                {
                    TenantId = t.Id,
                    TenantName = t.Name,
                    Status = t.Status,
                    StudentsCount = students,
                    TeachersCount = Teachers(t.Id),
                    AiGenerationsUsed = ai.Count(a => a.TenantId == t.Id),
                    MaxStudents = plan?.MaxStudents,
                    MaxAiGenerationsPerMonth = plan?.MaxAiGenerationsPerMonth,
                    OverStudentLimit = plan?.MaxStudents is int max && students > max
                });
            }

            var dto = new PlatformUsageDto
            {
                TenantsCount = rows.Count,
                TotalStudents = rows.Sum(r => r.StudentsCount),
                TotalTeachers = rows.Sum(r => r.TeachersCount),
                TotalAiGenerations = rows.Sum(r => r.AiGenerationsUsed),
                Tenants = rows,
                GeneratedAt = DateTime.UtcNow
            };
            return Ok(dto, 200, "Platform usage retrieved.");
        }

        // ---------------------------------------------------------------
        // Platform AI usage roll-up.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<PlatformAiUsageDto>> PlatformAiUsageAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();

            var ai = (await Repo<AiUsageRecord>().GetAllWithSpecAsync(All<AiUsageRecord>())).ToList();
            var tenants = await _tenants.ListAsync(t => true, ct);
            var nameById = tenants.ToDictionary(t => t.Id, t => t.Name);

            var tenantRows = ai.GroupBy(a => a.TenantId)
                .Select(g => new TenantAiUsageRowDto
                {
                    TenantId = g.Key ?? string.Empty,
                    TenantName = g.Key != null && nameById.TryGetValue(g.Key, out var n) ? n : string.Empty,
                    Records = g.Count(),
                    TotalTokens = g.Sum(a => (long)(a.TotalTokens ?? 0)),
                    TotalCost = g.Sum(a => a.Cost ?? 0m)
                })
                .OrderByDescending(r => r.TotalTokens)
                .ToList();

            var dto = new PlatformAiUsageDto
            {
                Records = ai.Count,
                TotalTokens = ai.Sum(a => (long)(a.TotalTokens ?? 0)),
                TotalCost = ai.Sum(a => a.Cost ?? 0m),
                Tenants = tenantRows,
                GeneratedAt = DateTime.UtcNow
            };
            return Ok(dto, 200, "Platform AI usage retrieved.");
        }

        // ---------------------------------------------------------------
        // Platform storage posture — HONEST: byte accounting is not implemented
        // (Phase 16 owns file storage/delivery). Reports the plan MaxStorageMb
        // ceilings that DO exist plus declared-at-upload file sizes, clearly labelled.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<PlatformStorageDto>> PlatformStorageAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();

            var tenants = (await _tenants.ListAsync(t => true, ct)).OrderBy(t => t.Id).ToList();
            var plans = await _plans.ListAsync(p => true, ct);
            var subs = (await Repo<TenantSubscription>().GetAllWithSpecAsync(All<TenantSubscription>())).ToList();
            var files = (await Repo<FileRecord>().GetAllWithSpecAsync(All<FileRecord>())).ToList();

            var rows = tenants.Select(t =>
            {
                var latest = subs.Where(s => s.TenantId == t.Id).OrderByDescending(s => s.StartsAt).FirstOrDefault();
                var plan = latest is null ? null : plans.FirstOrDefault(p => p.Id == latest.PlanDefinitionId);
                var tenantFiles = files.Where(f => f.TenantId == t.Id).ToList();
                return new TenantStorageRowDto
                {
                    TenantId = t.Id,
                    TenantName = t.Name,
                    MaxStorageMb = plan?.MaxStorageMb,
                    FileRecords = tenantFiles.Count,
                    DeclaredBytes = tenantFiles.Sum(f => f.SizeBytes)
                };
            }).ToList();

            var dto = new PlatformStorageDto
            {
                ByteAccountingImplemented = false,
                Note = "Per-tenant measured byte accounting is not implemented yet (object/file storage and " +
                       "delivery is the Phase 16 deliverable). Plan MaxStorageMb ceilings and declared-at-upload " +
                       "file sizes are shown; live measured usage is not yet available.",
                Tenants = rows,
                GeneratedAt = DateTime.UtcNow
            };
            return Ok(dto, 200, "Platform storage posture retrieved.");
        }

        // ---------------------------------------------------------------
        // Cross-tenant subscriptions list.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<IEnumerable<PlatformSubscriptionRowDto>>> ListSubscriptionsAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();

            var subs = (await Repo<TenantSubscription>().GetAllWithSpecAsync(All<TenantSubscription>())).ToList();
            var tenants = await _tenants.ListAsync(t => true, ct);
            var plans = await _plans.ListAsync(p => true, ct);
            var tenantName = tenants.ToDictionary(t => t.Id, t => t.Name);

            var rows = subs.OrderBy(s => s.TenantId).ThenByDescending(s => s.StartsAt).Select(s =>
            {
                var plan = plans.FirstOrDefault(p => p.Id == s.PlanDefinitionId);
                return new PlatformSubscriptionRowDto
                {
                    TenantId = s.TenantId ?? string.Empty,
                    TenantName = s.TenantId != null && tenantName.TryGetValue(s.TenantId, out var n) ? n : string.Empty,
                    SubscriptionId = s.Id,
                    PlanDefinitionId = s.PlanDefinitionId,
                    PlanCode = plan?.Code ?? string.Empty,
                    PlanName = plan?.Name ?? string.Empty,
                    Status = s.Status,
                    IsTrial = s.IsTrial,
                    StartsAt = s.StartsAt,
                    ExpiresAt = s.ExpiresAt
                };
            }).ToList();

            return Ok<IEnumerable<PlatformSubscriptionRowDto>>(rows, 200, "Subscriptions retrieved.");
        }

        // ---------------------------------------------------------------
        // Cross-tenant support inbox.
        // ---------------------------------------------------------------
        public async Task<PaginationResponse<IEnumerable<SupportRequestDto>>> ListSupportTicketsAsync(SystemSupportParameters p, CancellationToken ct = default)
        {
            RequireSystemAdmin();

            var status = p.Status;
            var tenantId = string.IsNullOrWhiteSpace(p.TenantId) ? null : p.TenantId;
            System.Linq.Expressions.Expression<Func<SupportRequest, bool>> criteria =
                r => (!status.HasValue || r.Status == status.Value)
                     && (tenantId == null || r.TenantId == tenantId);

            var repo = Repo<SupportRequest>();
            var total = await repo.CountAsync(new CriteriaSpecification<SupportRequest, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<SupportRequest, string>(criteria, r => r.CreatedAt, p.PageNumber, p.PageSize, descending: true));

            return new PaginationResponse<IEnumerable<SupportRequestDto>>(items.Select(MapSupport).ToList(), total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Support tickets retrieved." };
        }

        public async Task<ApiResponse<SupportRequestDto>> RespondSupportTicketAsync(string id, RespondSupportDto dto, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            if (string.IsNullOrWhiteSpace(dto.ResponseMessage)) throw new BadRequestException("ResponseMessage is required.");

            var request = await Repo<SupportRequest>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SupportRequest, string>(r => r.Id == id))
                ?? throw new NotFoundException("Support ticket not found.");

            request.ResponseMessage = dto.ResponseMessage;
            request.Status = dto.Status;
            request.RespondedAt = DateTime.UtcNow;
            Repo<SupportRequest>().Update(request);

            // Audit attributed to the ticket's tenant; platform actor preserved in metadata by the writer.
            await Audit.StageAsync(AuditActionType.Update, nameof(SupportRequest), request.Id,
                $"{{\"action\":\"platform-respond\",\"status\":\"{dto.Status}\"}}", ct, tenantOverride: request.TenantId);

            // Notify the requesting tenant user (preference-aware staging + honest delivery state).
            if (!string.IsNullOrEmpty(request.TenantId))
            {
                await NotificationStaging.StageAsync(UnitOfWork, request.TenantId, request.UserId,
                    "Support request updated", "A platform administrator responded to your support request.",
                    NotificationCategory.General, NotificationType.System, actorUserId: Tenant.UserId,
                    metadataJson: $"{{\"supportRequestId\":\"{request.Id}\"}}", ct: ct);
            }

            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapSupport(request), 200, "Support ticket updated.");
        }

        // ---------------------------------------------------------------
        // Platform announcements — durable, audited via the SystemSetting store.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<IEnumerable<PlatformAnnouncementDto>>> ListAnnouncementsAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();
            var list = await LoadAnnouncementsAsync(ct);
            return Ok<IEnumerable<PlatformAnnouncementDto>>(list.OrderByDescending(a => a.CreatedAt).ToList(), 200, "Platform announcements retrieved.");
        }

        public async Task<ApiResponse<PlatformAnnouncementDto>> CreateAnnouncementAsync(CreatePlatformAnnouncementDto dto, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new BadRequestException("Title is required.");
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new BadRequestException("Body is required.");

            var list = await LoadAnnouncementsAsync(ct);
            var item = new PlatformAnnouncementDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title.Trim(),
                Body = dto.Body.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Tenant.UserId
            };
            list.Add(item);

            var json = JsonSerializer.Serialize(list);
            var existing = await _systemSettings.FirstOrDefaultAsync(s => s.Key == AnnouncementsKey, ct);
            if (existing is null)
            {
                existing = new SystemSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = AnnouncementsKey,
                    Value = json,
                    ValueType = SettingValueType.Json,
                    Description = "Platform-wide announcements published by SystemAdmin.",
                    IsSecret = false
                };
                await _systemSettings.AddAsync(existing, ct);
            }
            else
            {
                existing.Value = json;
                existing.ValueType = SettingValueType.Json;
                _systemSettings.Update(existing);
            }
            // Phase 13 note: platform announcements remain a durable platform-store record (Phase 12).
            // We intentionally do NOT fan out an in-app notification to every SchoolAdmin here — the shared
            // local DB has a large accumulated SchoolAdmin set, and a cross-tenant fan-out on every publish
            // would flood inboxes and is not required for the gate. Tenant-level announcement targeting
            // (AnnouncementService) is the implemented, observable notification path. This is documented in
            // PHASE13_NOTIFICATION_ROUTING_MATRIX.md / PHASE13_REMAINING_GAPS.md.
            //
            // Platform-owned: publication is audited by the SystemSetting's IAuditable CreatedBy/UpdatedBy/
            // UpdatedAt stamping (set automatically to the acting SystemAdmin), consistent with platform
            // settings & feature flags.
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(item, 201, "Platform announcement published.");
        }

        // ---------------------------------------------------------------
        // Onboarding: create the INITIAL SchoolAdmin for a target tenant.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<CreatedSchoolAdminDto>> CreateSchoolAdminAsync(string tenantId, CreateSchoolAdminDto dto, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            if (string.IsNullOrWhiteSpace(dto.FullName)) throw new BadRequestException("FullName is required.");
            if (string.IsNullOrWhiteSpace(dto.LoginCode)) throw new BadRequestException("LoginCode is required.");

            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
                ?? throw new NotFoundException("Tenant not found.");

            if (await _users.Users.AnyAsync(u => u.LoginCode == dto.LoginCode, ct))
                throw new ConflictException("An account with this login code already exists.");

            await _limits.EnsureCanAddUserAsync(tenant.Id, Roles.SchoolAdmin, ct);

            var user = new SchoolAdmin
            {
                UserName = dto.LoginCode,
                FullName = dto.FullName.Trim(),
                LoginCode = dto.LoginCode.Trim(),
                TenantId = tenant.Id,
                IsDeleted = false
            };

            var tempPassword = GenerateTemporaryPassword();
            var result = await _users.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
            await _users.AddToRoleAsync(user, Roles.SchoolAdmin);

            // Audit attributed to the target tenant; NO secret material is recorded.
            await Audit.StageAsync(AuditActionType.Create, nameof(ApplicationUser), user.Id,
                $"{{\"action\":\"create-school-admin\",\"role\":\"{Roles.SchoolAdmin}\"}}", ct, tenantOverride: tenant.Id);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(new CreatedSchoolAdminDto
            {
                UserId = user.Id,
                TenantId = tenant.Id,
                LoginCode = user.LoginCode,
                Role = Roles.SchoolAdmin,
                TemporaryPassword = tempPassword
            }, 201, "School administrator created.");
        }

        // ---------------------------------------------------------------
        // Operational posture — real DB readiness + honest deferred states.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<OperationalStatusDto>> OperationalStatusAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();

            bool dbReachable;
            string dbNote;
            try
            {
                _ = await _tenants.CountAsync(t => true, ct);
                dbReachable = true;
                dbNote = "PostgreSQL reachable (live read succeeded).";
            }
            catch (Exception ex)
            {
                dbReachable = false;
                dbNote = "Database read failed: " + ex.GetType().Name;
            }

            var dto = new OperationalStatusDto
            {
                Health = new HealthStatusDto
                {
                    Api = "up",
                    DatabaseReachable = dbReachable,
                    DatabaseNote = dbNote,
                    CheckedAt = DateTime.UtcNow
                },
                ErrorMonitoring = new ServicePostureDto
                {
                    Configured = false,
                    Status = "logs-and-correlation",
                    Note = "Phase 19: errors are captured in structured application logs, each tagged with a request " +
                           "correlation id (X-Correlation-Id) and surfaced via /health. A centralized error-tracking/" +
                           "alerting pipeline (e.g. Sentry/Application Insights) is a production/staging follow-up."
                },
                Backups = new ServicePostureDto
                {
                    // In-PRODUCT automated/scheduled backup orchestration is still not configured
                    // (an infrastructure concern); Phase 19 adds operational scripts only.
                    Configured = false,
                    Status = "script-based",
                    Note = "Phase 19: PostgreSQL backup/restore is provided via operational scripts " +
                           "(scripts/backup-local-db.ps1, scripts/restore-local-db.ps1) and documented in " +
                           "PHASE19_BACKUP_RESTORE.md. Scheduled/managed in-product orchestration (PITR, offsite) " +
                           "remains an infrastructure concern for production."
                },
                SecurityEvents = new ServicePostureDto
                {
                    Configured = true,
                    Status = "audit-derived",
                    Note = "Security events are derived from the real platform audit trail (e.g. Login actions). A " +
                           "dedicated security-event/SIEM pipeline is deferred to a later phase."
                },
                GeneratedAt = DateTime.UtcNow
            };
            return Ok(dto, 200, "Operational status retrieved.");
        }

        // ---------------------------------------------------------------
        // SAFE, NON-DESTRUCTIVE tenant data workflow.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<TenantDataRequestDto>> ExportTenantDataAsync(string tenantId, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
                ?? throw new NotFoundException("Tenant not found.");

            var preview = await BuildTenantPreviewAsync(tenant.Id, ct);

            await Audit.StageAsync(AuditActionType.Export, nameof(Tenant), tenant.Id,
                "{\"action\":\"data-export-preview\"}", ct, tenantOverride: tenant.Id);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(new TenantDataRequestDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                RequestType = "export",
                Status = "preview-generated",
                Destructive = false,
                Preview = preview,
                Note = "This is a non-destructive export preview showing the entity counts the export would contain. " +
                       "The action has been recorded in the audit trail. Actual file generation/delivery is a later-phase deliverable.",
                RequestedAt = DateTime.UtcNow
            }, 200, "Tenant data export preview generated.");
        }

        public async Task<ApiResponse<TenantDataRequestDto>> RequestTenantDeletionAsync(string tenantId, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
                ?? throw new NotFoundException("Tenant not found.");

            var preview = await BuildTenantPreviewAsync(tenant.Id, ct);

            // IMPORTANT: this NEVER deletes tenant data. It only records an audited request for manual
            // platform approval (execution rule 10 — no destructive deletion in this phase).
            await Audit.StageAsync(AuditActionType.Export, nameof(Tenant), tenant.Id,
                "{\"action\":\"data-deletion-request\",\"executed\":false}", ct, tenantOverride: tenant.Id);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(new TenantDataRequestDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                RequestType = "deletion-request",
                Status = "request-recorded",
                Destructive = false,
                Preview = preview,
                Note = "A deletion request has been recorded in the audit trail for manual platform approval. " +
                       "NO tenant data has been deleted. Irreversible deletion is intentionally out of scope for this phase. " +
                       "To take a tenant offline now, use Suspend (reversible).",
                RequestedAt = DateTime.UtcNow
            }, 200, "Tenant deletion request recorded (non-destructive).");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private void RequireSystemAdmin()
        {
            if (!IsSystemAdmin) throw new ForbiddenException("Only a platform administrator may perform this operation.");
        }

        private IGenericRepository<T, string> Repo<T>() where T : Domain.Entities.Base.BaseEntity<string>
            => UnitOfWork.Repository<T, string>();

        private static CriteriaSpecification<T, string> All<T>() where T : Domain.Entities.Base.BaseEntity<string>
            => new(_ => true);

        private async Task<List<PlatformAnnouncementDto>> LoadAnnouncementsAsync(CancellationToken ct)
        {
            var setting = await _systemSettings.FirstOrDefaultAsync(s => s.Key == AnnouncementsKey, ct);
            if (setting is null || string.IsNullOrWhiteSpace(setting.Value)) return new List<PlatformAnnouncementDto>();
            try
            {
                return JsonSerializer.Deserialize<List<PlatformAnnouncementDto>>(setting.Value) ?? new List<PlatformAnnouncementDto>();
            }
            catch
            {
                return new List<PlatformAnnouncementDto>();
            }
        }

        private async Task<Dictionary<string, int>> BuildTenantPreviewAsync(string tenantId, CancellationToken ct)
        {
            return new Dictionary<string, int>
            {
                ["students"] = await _users.Users.CountAsync(u => u.TenantId == tenantId && u is Student, ct),
                ["teachers"] = await _users.Users.CountAsync(u => u.TenantId == tenantId && u is Teacher, ct),
                ["parents"] = await _users.Users.CountAsync(u => u.TenantId == tenantId && u is Parent, ct),
                ["schoolAdmins"] = await _users.Users.CountAsync(u => u.TenantId == tenantId && u is SchoolAdmin, ct),
                ["announcements"] = await Repo<Announcement>().CountAsync(new CriteriaSpecification<Announcement, string>(a => a.TenantId == tenantId)),
                ["supportRequests"] = await Repo<SupportRequest>().CountAsync(new CriteriaSpecification<SupportRequest, string>(s => s.TenantId == tenantId)),
                ["aiUsageRecords"] = await Repo<AiUsageRecord>().CountAsync(new CriteriaSpecification<AiUsageRecord, string>(a => a.TenantId == tenantId)),
                ["fileRecords"] = await Repo<FileRecord>().CountAsync(new CriteriaSpecification<FileRecord, string>(f => f.TenantId == tenantId)),
                ["auditLogs"] = await Repo<AuditLog>().CountAsync(new CriteriaSpecification<AuditLog, string>(a => a.TenantId == tenantId)),
            };
        }

        private static SupportRequestDto MapSupport(SupportRequest r) => new()
        {
            Id = r.Id, TenantId = r.TenantId, UserId = r.UserId, Type = r.Type, Status = r.Status,
            Message = r.Message, ResponseMessage = r.ResponseMessage, CreatedAt = r.CreatedAt, RespondedAt = r.RespondedAt
        };

        /// <summary>
        /// Strong one-time password (upper+lower+digit, length 14) via CSPRNG; mirrors
        /// <c>UserProvisioningService.GenerateTemporaryPassword</c>. Never logged or persisted in clear text.
        /// </summary>
        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string all = upper + lower + digits;
            var chars = new char[14];
            chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            for (var i = 3; i < chars.Length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}
