"""
Computer-vision (CV) module for school-ai-rag (Phase 15).

Adapted from the mandatory reference repo `student-engagement-system`
(commit e4352456) — see docs/phase15/PHASE15_REFERENCE_REPO_AUDIT.md. The
reusable CV logic (face-crop geometry, FaceNet -> cosine -> threshold ->
Unknown recognition decision, ResNet emotion softmax, CNN-BiLSTM 16-frame
engagement sequence, per-track frame buffering, response shape) is
re-implemented here tenant-safely, auth-gated, lazily-loaded and
biometric-safe.

Design rules (enforced across this package):
  * This service is STATELESS about DerasaX identity and storage. It performs
    inference and returns NORMALIZED results only. The backend owns persistence
    and student identity mapping.
  * Heavy CV dependencies (opencv / facenet / a face detector / torchvision
    model weights) are OPTIONAL and lazily imported. When absent (the default
    local/test state) a deterministic STUB engine runs, clearly labelled
    ``engine="stub"`` + ``degraded=true``, and readiness reports CV as not
    ready. Production loads real models from a configured dir with NO downloads.
  * All per-track frame buffers are keyed by (tenant_id, session_id, track_id)
    so state can never leak across tenants/classes (fixing the reference's
    process-global ``student_buffers``).
"""

SEQUENCE_LENGTH = 16
DEFAULT_RECOGNITION_THRESHOLD = 0.5
EMOTION_LABELS = ("Angry", "Fear", "Happy", "Sad", "Surprise")
ENGAGEMENT_LABELS = ("Engaged", "Disengaged")
