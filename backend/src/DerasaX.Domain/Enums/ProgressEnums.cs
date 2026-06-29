namespace DerasaX.Domain.Enums
{
    public enum ProgressMetricType
    {
        Attendance = 0,
        QuizScore = 1,
        AssignmentScore = 2,
        LessonCompletion = 3,
        StudyTime = 4,
        Engagement = 5
    }

    public enum PainPointCategory
    {
        Concept = 0,
        Skill = 1,
        Attendance = 2,
        Engagement = 3,
        Assessment = 4,
        NoFinding = 5
    }

    public enum RecommendationStatus
    {
        Open = 0,
        InProgress = 1,
        Completed = 2,
        Dismissed = 3
    }

    public enum PredictionKind
    {
        Performance = 0,
        Risk = 1,
        Engagement = 2
    }
}
