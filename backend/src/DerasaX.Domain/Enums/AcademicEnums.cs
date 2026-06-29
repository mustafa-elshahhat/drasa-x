namespace DerasaX.Domain.Enums
{
    /// <summary>Status of a student's enrollment in a class for an academic year.</summary>
    public enum EnrollmentStatus
    {
        Active = 0,
        Withdrawn = 1,
        Completed = 2,
        Transferred = 3,
        Suspended = 4
    }

    /// <summary>Guardian relationship type between a parent/guardian and a student.</summary>
    public enum GuardianRelationship
    {
        Mother = 0,
        Father = 1,
        Guardian = 2,
        Sibling = 3,
        Other = 4
    }

    /// <summary>Role a teacher plays for a class assignment.</summary>
    public enum TeacherClassRole
    {
        SubjectTeacher = 0,
        HomeroomTeacher = 1,
        Assistant = 2
    }
}
