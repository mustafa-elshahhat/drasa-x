using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Account;
using DerasaX.Application.Services.Abstractions.Grade;
using DerasaX.Application.Services.Abstractions.Lesson;
using DerasaX.Application.Services.Abstractions.LessonMaterial;
using DerasaX.Application.Services.Abstractions.Quiz;
using DerasaX.Application.Services.Abstractions.Subject;
using DerasaX.Application.Services.Abstractions.Unit;
using DerasaX.Application.Services.Account;
using DerasaX.Application.Services.Grades;
using DerasaX.Application.Services.Image.FileServices;
using DerasaX.Application.Services.LessonMaterials;
using DerasaX.Application.Services.Lessons;
using DerasaX.Application.Services.Quizzes;
using DerasaX.Application.Services.ServiceAuth;
using DerasaX.Application.Services.Subjects;
using DerasaX.Application.Services.Subjects.Mapping;
using DerasaX.Application.Services.Units;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<ServiceAuthSettings>(configuration.GetSection(ServiceAuthSettings.SectionName));
            services.Configure<AiServiceSettings>(configuration.GetSection(AiServiceSettings.SectionName));

            services.RegisterApplicationServices(configuration);
            services.AddJwtAuthentication(configuration);
            services.AddDerasaXAuthorization();

            services.AddHttpContextAccessor();
            services.AddScoped<ITenantContext, HttpTenantContext>();
            services.AddScoped<IAiServiceTokenProvider, AiServiceTokenProvider>();

            services.AddAutoMapperServices();
            services.AddScoped<SubjectPictureUrlResolver>();

            return services;
        }

        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAccountServices, AccountServices>();
            services.AddScoped<ISubjectServices, SubjectServices>();
            services.AddScoped<IUnitServices, UnitServices>();
            services.AddScoped<ILessonServices, LessonServices>();
            services.AddScoped<IGradeServices, GradeServices>();
            services.AddScoped<ILessonMaterialServicess, LessonMaterialServices>();
            services.AddScoped<IQuizServices, QuizServices>();
            // Phase 19 — legacy image migration: subject/profile image uploads now flow through the
            // durable Phase 16 storage layer (DurableImageFileService) instead of raw wwwroot. The legacy
            // FileService is kept (concrete) only for backward-compatible deletes of pre-existing files.
            services.AddScoped<FileService>();
            services.AddScoped<IFileService, DerasaX.Application.Services.Storage.DurableImageFileService>();

            // Phase 5 — cross-cutting audit writer + academic administration services.
            services.AddScoped<DerasaX.Application.Services.Abstractions.Audit.IAuditWriter,
                DerasaX.Application.Services.Audit.AuditWriter>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Academic.IAcademicYearService,
                DerasaX.Application.Services.Academic.AcademicYearService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Academic.ITermService,
                DerasaX.Application.Services.Academic.TermService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Academic.ISchoolClassService,
                DerasaX.Application.Services.Academic.SchoolClassService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Academic.IEnrollmentService,
                DerasaX.Application.Services.Academic.EnrollmentService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Academic.ITeacherAssignmentService,
                DerasaX.Application.Services.Academic.TeacherAssignmentService>();

            // Phase 5 Increment 3 — assessment lifecycle (authoring, assignment, attempts, grading).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Assessment.IQuizAuthoringService,
                DerasaX.Application.Services.Assessment.QuizAuthoringService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Assessment.IQuizAssignmentService,
                DerasaX.Application.Services.Assessment.QuizAssignmentService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Assessment.IQuizAttemptService,
                DerasaX.Application.Services.Assessment.QuizAttemptService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Assessment.IQuizGradingService,
                DerasaX.Application.Services.Assessment.QuizGradingService>();

            // Phase 5 Increment 4 — relationship authorization + progress/insights/performance.
            services.AddScoped<DerasaX.Application.Services.Abstractions.Authorization.IStudentAccessAuthorizer,
                DerasaX.Application.Services.Authorization.StudentAccessAuthorizer>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Progress.IStudentProgressService,
                DerasaX.Application.Services.Progress.StudentProgressService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Progress.IStudentAttendanceService,
                DerasaX.Application.Services.Progress.StudentAttendanceService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Progress.IPerformanceService,
                DerasaX.Application.Services.Progress.PerformanceService>();

            // Phase 9 — Teacher Portal summary + assignment-scoped reads.
            services.AddScoped<DerasaX.Application.Services.Abstractions.TeacherPortal.ITeacherPortalService,
                DerasaX.Application.Services.TeacherPortal.TeacherPortalService>();

            // Phase 10 — Parent Portal summary + relationship-scoped reads.
            services.AddScoped<DerasaX.Application.Services.Abstractions.ParentPortal.IParentPortalService,
                DerasaX.Application.Services.ParentPortal.ParentPortalService>();

            // Phase 11 — School Admin Portal: aggregate dashboard + parent↔student relationship and
            // teacher↔class assignment management (the admin contracts that did not exist before).
            services.AddScoped<DerasaX.Application.Services.Abstractions.SchoolAdminPortal.ISchoolAdminPortalService,
                DerasaX.Application.Services.SchoolAdminPortal.SchoolAdminPortalService>();

            // Phase 12 — System Admin (platform) Portal: aggregate dashboard + platform usage/AI/storage
            // roll-ups + cross-tenant support inbox + durable platform announcements + create-initial-
            // school-admin + operational status + SAFE non-destructive tenant data export/deletion request
            // (the platform contracts that did not exist before; tenant lifecycle/plans/audit/settings reuse Phase 5).
            services.AddScoped<DerasaX.Application.Services.Abstractions.SystemAdminPortal.ISystemAdminPortalService,
                DerasaX.Application.Services.SystemAdminPortal.SystemAdminPortalService>();

            // Phase 5 Increment 5 — communication (conversations, parent requests, announcements, suggestions).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Communication.IConversationService,
                DerasaX.Application.Services.Communication.ConversationService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Communication.IParentRequestService,
                DerasaX.Application.Services.Communication.ParentRequestService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Communication.IAnnouncementService,
                DerasaX.Application.Services.Communication.AnnouncementService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Communication.ISuggestionService,
                DerasaX.Application.Services.Communication.SuggestionService>();

            // Phase 13 — per-user notification preferences (honoured by NotificationStaging routing).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Notification.INotificationPreferenceService,
                DerasaX.Application.Services.Notification.NotificationPreferenceService>();

            // Phase 5 Increment 6 — engagement (communities, competitions, badges, office hours).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Engagement.ICommunityService,
                DerasaX.Application.Services.Engagement.CommunityService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Engagement.ICompetitionService,
                DerasaX.Application.Services.Engagement.CompetitionService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Engagement.IBadgeService,
                DerasaX.Application.Services.Engagement.BadgeService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Engagement.IOfficeHourService,
                DerasaX.Application.Services.Engagement.OfficeHourService>();
            // Phase 14 — ledger-based gamification (points, rules, leaderboard).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Engagement.IGamificationService,
                DerasaX.Application.Services.Engagement.GamificationService>();

            // Phase 5 Increment 7 — tenant & operations.
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.ITenantAdminService,
                DerasaX.Application.Services.Operations.TenantAdminService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.ITenantSelfService,
                DerasaX.Application.Services.Operations.TenantSelfService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.ISupportService,
                DerasaX.Application.Services.Operations.SupportService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.IAuditQueryService,
                DerasaX.Application.Services.Operations.AuditQueryService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.IAiUsageService,
                DerasaX.Application.Services.Operations.AiUsageService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.IPlanLimitEnforcer,
                DerasaX.Application.Services.Operations.PlanLimitEnforcer>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.ISettingsService,
                DerasaX.Application.Services.Operations.SettingsService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.IFileMetadataService,
                DerasaX.Application.Services.Operations.FileMetadataService>();
            // Phase 16 — durable, tenant-isolated file storage orchestration (providers live in Infrastructure).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Storage.IFileStorageService,
                DerasaX.Application.Services.Storage.FileStorageService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Operations.IReportService,
                DerasaX.Application.Services.Operations.ReportService>();

            // Centralized login-code/temporary-password generation shared by every provisioning
            // and reset-credential flow (SystemAdmin onboarding + SchoolAdmin user management).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Provisioning.ICredentialProvisioningService,
                DerasaX.Application.Services.Provisioning.CredentialProvisioningService>();

            // Phase 5 closure — SchoolAdmin user/credential provisioning.
            services.AddScoped<DerasaX.Application.Services.Abstractions.Provisioning.IUserProvisioningService,
                DerasaX.Application.Services.Provisioning.UserProvisioningService>();

            // Phase 5 closure — lesson-resource comments.
            services.AddScoped<DerasaX.Application.Services.Abstractions.Engagement.IResourceCommentService,
                DerasaX.Application.Services.Engagement.ResourceCommentService>();

            // Phase 5 closure — homework / general-assignment lifecycle.
            services.AddScoped<DerasaX.Application.Services.Abstractions.Assessment.IHomeworkService,
                DerasaX.Application.Services.Assessment.HomeworkService>();

            // Phase 6 — AI tutor + curriculum ingestion orchestration (the IAiRagClient
            // typed HttpClient is registered in the API project, which has Microsoft.Extensions.Http).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Ai.ITutorService,
                DerasaX.Application.Services.Ai.TutorService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Ai.IAiDocumentService,
                DerasaX.Application.Services.Ai.AiDocumentService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Ai.IQuizDraftService,
                DerasaX.Application.Services.Ai.QuizDraftService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Ai.IPredictionService,
                DerasaX.Application.Services.Ai.PredictionService>();
            services.AddScoped<DerasaX.Application.Services.Abstractions.Ai.IAnalysisService,
                DerasaX.Application.Services.Ai.AnalysisService>();

            // Phase 15 — computer-vision attendance + engagement (backend-mediated AI calls).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Vision.IClassroomVisionService,
                DerasaX.Application.Services.Vision.ClassroomVisionService>();
            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var secretKey = configuration["SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                    throw new InvalidOperationException("JWT secret key (SecretKey) is missing in configuration.");

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = securityKey,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
                };

                options.Events = new JwtBearerEvents
                {
                    // SignalR sends the token as a query parameter for WebSocket connections.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    },
                    // Tenant gate: tenant-role tokens MUST carry a tenantId; a platform-scoped
                    // SystemAdmin token may omit it (Phase 2 AUTHENTICATION_FLOW §6 / D-30).
                    OnTokenValidated = context =>
                    {
                        var principal = context.Principal;
                        var tenantId = principal?.FindFirst("tenantId")?.Value;
                        var isSystemAdmin =
                            principal?.IsInRole(Roles.SystemAdmin) == true ||
                            principal?.Claims.Any(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == Roles.SystemAdmin) == true;

                        if (string.IsNullOrEmpty(tenantId) && !isSystemAdmin)
                            context.Fail("TenantId claim is required for tenant-scoped users.");

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>().CreateLogger("JWT");
                        // Log the failure category only — never the token value.
                        logger.LogWarning("JWT authentication failed: {Error}", context.Exception.GetType().Name);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>Registers the Phase 2 role policies and a secure-by-default fallback policy.</summary>
        public static IServiceCollection AddDerasaXAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.StudentOnly, p => p.RequireRole(Roles.Student));
                options.AddPolicy(Policies.TeacherOnly, p => p.RequireRole(Roles.Teacher));
                options.AddPolicy(Policies.ParentOnly, p => p.RequireRole(Roles.Parent));
                options.AddPolicy(Policies.SchoolAdminOnly, p => p.RequireRole(Roles.SchoolAdmin));
                options.AddPolicy(Policies.SystemAdminOnly, p => p.RequireRole(Roles.SystemAdmin));
                options.AddPolicy(Policies.TeacherOrSchoolAdmin, p => p.RequireRole(Roles.Teacher, Roles.SchoolAdmin));
                options.AddPolicy(Policies.TenantStaff, p => p.RequireRole(Roles.Teacher, Roles.SchoolAdmin));
                options.AddPolicy(Policies.TenantMember, p => p.RequireAuthenticatedUser()
                    .RequireAssertion(ctx => ctx.User.HasClaim(c => c.Type == "tenantId" && !string.IsNullOrEmpty(c.Value))));

                // Self-account: authenticated principal only. SystemAdmin (no tenant)
                // can change its own password / revoke its own session; tenant-domain
                // routes remain protected by their role/tenant policies.
                options.AddPolicy(Policies.SelfAccount, p => p.RequireAuthenticatedUser());

                // Secure default: any endpoint without an explicit decision still requires
                // an authenticated user. Endpoints opt out only via [AllowAnonymous].
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }

        private static void AddAutoMapperServices(this IServiceCollection services)
        {
            var applicationsAssembly = Assembly.GetExecutingAssembly();
            services.AddAutoMapper(applicationsAssembly);
        }
    }
}
