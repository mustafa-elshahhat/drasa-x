namespace DerasaX.Domain.Enums
{
    public enum AuditActionType { Create = 0, Update = 1, Delete = 2, Login = 3, Export = 4, System = 5 }
    public enum FileRecordType { LessonMaterial = 0, MessageAttachment = 1, SubmissionAttachment = 2, ProfileImage = 3, Other = 4 }
    public enum AiUsageKind { Chat = 0, QuizGeneration = 1, Prediction = 2, Recommendation = 3 }
    public enum SettingValueType { String = 0, Number = 1, Boolean = 2, Json = 3 }
}
