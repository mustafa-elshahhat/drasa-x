using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// Performance prediction orchestration. Tenant/user from trusted claims; access
    /// via <see cref="IStudentAccessAuthorizer"/>; features derived from authoritative
    /// records (StudentMetricHistory attendance + study-time, Student identity gender,
    /// StudentLearningProfile demographics); inference via the internal contract;
    /// immutable historical persistence; AiUsage on success and failure.
    /// </summary>
    public class PredictionService : OperationsServiceBase, IPredictionService
    {
        public const string FeatureSchemaVersion = "perf-v1";
        // Prediction runs a local scikit-learn model in school-ai-rag, not an LLM provider.
        private const string Provider = "local-model";

        private static readonly HashSet<string> SchoolTypes = new() { "public", "private" };
        private static readonly HashSet<string> YesNo = new() { "yes", "no" };
        private static readonly HashSet<string> TravelTimes = new() { "<15 min", "15-30 min", "30-60 min", ">60 min" };
        private static readonly HashSet<string> StudyMethods = new() { "textbook", "notes", "online videos", "group study", "mixed" };

        private readonly IAiRagClient _ai;
        private readonly IAiUsageService _usage;
        private readonly IStudentAccessAuthorizer _access;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<PredictionService> _logger;

        public PredictionService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IAiRagClient ai, IAiUsageService usage, IStudentAccessAuthorizer access,
            UserManager<ApplicationUser> users, ILogger<PredictionService> logger) : base(uow, tenant, audit)
        {
            _ai = ai;
            _usage = usage;
            _access = access;
            _users = users;
            _logger = logger;
        }

        public async Task<PredictionResultDto> GenerateAsync(GeneratePredictionDto request, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(request.StudentId))
                throw new BadRequestException("StudentId is required.");

            // Access: self / assigned teacher / linked parent / same-tenant admin; cross-tenant → 404.
            await _access.EnsureCanAccessStudentAsync(request.StudentId, ct);

            var (features, dataFrom, dataTo) = await BuildFeaturesAsync(request.StudentId, ct);

            var correlationId = Guid.NewGuid().ToString("N");
            var aiReq = new AiPredictionRequest
            {
                CorrelationId = correlationId,
                StudentRef = request.StudentId,
                PredictionType = "performance",
                FeatureSchemaVersion = FeatureSchemaVersion,
                DataRangeFrom = dataFrom?.ToString("o"),
                DataRangeTo = dataTo?.ToString("o"),
                Features = features,
            };

            AiPredictionResponse ai;
            try
            {
                ai = await _ai.PredictAsync(aiReq, tenantId, Tenant.UserId, ct);
            }
            catch (AiServiceException ex)
            {
                await TryRecordUsageAsync(model: null, failed: true, correlationId, ex.Category, ct);
                throw;
            }

            ValidatePrediction(ai);

            // Append an immutable historical record (never overwrites prior predictions).
            var record = new PredictionRecord
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                StudentId = request.StudentId,
                Kind = PredictionKind.Performance,
                PredictedScore = (decimal)ai.Score,
                Level = MapLevel(ai.Level),
                ConfidenceScore = (decimal)(ai.Confidence ?? 0d),
                ModelName = ai.ModelName,
                ModelVersion = ai.ModelVersion,
                InputSnapshotJson = JsonSerializer.Serialize(new
                {
                    featureSchemaVersion = ai.FeatureSchemaVersion,
                    dataRangeFrom = aiReq.DataRangeFrom,
                    dataRangeTo = aiReq.DataRangeTo,
                    features = aiReq.Features,
                }),
                PredictedAt = DateTime.UtcNow,
            };
            await UnitOfWork.Repository<PredictionRecord, string>().AddAsync(record);
            await Audit.StageAsync(AuditActionType.Create, nameof(PredictionRecord), record.Id,
                $"{{\"kind\":\"Performance\",\"modelVersion\":\"{ai.ModelVersion}\"}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            await TryRecordUsageAsync(model: ai.ModelVersion, failed: false, correlationId, "prediction", ct);

            return new PredictionResultDto
            {
                PredictionId = record.Id,
                StudentId = request.StudentId,
                PredictionType = ai.PredictionType,
                Score = record.PredictedScore,
                Level = ai.Level,
                RiskBand = ai.RiskBand,
                Confidence = ai.Confidence.HasValue ? (decimal)ai.Confidence.Value : (decimal?)null,
                ModelName = ai.ModelName,
                ModelVersion = ai.ModelVersion,
                FeatureSchemaVersion = ai.FeatureSchemaVersion,
                DataRangeFrom = ai.DataRangeFrom,
                DataRangeTo = ai.DataRangeTo,
                GeneratedAt = record.PredictedAt,
                Limitations = ai.Limitations ?? new List<string>(),
                CorrelationId = correlationId,
            };
        }

        public async Task<IReadOnlyList<PredictionHistoryItemDto>> GetHistoryAsync(string studentId, CancellationToken ct = default)
        {
            RequireTenant();
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var records = await UnitOfWork.Repository<PredictionRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<PredictionRecord, string>(r => r.StudentId == studentId));
            return records
                .OrderByDescending(r => r.PredictedAt)
                .Select(r => new PredictionHistoryItemDto
                {
                    Id = r.Id, Kind = r.Kind.ToString(), PredictedScore = r.PredictedScore,
                    Level = r.Level.ToString(), ConfidenceScore = r.ConfidenceScore,
                    ModelName = r.ModelName, ModelVersion = r.ModelVersion, PredictedAt = r.PredictedAt,
                }).ToList();
        }

        public async Task UpsertLearningProfileAsync(string studentId, SetLearningProfileDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!(IsSchoolAdmin || Tenant.Role == Roles.Teacher))
                throw new ForbiddenException("Only a teacher or school administrator may set a learning profile.");
            await _access.EnsureCanAccessStudentAsync(studentId, ct);

            if (dto.AgeYears < 3 || dto.AgeYears > 100) throw new BadRequestException("AgeYears must be between 3 and 100.");
            ValidateCategory("schoolType", dto.SchoolType, SchoolTypes);
            ValidateCategory("internetAccess", dto.InternetAccess, YesNo);
            ValidateCategory("travelTime", dto.TravelTime, TravelTimes);
            ValidateCategory("extraActivities", dto.ExtraActivities, YesNo);
            ValidateCategory("studyMethod", dto.StudyMethod, StudyMethods);

            var existing = await UnitOfWork.Repository<StudentLearningProfile, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<StudentLearningProfile, string>(p => p.StudentId == studentId));
            if (existing is null)
            {
                await UnitOfWork.Repository<StudentLearningProfile, string>().AddAsync(new StudentLearningProfile
                {
                    Id = Guid.NewGuid().ToString(), TenantId = tenantId, StudentId = studentId,
                    AgeYears = dto.AgeYears, SchoolType = dto.SchoolType, InternetAccess = dto.InternetAccess,
                    TravelTime = dto.TravelTime, ExtraActivities = dto.ExtraActivities, StudyMethod = dto.StudyMethod,
                    FeatureSchemaVersion = FeatureSchemaVersion,
                });
            }
            else
            {
                existing.AgeYears = dto.AgeYears; existing.SchoolType = dto.SchoolType;
                existing.InternetAccess = dto.InternetAccess; existing.TravelTime = dto.TravelTime;
                existing.ExtraActivities = dto.ExtraActivities; existing.StudyMethod = dto.StudyMethod;
                existing.FeatureSchemaVersion = FeatureSchemaVersion;
                // tracked entity — SaveChanges persists the update
            }
            await UnitOfWork.SaveChangesAsync(ct);
        }

        private async Task<(AiPredictionFeatures, DateTime?, DateTime?)> BuildFeaturesAsync(string studentId, CancellationToken ct)
        {
            var metrics = (await UnitOfWork.Repository<StudentMetricHistory, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentMetricHistory, string>(m => m.StudentId == studentId))).ToList();

            var attendance = metrics.Where(m => m.MetricType == ProgressMetricType.Attendance)
                .OrderByDescending(m => m.MeasuredAt).FirstOrDefault();
            var studyTime = metrics.Where(m => m.MetricType == ProgressMetricType.StudyTime)
                .OrderByDescending(m => m.MeasuredAt).FirstOrDefault();
            var profile = await UnitOfWork.Repository<StudentLearningProfile, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<StudentLearningProfile, string>(p => p.StudentId == studentId));

            if (attendance is null || studyTime is null || profile is null)
                throw new BadRequestException("Insufficient data: an attendance metric, a study-time metric, and a learning profile are required.");

            var user = await _users.FindByIdAsync(studentId)
                ?? throw new NotFoundException("Student not found.");
            if (user.Gender is null)
                throw new BadRequestException("Insufficient data: student gender is not recorded.");

            var usedTimes = new[] { attendance.MeasuredAt, studyTime.MeasuredAt };
            var features = new AiPredictionFeatures
            {
                Age = profile.AgeYears,
                StudyHours = (double)studyTime.Value,
                AttendancePercentage = (double)attendance.Value,
                Gender = user.Gender == Gender.Male ? "male" : "female",
                SchoolType = profile.SchoolType,
                InternetAccess = profile.InternetAccess,
                TravelTime = profile.TravelTime,
                ExtraActivities = profile.ExtraActivities,
                StudyMethod = profile.StudyMethod,
            };
            return (features, usedTimes.Min(), usedTimes.Max());
        }

        private static void ValidatePrediction(AiPredictionResponse ai)
        {
            if (ai is null || string.IsNullOrWhiteSpace(ai.Level))
                throw new AiServiceException("bad_response", "The AI prediction was invalid.");
            if (double.IsNaN(ai.Score) || double.IsInfinity(ai.Score))
                throw new AiServiceException("bad_response", "The AI prediction score was invalid.");
            if (ai.Confidence is { } c && (c < 0 || c > 1))
                throw new AiServiceException("bad_response", "The AI prediction confidence was out of range.");
            if (string.IsNullOrWhiteSpace(ai.ModelVersion) || string.IsNullOrWhiteSpace(ai.FeatureSchemaVersion))
                throw new AiServiceException("bad_response", "The AI prediction was missing model/schema metadata.");
        }

        private async Task TryRecordUsageAsync(string? model, bool failed, string correlationId, string category, CancellationToken ct)
        {
            try
            {
                await _usage.RecordInternalAsync(new RecordAiUsageDto
                {
                    Kind = AiUsageKind.Prediction, Provider = Provider, Model = model,
                    Failed = failed, CorrelationId = correlationId,
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI prediction usage recording failed. correlationId={CorrelationId} category={Category}", correlationId, category);
            }
        }

        private static void ValidateCategory(string name, string value, HashSet<string> allowed)
        {
            if (!allowed.Contains(value))
                throw new BadRequestException($"Invalid {name} value.");
        }

        private static PerformanceLevel MapLevel(string level) => level switch
        {
            "Weak" => PerformanceLevel.AtRisk,
            "Strong" => PerformanceLevel.OnTrack,
            _ => PerformanceLevel.NeedsSupport,
        };
    }
}
