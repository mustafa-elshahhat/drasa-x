namespace DerasaX.Domain.Enums
{
    /// <summary>
    /// Lifecycle of a quiz, distinguishing an AI draft from a teacher-approved,
    /// published assessment.
    /// </summary>
    public enum QuizStatus
    {
        Draft = 0,
        AiGenerated = 1,
        PendingReview = 2,
        Approved = 3,
        Published = 4,
        Archived = 5
    }

    /// <summary>How a quiz originated.</summary>
    public enum QuizOrigin
    {
        Manual = 0,
        AiGenerated = 1
    }

    /// <summary>Review state of an AI quiz-generation record.</summary>
    public enum QuizGenerationStatus
    {
        Pending = 0,
        Succeeded = 1,
        Failed = 2,
        UnderReview = 3,
        Approved = 4,
        Rejected = 5
    }

    /// <summary>Kind of assignment given to students.</summary>
    public enum AssignmentType
    {
        Quiz = 0,
        Homework = 1,
        Reading = 2,
        Project = 3,
        Practice = 4
    }

    /// <summary>Lifecycle of an assignment.</summary>
    public enum AssignmentStatus
    {
        Draft = 0,
        Published = 1,
        Closed = 2,
        Archived = 3
    }

    /// <summary>Who/what an assignment is targeted at.</summary>
    public enum AssignmentTargetType
    {
        Class = 0,
        Student = 1,
        Grade = 2
    }

    /// <summary>How a score was produced.</summary>
    public enum GradingMethod
    {
        Automatic = 0,
        Manual = 1,
        Mixed = 2
    }
}
