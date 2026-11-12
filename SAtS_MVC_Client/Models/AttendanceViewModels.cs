namespace SAtS_MVC_Client.Models
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // ViewModels for the My Attendance report and QR check-in pages
    //------------------------------------------------------------

    //One course row on the My Attendance page
    public class CourseAttendanceViewModel
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }

        //Attendance as a whole-number percentage
        public int Percentage =>
            TotalSessions == 0 ? 0 : (int)Math.Round(AttendedSessions * 100.0 / TotalSessions);
    }

    //The whole My Attendance page
    public class MyAttendanceViewModel
    {
        public string StudentName { get; set; } = "";
        public List<CourseAttendanceViewModel> Courses { get; set; } = new List<CourseAttendanceViewModel>();

        public int TotalSessions => Courses.Sum(c => c.TotalSessions);
        public int AttendedSessions => Courses.Sum(c => c.AttendedSessions);
        public int OverallPercentage =>
            TotalSessions == 0 ? 0 : (int)Math.Round(AttendedSessions * 100.0 / TotalSessions);
    }

    //A course session, as sent by the API
    public class SessionInfo
    {
        public int CSessionId { get; set; }
        public DateOnly CSessionDate { get; set; }
        public string CSessionQrToken { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int CheckedInCount { get; set; }
    }

    //The page a teacher shows on screen with the session's QR code
    public class SessionQrViewModel
    {
        public SessionInfo Session { get; set; } = new SessionInfo();

        //The link the QR code points to (the student check-in page)
        public string CheckInUrl { get; set; } = "";

        //The QR code image, made by the external goQR.me API
        public string QrImageUrl { get; set; } = "";
    }

    //The answer the API gives after a check-in attempt
    public class CheckInResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
