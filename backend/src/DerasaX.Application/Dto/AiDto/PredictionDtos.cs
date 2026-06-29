using System;
using System.Collections.Generic;

namespace DerasaX.Application.Dto.AiDto
{
    // ---------------------------------------------------------------------
    // Caller-facing. The browser sends ONLY the target student id + type;
    // the backend derives all features from authoritative records.
    // ---------------------------------------------------------------------
    public class GeneratePredictionDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string PredictionType { get; set; } = "performance";
    }

    public class SetLearningProfileDto
    {
        public int AgeYears { get; set; }
        public string SchoolType { get; set; } = string.Empty;
        public string InternetAccess { get; set; } = string.Empty;
        public string TravelTime { get; set; } = string.Empty;
        public string ExtraActivities { get; set; } = string.Empty;
        public string StudyMethod { get; set; } = string.Empty;
    }

    public class PredictionResultDto
    {
        public string PredictionId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string PredictionType { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string Level { get; set; } = string.Empty;
        public string RiskBand { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string FeatureSchemaVersion { get; set; } = string.Empty;
        public string? DataRangeFrom { get; set; }
        public string? DataRangeTo { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<string> Limitations { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class PredictionHistoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public decimal PredictedScore { get; set; }
        public string Level { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; }
        public string? ModelName { get; set; }
        public string? ModelVersion { get; set; }
        public DateTime PredictedAt { get; set; }
    }

    // ---------------------------------------------------------------------
    // Internal wire contract (snake_case) mirrored from school-ai-rag.
    // ---------------------------------------------------------------------
    public class AiPredictionRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string StudentRef { get; set; } = string.Empty;
        public string PredictionType { get; set; } = "performance";
        public string FeatureSchemaVersion { get; set; } = string.Empty;
        public string? ModelVersion { get; set; }
        public string? DataRangeFrom { get; set; }
        public string? DataRangeTo { get; set; }
        public AiPredictionFeatures Features { get; set; } = new();
    }

    public class AiPredictionFeatures
    {
        public int Age { get; set; }
        public double StudyHours { get; set; }
        public double AttendancePercentage { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string SchoolType { get; set; } = string.Empty;
        public string InternetAccess { get; set; } = string.Empty;
        public string TravelTime { get; set; } = string.Empty;
        public string ExtraActivities { get; set; } = string.Empty;
        public string StudyMethod { get; set; } = string.Empty;
    }

    public class AiPredictionResponse
    {
        public string StudentRef { get; set; } = string.Empty;
        public string PredictionType { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Level { get; set; } = string.Empty;
        public string RiskBand { get; set; } = string.Empty;
        public double? Confidence { get; set; }
        public List<AiPredictionFactor> Factors { get; set; } = new();
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string FeatureSchemaVersion { get; set; } = string.Empty;
        public string? DataRangeFrom { get; set; }
        public string? DataRangeTo { get; set; }
        public string GeneratedAt { get; set; } = string.Empty;
        public List<string> Limitations { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class AiPredictionFactor
    {
        public string Feature { get; set; } = string.Empty;
        public double Importance { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
