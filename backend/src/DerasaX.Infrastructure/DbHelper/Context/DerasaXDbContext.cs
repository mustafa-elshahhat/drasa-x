using DerasaX.Domain.Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DerasaX.Infrastructure.configuration;
using DerasaX.Domain.Entities.Base;
using DerasaX.Application.Services.Abstractions;

namespace DerasaX.Infrastructure.DbHelper.Context
{
    public class DerasaXDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ITenantContext? _tenant;

        public DerasaXDbContext(DbContextOptions<DerasaXDbContext> options, ITenantContext? tenant = null)
            : base(options)
        {
            _tenant = tenant;
        }

        // These are referenced by the global query-filter expressions below. Because
        // the filters reference *this DbContext instance*, EF re-evaluates them per
        // request instead of baking a single tenant into the cached model.
        private bool IsPlatformScope => _tenant?.IsPlatformScope ?? false;
        private string? CurrentTenantId => _tenant?.TenantId;

        public override int SaveChanges()
        {
            StampAudit();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, System.Threading.CancellationToken cancellationToken = default)
        {
            StampAudit();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Stamps audit metadata (UTC) for IAuditable entities. CreatedAt/CreatedBy are
        // set once on insert; UpdatedAt/UpdatedBy on every modification.
        private void StampAudit()
        {
            var now = DateTime.UtcNow;
            var actor = _tenant?.UserId;

            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= actor;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor ?? entry.Entity.UpdatedBy;
                    // Never let CreatedAt/CreatedBy be overwritten on update.
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyEnumToStringConversions();
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Table Per Type
            builder.Entity<Student>().ToTable("Student");
            builder.Entity<Teacher>().ToTable("Teacher");
            builder.Entity<Parent>().ToTable("Parent");
            builder.Entity<SystemAdmin>().ToTable("SystemAdmin");
            builder.Entity<SchoolAdmin>().ToTable("SchoolAdmin");

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                // Query filters can only be declared on the ROOT of an inheritance
                // hierarchy (e.g. ApplicationUser, not Student/Teacher). Skip derived
                // types so EF does not throw at model-build time.
                if (entityType.BaseType is not null)
                    continue;

                var hasTenant = typeof(IMustHaveTenant).IsAssignableFrom(clrType);
                var hasSoftDelete = typeof(ISoftDeletable).IsAssignableFrom(clrType);
                var isAuditable = typeof(IAuditable).IsAssignableFrom(clrType);

                if (hasTenant)
                {
                    // Tenant integrity: every tenant-owned row MUST carry a non-null
                    // tenant and that tenant MUST exist. RESTRICT prevents a tenant
                    // delete from silently erasing tenant data (deletion is an explicit
                    // lifecycle operation, never an accidental cascade).
                    builder.Entity(clrType).Property("TenantId").IsRequired().HasMaxLength(450);
                    builder.Entity(clrType).HasIndex("TenantId");
                    builder.Entity(clrType)
                        .HasOne(typeof(Tenant))
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Restrict);
                }

                if (isAuditable)
                {
                    // Optimistic concurrency via PostgreSQL's xmin system column. Mapped
                    // as a shadow uint property (Npgsql 9 replacement for the removed
                    // UseXminAsConcurrencyToken helper). xmin is a system column, so no
                    // DDL/column is generated for it.
                    builder.Entity(clrType).Property<uint>("xmin")
                        .HasColumnName("xmin")
                        .HasColumnType("xid")
                        .ValueGeneratedOnAddOrUpdate()
                        .IsConcurrencyToken();
                    builder.Entity(clrType).Property("CreatedBy").HasMaxLength(450);
                    builder.Entity(clrType).Property("UpdatedBy").HasMaxLength(450);
                }

                if (!hasTenant && !hasSoftDelete)
                    continue;

                // EF Core 9 supports a single effective query filter per entity, so the
                // tenant-isolation predicate and the soft-delete predicate MUST be
                // combined into ONE expression. Building them separately and calling
                // HasQueryFilter twice would silently overwrite the first — exactly the
                // Phase 3 defect this guards against. The combined predicate is:
                //
                //     (platform scope OR row.TenantId == current tenant)  AND  !row.IsDeleted
                //
                // Each branch is included only when the entity actually supports it, so
                // platform-scoped (non-tenant) and non-soft-deletable entities are
                // handled explicitly rather than by accident.
                var builderName =
                    hasTenant && hasSoftDelete ? nameof(BuildTenantAndSoftDeleteFilter)
                    : hasTenant ? nameof(BuildTenantOnlyFilter)
                    : nameof(BuildSoftDeleteOnlyFilter);

                var method = typeof(DerasaXDbContext)
                    .GetMethod(builderName, BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                var filter = (LambdaExpression)method.Invoke(this, null)!;
                builder.Entity(clrType).HasQueryFilter(filter);
            }
        }

