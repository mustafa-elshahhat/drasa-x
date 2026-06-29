using System.Collections.Generic;

namespace DerasaX.Application.Dto.AiDto
{
    // ---------------------------------------------------------------------
    // Internal wire contract (snake_case) mirrored from school-ai-rag
    // /internal/v1/vision/* (Phase 15). The tenant is NEVER in the body — it is
    // taken from the signed service token. The image is an EPHEMERAL frame the
    // AI never persists; the backend persists only the normalized results below.
    // ---------------------------------------------------------------------
    public class AiVisionAnalyzeRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string ImageBase64 { get; set; } = string.Empty;
        public int FrameIndex { get; set; }
        public string? CaptureLabel { get; set; }
        public bool WantEngagement { get; set; } = true;
        public double? RecognitionThreshold { get; set; }
    }

    public class AiVisionFaceResult
    {
        public string TrackId { get; set; } = string.Empty;
        public List<int> Bbox { get; set; } = new();
        public string ExternalLabel { get; set; } = "Unknown";
        public string? ExternalLabelId { get; set; }
        public double RecognitionConfidence { get; set; }
        public string RecognitionStatus { get; set; } = "unknown";
        public string Emotion { get; set; } = "Unknown";
        public double EmotionConfidence { get; set; }
        public string Engagement { get; set; } = "NotReady";
        public double EngagementConfidence { get; set; }
        public int EngagementFrames { get; set; }
        public int EngagementFramesRequired { get; set; }
        public List<string> QualityFlags { get; set; } = new();
    }

    public class AiVisionAnalyzeResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int FacesDetected { get; set; }
        public List<AiVisionFaceResult> Results { get; set; } = new();
        public string Engine { get; set; } = string.Empty;
        public bool Degraded { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
        public Dictionary<string, string> ModelVersions { get; set; } = new();
        public int SequenceLength { get; set; }
        public List<string> QualityFlags { get; set; } = new();
        public string GeneratedAt { get; set; } = string.Empty;
    }

    public class AiVisionEndSessionRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class AiVisionEndSessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public int BuffersCleared { get; set; }
    }
}
