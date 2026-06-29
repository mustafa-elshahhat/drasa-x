namespace DerasaX.Domain.Enums
{
    public enum ConversationType
    {
        ParentTeacher = 0,
        StudentTeacher = 1,
        ClassGroup = 2,
        Support = 3
    }

    public enum ConversationParticipantRole
    {
        Student = 0,
        Parent = 1,
        Teacher = 2,
        SchoolAdmin = 3,
        SystemAdmin = 4
    }

    public enum MessageType
    {
        Text = 0,
        Attachment = 1,
        System = 2
    }

    public enum ParentRequestType
    {
        Document = 0,
        Meeting = 1,
        ProgressFollowUp = 2,
        TeacherContact = 3,
        Other = 4
    }

    public enum ParentRequestStatus
    {
        Open = 0,
        InProgress = 1,
        Resolved = 2,
        Rejected = 3,
        Closed = 4
    }

    public enum SuggestionStatus
    {
        Submitted = 0,
        UnderReview = 1,
        Accepted = 2,
        Rejected = 3,
        Implemented = 4
    }
}
