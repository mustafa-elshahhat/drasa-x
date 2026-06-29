"""
Typed, strict request/response schemas for the internal v1 AI contract
(Phase 6 §4). These are the stable boundary between DerasaX-backend and
school-ai-rag. Unknown fields are rejected (``extra="forbid"``); identifiers,
language, and sizes are validated.

The tenant is NEVER part of these bodies — it is taken from the signed service
token. The backend supplies the remaining (already-authorized) curriculum
context in the body.
"""
from __future__ import annotations

from typing import List, Literal, Optional

from pydantic import BaseModel, ConfigDict, Field

# Bounds (also enforced as defense-in-depth in the service layer).
MAX_MESSAGE_CHARS = 4000
MAX_HISTORY_TURNS = 20
MAX_HISTORY_CHARS = 8000

Language = Literal["en", "ar"]


class _Strict(BaseModel):
    model_config = ConfigDict(extra="forbid")


class ConversationTurn(_Strict):
    role: Literal["user", "assistant"]
    content: str = Field(min_length=1, max_length=MAX_MESSAGE_CHARS)


class TutorRequest(_Strict):
    """Backend-mediated tutor query. Trusted curriculum context only."""
    correlation_id: str = Field(min_length=1, max_length=128)
    message: str = Field(min_length=1, max_length=MAX_MESSAGE_CHARS)
    language: Language = "en"

    # Curriculum scope (already authorized by the backend for this student).
    grade: Optional[int] = Field(default=None, ge=1, le=12)
    subject: Optional[str] = Field(default=None, max_length=64)
    unit_id: Optional[str] = Field(default=None, max_length=64)
    lesson_id: Optional[str] = Field(default=None, max_length=64)

    # Bounded prior turns (history is trimmed server-side too).
    history: List[ConversationTurn] = Field(default_factory=list)

    # Operation tuning.
    prompt_version: Optional[str] = Field(default=None, max_length=32)
    top_k: int = Field(default=4, ge=1, le=12)


class Citation(_Strict):
    """A grounded citation referencing only a retrieved, tenant-authorized source."""
    source_document_id: str
    chunk_id: str
    score: float = Field(ge=0.0, le=1.0)
    title: Optional[str] = None
    grade: Optional[int] = None
    subject: Optional[str] = None
    unit: Optional[str] = None
    lesson: Optional[str] = None
    snippet: Optional[str] = None


# --- quiz draft generation (§11) --------------------------------------------
QuizQuestionType = Literal["mcq", "true_false"]
Difficulty = Literal["remedial", "core", "advanced"]
MAX_QUIZ_QUESTIONS = 20
MAX_QUESTION_POINTS = 10


class QuizDraftRequest(_Strict):
    """Backend-mediated request for a grounded, draft-only quiz. Tenant from token."""
    correlation_id: str = Field(min_length=1, max_length=128)
    num_questions: int = Field(ge=1, le=MAX_QUIZ_QUESTIONS)
    language: Language = "en"
    grade: Optional[int] = Field(default=None, ge=1, le=12)
    subject: Optional[str] = Field(default=None, max_length=64)
    unit_id: Optional[str] = Field(default=None, max_length=64)
    lesson_id: Optional[str] = Field(default=None, max_length=64)
    topic: Optional[str] = Field(default=None, max_length=256)
    difficulty: Difficulty = "core"
    question_types: List[QuizQuestionType] = Field(default_factory=lambda: ["mcq"])
    prompt_version: Optional[str] = Field(default=None, max_length=32)
    top_k: int = Field(default=6, ge=1, le=12)


class QuizDraftQuestion(_Strict):
    question_type: QuizQuestionType
    question_text: str
    options: List[str]
    correct_index: int
    explanation: str
    points: int
    source_references: List[str] = Field(default_factory=list)


class QuizDraft(_Strict):
    title: str
    instructions: str
    grade: Optional[int] = None
    subject: Optional[str] = None
    unit: Optional[str] = None
    lesson: Optional[str] = None
    difficulty: Difficulty
    question_count: int
    questions: List[QuizDraftQuestion]


class QuizDraftResponse(_Strict):
    draft: QuizDraft
    grounded: bool
    citations: List[Citation] = Field(default_factory=list)
    provider: str
    model: str
    model_version: str
    prompt_version: str
    retrieval_count: int
    correlation_id: str
    generated_at: str


# --- conversation / pain-point analysis (§13) -------------------------------
PainPointCategoryLiteral = Literal["concept", "skill", "attendance", "engagement", "assessment", "none"]
EscalationLiteral = Literal["none", "monitor", "escalate"]
MAX_ANALYSIS_TURNS = 40


class AnalysisTurn(_Strict):
    role: Literal["user", "assistant"]
    content: str = Field(min_length=1, max_length=MAX_MESSAGE_CHARS)


class AnalysisRequest(_Strict):
    correlation_id: str = Field(min_length=1, max_length=128)
    student_ref: str = Field(min_length=1, max_length=128)
    conversation: List[AnalysisTurn] = Field(min_length=1, max_length=MAX_ANALYSIS_TURNS)
    language: Language = "en"
    subject: Optional[str] = Field(default=None, max_length=64)
    unit_id: Optional[str] = Field(default=None, max_length=64)
    lesson_id: Optional[str] = Field(default=None, max_length=64)
    prompt_version: Optional[str] = Field(default=None, max_length=32)


