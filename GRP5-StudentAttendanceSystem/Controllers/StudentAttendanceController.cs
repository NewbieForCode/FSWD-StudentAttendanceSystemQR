using GRP5_StudentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRP5_StudentAttendanceSystem.Controllers
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // API endpoints for the student attendance report and QR check-in.
    //------------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    public class StudentAttendanceController : ControllerBase
    {
        private readonly SAtS_Context _context;

        public StudentAttendanceController(SAtS_Context context)
        {
            _context = context;
        }

        // GET api/StudentAttendance/Summary/5
        // Attendance report for one student: for every course, how many
        // sessions it has and how many of them the student attended.
        // Uses JOINs (sessions -> courses, sessions -> attendance records)
        // and GROUP BY course, with COUNT aggregates.
        [HttpGet("Summary/{studentId}")]
        public async Task<IActionResult> GetSummary(int studentId)
        {
            var summary = await (
                from session in _context.TblCourseSessions
                join course in _context.TblCourses
                    on session.CourseId equals course.CourseId
                join record in _context.TblAttendanceRecords.Where(r => r.StudentId == studentId)
                    on session.CSessionId equals record.CSessionId into studentRecords
                from record in studentRecords.DefaultIfEmpty()
                group new { session.CSessionId, AttendedSessionId = (int?)record.CSessionId }
                    by new { course.CourseId, course.CourseName } into courseGroup
                orderby courseGroup.Key.CourseName
                select new AttendanceSummaryDto
                {
                    CourseId = courseGroup.Key.CourseId,
                    CourseName = courseGroup.Key.CourseName,
                    TotalSessions = courseGroup.Select(x => x.CSessionId).Distinct().Count(),
                    AttendedSessions = courseGroup.Select(x => x.AttendedSessionId).Distinct().Count()
                }).ToListAsync();

            return Ok(summary);
        }

        // GET api/StudentAttendance/Sessions
        // Every course session with its course name and how many students checked in
        // (JOIN sessions -> courses, LEFT JOIN attendance records, GROUP BY session).
        [HttpGet("Sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var sessions = await (
                from session in _context.TblCourseSessions
                join course in _context.TblCourses
                    on session.CourseId equals course.CourseId
                join record in _context.TblAttendanceRecords
                    on session.CSessionId equals record.CSessionId into sessionRecords
                from record in sessionRecords.DefaultIfEmpty()
                group new { CheckedInStudentId = (int?)record.StudentId }
                    by new { session.CSessionId, session.CSessionDate, session.CSessionQrtoken, course.CourseId, course.CourseName } into sessionGroup
                orderby sessionGroup.Key.CSessionDate descending
                select new SessionInfoDto
                {
                    CSessionId = sessionGroup.Key.CSessionId,
                    CSessionDate = sessionGroup.Key.CSessionDate,
                    CSessionQrToken = sessionGroup.Key.CSessionQrtoken,
                    CourseId = sessionGroup.Key.CourseId,
                    CourseName = sessionGroup.Key.CourseName,
                    CheckedInCount = sessionGroup.Select(x => x.CheckedInStudentId).Distinct().Count()
                }).ToListAsync();

            return Ok(sessions);
        }

        // GET api/StudentAttendance/Session/5
        // One course session (used to show its QR code)
        [HttpGet("Session/{id}")]
        public async Task<IActionResult> GetSession(int id)
        {
            var session = await FindSessionInfo(s => s.CSessionId == id);
            if (session == null)
            {
                return NotFound();
            }
            return Ok(session);
        }

        // GET api/StudentAttendance/SessionByToken?token=abc
        // One course session, found by the token inside its QR code
        [HttpGet("SessionByToken")]
        public async Task<IActionResult> GetSessionByToken([FromQuery] string token)
        {
            var session = await FindSessionInfo(s => s.CSessionQrtoken == token);
            if (session == null)
            {
                return NotFound();
            }
            return Ok(session);
        }

        // POST api/StudentAttendance/CheckIn
        // Records that a student attended a session.
        // Body: { "studentId": 11, "token": "the session's QR token" }
        [HttpPost("CheckIn")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDto request)
        {
            //The QR token must belong to a real session
            var session = await _context.TblCourseSessions
                .FirstOrDefaultAsync(s => s.CSessionQrtoken == request.Token);
            if (session == null)
            {
                return NotFound(new CheckInResultDto { Success = false, Message = "This QR code doesn't match any session." });
            }

            //The student must exist
            var studentExists = await _context.TblStudentDetails.AnyAsync(s => s.StudentId == request.StudentId);
            if (!studentExists)
            {
                return NotFound(new CheckInResultDto { Success = false, Message = "Your account isn't linked to a student record." });
            }

            //Students can only check in on the day of the session
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (session.CSessionDate != today)
            {
                return BadRequest(new CheckInResultDto
                {
                    Success = false,
                    Message = $"Check-in for this session is only open on {session.CSessionDate:dd/MM/yyyy}."
                });
            }

            //A student can only check in once per session
            var alreadyCheckedIn = await _context.TblAttendanceRecords
                .AnyAsync(r => r.StudentId == request.StudentId && r.CSessionId == session.CSessionId);
            if (alreadyCheckedIn)
            {
                return Conflict(new CheckInResultDto { Success = false, Message = "You've already checked in to this session." });
            }

            //attendanceID isn't auto-numbered in the database, so use the next number
            var nextId = (await _context.TblAttendanceRecords.MaxAsync(r => (int?)r.AttendanceId) ?? 0) + 1;

            _context.TblAttendanceRecords.Add(new TblAttendanceRecord
            {
                AttendanceId = nextId,
                StudentId = request.StudentId,
                CSessionId = session.CSessionId,
                AttendanceTimeOfScan = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Ok(new CheckInResultDto { Success = true, Message = "You're checked in. Thanks!" });
        }

        //------------------------------------------------------------
        // Caleb McIntyre - 12/11/2026
        // Create a course session, and list who attended a session
        //------------------------------------------------------------

        // POST api/StudentAttendance/Session
        // Body: { "courseId": "FS101", "sessionDate": "2026-11-12" }
        // Creates a session with a new random QR token
        [HttpPost("Session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto request)
        {
            var courseExists = await _context.TblCourses.AnyAsync(c => c.CourseId == request.CourseId);
            if (!courseExists)
            {
                return NotFound(new { message = "That course doesn't exist." });
            }

            //Sessions can't be created in the past
            if (request.SessionDate < DateOnly.FromDateTime(DateTime.Now))
            {
                return BadRequest(new { message = "The session date can't be in the past." });
            }

            //Only one session per course per day
            var duplicate = await _context.TblCourseSessions
                .AnyAsync(s => s.CourseId == request.CourseId && s.CSessionDate == request.SessionDate);
            if (duplicate)
            {
                return Conflict(new { message = $"{request.CourseId} already has a session on {request.SessionDate:dd/MM/yyyy}." });
            }

            //cSessionID isn't auto-numbered in the database, so use the next number
            var nextId = (await _context.TblCourseSessions.MaxAsync(s => (int?)s.CSessionId) ?? 0) + 1;

            _context.TblCourseSessions.Add(new TblCourseSession
            {
                CSessionId = nextId,
                CourseId = request.CourseId,
                CSessionDate = request.SessionDate,
                //A random, hard-to-guess token for the QR code
                CSessionQrtoken = Guid.NewGuid().ToString("N")
            });
            await _context.SaveChangesAsync();

            var created = await FindSessionInfo(s => s.CSessionId == nextId);
            return Ok(created);
        }

        // GET api/StudentAttendance/Session/5/Records
        // Every student who checked in to a session, earliest first
        // (JOIN attendance records -> students)
        [HttpGet("Session/{id}/Records")]
        public async Task<IActionResult> GetSessionRecords(int id)
        {
            var sessionExists = await _context.TblCourseSessions.AnyAsync(s => s.CSessionId == id);
            if (!sessionExists)
            {
                return NotFound(new { message = "That session doesn't exist." });
            }

            var records = await (
                from record in _context.TblAttendanceRecords
                join student in _context.TblStudentDetails
                    on record.StudentId equals student.StudentId
                where record.CSessionId == id
                orderby record.AttendanceTimeOfScan
                select new AttendanceRecordDto
                {
                    AttendanceId = record.AttendanceId,
                    StudentId = student.StudentId,
                    StudentName = student.StudentFname + " " + student.StudentLname,
                    StudentEmail = student.StudentEmail,
                    TimeOfScan = record.AttendanceTimeOfScan
                }).ToListAsync();

            return Ok(records);
        }

        //Finds one session (with its course name and check-in count) using the given condition
        private async Task<SessionInfoDto?> FindSessionInfo(System.Linq.Expressions.Expression<Func<TblCourseSession, bool>> condition)
        {
            return await _context.TblCourseSessions
                .Where(condition)
                .Select(s => new SessionInfoDto
                {
                    CSessionId = s.CSessionId,
                    CSessionDate = s.CSessionDate,
                    CSessionQrToken = s.CSessionQrtoken,
                    CourseId = s.CourseId,
                    CourseName = s.Course.CourseName,
                    CheckedInCount = s.TblAttendanceRecords.Count()
                })
                .FirstOrDefaultAsync();
        }
    }
}