        // Tenant-owned AND soft-deletable (every BaseEntity-derived domain entity).
        private LambdaExpression BuildTenantAndSoftDeleteFilter<TEntity>()
            where TEntity : class, IMustHaveTenant, ISoftDeletable
        {
            Expression<Func<TEntity, bool>> filter =
                e => (IsPlatformScope || e.TenantId == CurrentTenantId) && !e.IsDeleted;
            return filter;
        }

        // Tenant-owned but not soft-deletable.
        private LambdaExpression BuildTenantOnlyFilter<TEntity>()
            where TEntity : class, IMustHaveTenant
        {
            Expression<Func<TEntity, bool>> filter =
                e => IsPlatformScope || e.TenantId == CurrentTenantId;
            return filter;
        }

        // Soft-deletable but not tenant-owned (platform-scoped soft-deletable rows).
        private LambdaExpression BuildSoftDeleteOnlyFilter<TEntity>()
            where TEntity : class, ISoftDeletable
        {
            Expression<Func<TEntity, bool>> filter = e => !e.IsDeleted;
            return filter;
        }

        public DbSet<Announcement> announcements { get; set; }
        public DbSet<ApplicationUser> applicationUsers { get; set; }
        public DbSet<Student> students { get; set; }
        public DbSet<Teacher> teachers { get; set; }
        public DbSet<Parent> parents { get; set; }
        public DbSet<SystemAdmin> systemAdmins { get; set; }
        public DbSet<SchoolAdmin> SchoolAdmin { get; set; }
        public DbSet<Unit> units { get; set; }
        public DbSet<Grade> grades { get; set; }
        public DbSet<Lesson> lessons { get; set; }
        public DbSet<LessonMaterial> lessonMaterials { get; set; }
        public DbSet<LessonMaterialComment> lessonMaterialComments { get; set; }
        public DbSet<Notification> notifications { get; set; }
        public DbSet<NotificationPreference> notificationPreferences { get; set; }
        public DbSet<Post> posts { get; set; }
        public DbSet<Question> questions { get; set; }
        public DbSet<QuestionOption> questionOptions { get; set; }
        public DbSet<Quiz> quizzes { get; set; }
        public DbSet<QuizGeneration> quizGenerations { get; set; }
        public DbSet<QuizSubmission> quizSubmissions { get; set; }
        public DbSet<StudentInsight> studentInsights { get; set; }
        public DbSet<StudentLessonProgress> studentLessonProgresses { get; set; }
        public DbSet<StudentAttendanceRecord> studentAttendanceRecords { get; set; }
        public DbSet<Subject> subjects { get; set; }
        public DbSet<SubmissionAnswer> submissionAnswers { get; set; }
        public DbSet<SupportRequest> supportRequests { get; set; }
        public DbSet<Tenant> tenants { get; set; }

        // ---- Phase 4: Tenant & subscription ----
        public DbSet<SubscriptionPlanDefinition> subscriptionPlanDefinitions { get; set; }
        public DbSet<TenantSubscription> tenantSubscriptions { get; set; }
        public DbSet<SubscriptionRenewal> subscriptionRenewals { get; set; }
        public DbSet<TenantUsageCounter> tenantUsageCounters { get; set; }

