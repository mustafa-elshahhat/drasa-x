namespace DerasaX.Domain.Enums
{
    public enum AttendanceStatus
    {
        Present = 0,
        Absent = 1,
        Late = 2,
        Excused = 3
    }

    public enum AttendanceSource
    {
        Manual = 0,
        Import = 1,
        ComputerVision = 2
    }
}