class AnalysisResponse(_Strict):
    student_ref: str
    pain_point_category: PainPointCategoryLiteral
    subject: Optional[str] = None
    unit: Optional[str] = None
    lesson: Optional[str] = None
    evidence_summary: str
    recommendation: str
    confidence: float = Field(ge=0.0, le=1.0)
    escalation_level: EscalationLiteral
    human_review_required: bool
    signals: List[str] = Field(default_factory=list)
    model: str
    model_version: str
    prompt_version: str
    generated_at: str
    correlation_id: str


# --- performance prediction (§12) -------------------------------------------
GenderLiteral = Literal["male", "female"]
SchoolTypeLiteral = Literal["public", "private"]
YesNoLiteral = Literal["yes", "no"]
TravelTimeLiteral = Literal["<15 min", "15-30 min", "30-60 min", ">60 min"]
StudyMethodLiteral = Literal["textbook", "notes", "online videos", "group study", "mixed"]


class PredictionFeatures(_Strict):
    """Raw feature schema v1 — derived by the backend from authoritative records."""
    age: int = Field(ge=3, le=100)
    study_hours: float = Field(ge=0, le=168, allow_inf_nan=False)
    attendance_percentage: float = Field(ge=0, le=100, allow_inf_nan=False)
    gender: GenderLiteral
    school_type: SchoolTypeLiteral
    internet_access: YesNoLiteral
    travel_time: TravelTimeLiteral
    extra_activities: YesNoLiteral
    study_method: StudyMethodLiteral


class PredictionRequest(_Strict):
    correlation_id: str = Field(min_length=1, max_length=128)
    student_ref: str = Field(min_length=1, max_length=128)
    prediction_type: Literal["performance"] = "performance"
    feature_schema_version: str = Field(min_length=1, max_length=32)
    model_version: Optional[str] = Field(default=None, max_length=64)
    data_range_from: Optional[str] = Field(default=None, max_length=40)
    data_range_to: Optional[str] = Field(default=None, max_length=40)
    features: PredictionFeatures


class PredictionFactor(_Strict):
    feature: str
    importance: float
    kind: str


class PredictionResponse(_Strict):
    student_ref: str
    prediction_type: str
    score: float
    level: str
    risk_band: str
    confidence: Optional[float] = None
    factors: List[PredictionFactor] = Field(default_factory=list)
    model_name: str
    model_version: str
    feature_schema_version: str
    data_range_from: Optional[str] = None
    data_range_to: Optional[str] = None
    generated_at: str
    limitations: List[str] = Field(default_factory=list)
    correlation_id: str


# --- document ingestion / lifecycle (§7) ------------------------------------
MAX_DOCUMENT_CHARS = 1_000_000
SUPPORTED_MATERIAL_TYPES = ("textbook", "notes", "worksheet", "summary", "transcript", "other")
MaterialType = Literal["textbook", "notes", "worksheet", "summary", "transcript", "other"]


class IngestDocumentRequest(_Strict):
    """Authenticated, backend-mediated ingestion of one curriculum document.

    ``content`` is the already-extracted text (the backend owns the source file
    and performs extraction). Tenant is taken from the token, never this body.
    """
    correlation_id: str = Field(min_length=1, max_length=128)
    document_id: str = Field(min_length=1, max_length=128)
    version: int = Field(ge=1, le=1_000_000)
    content: str = Field(min_length=1, max_length=MAX_DOCUMENT_CHARS)
    language: Language = "en"
    material_type: MaterialType = "other"

    title: Optional[str] = Field(default=None, max_length=256)
    file_id: Optional[str] = Field(default=None, max_length=128)
    academic_year: Optional[str] = Field(default=None, max_length=32)
    grade: Optional[int] = Field(default=None, ge=1, le=12)
    subject: Optional[str] = Field(default=None, max_length=64)
    unit: Optional[str] = Field(default=None, max_length=64)
    lesson: Optional[str] = Field(default=None, max_length=64)


class IngestDocumentResponse(_Strict):
    document_id: str
    version: int
    status: str            # "indexed" | "reindexed" | "duplicate"
    chunk_count: int
    removed_chunks: int
    checksum: str
    language: str
    collection: str        # opaque, non-sensitive tenant collection id
    indexed_at: str
    correlation_id: str


class DeleteDocumentResponse(_Strict):
    document_id: str
    deleted_chunks: int
    status: str            # "deleted" | "not_found"


class DocumentStatusResponse(_Strict):
    document_id: str
    indexed: bool
    version: Optional[int] = None
    chunk_count: int = 0


class TutorResponse(_Strict):
    """Normalized retrieval contract (§8)."""
    answer: str
    grounded: bool
    no_answer_reason: Optional[str] = None
    citations: List[Citation] = Field(default_factory=list)

    provider: str
    model: str
    model_version: str
    prompt_version: str

    retrieval_count: int
    citation_count: int
    latency_ms: int
    correlation_id: str
