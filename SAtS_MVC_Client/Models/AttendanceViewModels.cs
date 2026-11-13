using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // Caleb McIntyre - 13/11/2026: the public holiday on this date, if there is one
        public string? HolidayName { get; set; }
    }

    // Caleb McIntyre - 13/11/2026
    //An upcoming NZ public holiday (from the Nager.Date API, through our API)
    public class HolidayInfo
    {
        public DateOnly Date { get; set; }
        public string Name { get; set; } = "";
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

    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // Create Session form and the session attendance list
    //------------------------------------------------------------

    //The Create Session form
    public class CreateSessionViewModel
    {
        [Required(ErrorMessage = "Please pick a course.")]
        [Display(Name = "Course")]
        public string CourseId { get; set; } = "";

        [Required(ErrorMessage = "Please pick a date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Session date")]
        public DateOnly SessionDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        //The options for the course drop-down
        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();

        // Caleb McIntyre - 13/11/2026: upcoming public holidays, shown beside the form
        public List<HolidayInfo> Holidays { get; set; } = new List<HolidayInfo>();
    }

    // Caleb McIntyre - 13/11/2026
    //The Edit Session form (same fields as Create Session, plus which session it is)
    public class EditSessionViewModel : CreateSessionViewModel
    {
        public int CSessionId { get; set; }

        //The date before editing, so an old session can keep its date
        public DateOnly OriginalDate { get; set; }

        public string QrToken { get; set; } = "";
    }

    //One student who checked in to a session
    public class AttendanceRecordRow
    {
        public int AttendanceId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentEmail { get; set; } = "";
        public DateTime TimeOfScan { get; set; }
    }

    //The page listing who attended one session
    public class SessionAttendanceViewModel
    {
        public SessionInfo Session { get; set; } = new SessionInfo();
        public List<AttendanceRecordRow> Records { get; set; } = new List<AttendanceRecordRow>();
    }

    //The answer the API gives after a check-in attempt
    public class CheckInResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