        // ---- Phase 4: Academic structure & user relationships ----
        public DbSet<AcademicYear> academicYears { get; set; }
        public DbSet<Term> terms { get; set; }
        public DbSet<SchoolClass> schoolClasses { get; set; }
        public DbSet<Enrollment> enrollments { get; set; }
        public DbSet<ParentStudentRelationship> parentStudentRelationships { get; set; }
        public DbSet<TeacherSubjectAssignment> teacherSubjectAssignments { get; set; }
        public DbSet<TeacherClassAssignment> teacherClassAssignments { get; set; }

        // ---- Phase 4: Assessment ----
        public DbSet<Assignment> assignments { get; set; }
        public DbSet<AssignmentTarget> assignmentTargets { get; set; }
        public DbSet<AssignmentSubmission> assignmentSubmissions { get; set; }

        // ---- Phase 4: Progress & insights ----
        public DbSet<SubjectProgress> subjectProgresses { get; set; }
        public DbSet<StudentMetricHistory> studentMetricHistories { get; set; }
        public DbSet<StudentLearningProfile> studentLearningProfiles { get; set; }
        public DbSet<PainPoint> painPoints { get; set; }
        public DbSet<StudentRecommendation> studentRecommendations { get; set; }
        public DbSet<PredictionRecord> predictionRecords { get; set; }

        // ---- Phase 4: Communication ----
        public DbSet<Conversation> conversations { get; set; }
        public DbSet<ConversationParticipant> conversationParticipants { get; set; }
        public DbSet<Message> messages { get; set; }
        public DbSet<MessageAttachment> messageAttachments { get; set; }
        public DbSet<MessageReadReceipt> messageReadReceipts { get; set; }
        public DbSet<ParentRequest> parentRequests { get; set; }
        public DbSet<ParentRequestResponse> parentRequestResponses { get; set; }
        public DbSet<Suggestion> suggestions { get; set; }

        // ---- Phase 4: Engagement ----
        public DbSet<Community> communities { get; set; }
        public DbSet<CommunityMembership> communityMemberships { get; set; }
        public DbSet<PostComment> postComments { get; set; }
        public DbSet<PostReport> postReports { get; set; }
        public DbSet<Competition> competitions { get; set; }
        public DbSet<CompetitionEntry> competitionEntries { get; set; }
        public DbSet<CompetitionScore> competitionScores { get; set; }
        public DbSet<CompetitionSubmission> competitionSubmissions { get; set; }
        public DbSet<LeaderboardEntry> leaderboardEntries { get; set; }
        public DbSet<Badge> badges { get; set; }
        public DbSet<StudentBadge> studentBadges { get; set; }
        public DbSet<StudentStreak> studentStreaks { get; set; }
        public DbSet<OfficeHourSession> officeHourSessions { get; set; }
        public DbSet<OfficeHourBooking> officeHourBookings { get; set; }
        public DbSet<StudentPointTransaction> studentPointTransactions { get; set; }
        public DbSet<GamificationRule> gamificationRules { get; set; }

        // ---- Phase 15: Computer-vision attendance + engagement ----
        public DbSet<ClassroomVisionSession> classroomVisionSessions { get; set; }
        public DbSet<ClassroomVisionFrameAnalysis> classroomVisionFrameAnalyses { get; set; }
        public DbSet<StudentEngagementObservation> studentEngagementObservations { get; set; }
        public DbSet<AttendanceDetectionCandidate> attendanceDetectionCandidates { get; set; }
        public DbSet<ClassroomVisionSessionSummary> classroomVisionSessionSummaries { get; set; }
        public DbSet<StudentFaceEnrollment> studentFaceEnrollments { get; set; }

        // ---- Phase 4: Operations ----
        public DbSet<AuditLog> auditLogs { get; set; }
        public DbSet<FileRecord> fileRecords { get; set; }
        public DbSet<AiUsageRecord> aiUsageRecords { get; set; }
        public DbSet<SystemSetting> systemSettings { get; set; }
        public DbSet<TenantSetting> tenantSettings { get; set; }
        public DbSet<FeatureFlag> featureFlags { get; set; }
    }
}
