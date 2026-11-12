namespace GRP5_StudentAttendanceSystem.Models
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // Small classes the API sends back for the attendance report
    // and QR check-in (so we don't send whole database entities).
    //------------------------------------------------------------

    //One row of a student's attendance report (one per course)
    public class AttendanceSummaryDto
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
    }

    //A course session with its course name and how many students checked in
    public class SessionInfoDto
    {
        public int CSessionId { get; set; }
        public DateOnly CSessionDate { get; set; }
        public string CSessionQrToken { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int CheckedInCount { get; set; }
    }

    //What the MVC client sends when a student checks in
    public class CheckInRequestDto
    {
        public int StudentId { get; set; }
        public string Token { get; set; } = "";
    }

    //The answer the API gives back after a check-in attempt
    public class CheckInResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
