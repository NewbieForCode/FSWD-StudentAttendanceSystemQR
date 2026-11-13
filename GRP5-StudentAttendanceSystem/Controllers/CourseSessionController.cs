using GRP5_StudentAttendanceSystem.Models;
using GRP5_StudentAttendanceSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRP5_StudentAttendanceSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseSessionController : Controller
    {
        private readonly SAtS_Context _context;
        private readonly HttpClient _httpClient;
        private readonly PublicHolidayService _holidays;
        public CourseSessionController(SAtS_Context context, IHttpClientFactory httpClientFactory, PublicHolidayService holidays)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _holidays = holidays; // Caleb McIntyre - 13/11/2026: NZ public holidays
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var coursesession = await _context.TblCourseSessions.ToListAsync();

            return Ok(coursesession);
        }

        //------------------------------------------------------------
        // Caleb McIntyre - 13/11/2026
        // Get one session, and fixes to edit and delete:
        //  - edit only changes the course and date (the QR token stays the same)
        //  - the course must exist, and a course can't have two sessions on one day
        //  - a session that students have checked in to can't be deleted
        //  - errors come back with a message the website can show
        //------------------------------------------------------------

        // GET api/CourseSession/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSession(int id)
        {
            var session = await _context.TblCourseSessions.FindAsync(id);
            if (session == null)
            {
                return NotFound(new { message = "That session doesn't exist." });
            }

            return Ok(new { session.CSessionId, session.CSessionDate, session.CSessionQrtoken, session.CourseId });
        }

        // PUT api/CourseSession/5
        // Body: { "courseId": "FS101", "sessionDate": "2026-11-20" }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, [FromBody] UpdateSessionDto session)
        {
            var existingSession = await _context.TblCourseSessions
                .FindAsync(id);

            if (existingSession == null)
            {
                return NotFound(new { message = "That session doesn't exist." });
            }

            if (!await _context.TblCourses.AnyAsync(c => c.CourseId == session.CourseId))
            {
                return BadRequest(new { message = "That course doesn't exist." });
            }

            // Caleb McIntyre - 13/11/2026: a session can't be moved onto a public holiday
            if (session.SessionDate != existingSession.CSessionDate)
            {
                var holiday = await _holidays.FindHolidayAsync(session.SessionDate);
                if (holiday != null)
                {
                    return BadRequest(new { message = $"{session.SessionDate:dd/MM/yyyy} is {holiday.LocalName}, a public holiday. Please pick another date." });
                }
            }

            var duplicate = await _context.TblCourseSessions.AnyAsync(s =>
                s.CSessionId != id && s.CourseId == session.CourseId && s.CSessionDate == session.SessionDate);
            if (duplicate)
            {
                return Conflict(new { message = $"{session.CourseId} already has a session on {session.SessionDate:dd/MM/yyyy}." });
            }

            existingSession.CSessionDate = session.SessionDate;
            existingSession.CourseId = session.CourseId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.TblCourseSessions
                .FindAsync(id);

            if (session == null)
            {
                return NotFound(new { message = "That session doesn't exist." });
            }

            // Caleb McIntyre - 13/11/2026: keep attendance records safe
            if (await _context.TblAttendanceRecords.AnyAsync(r => r.CSessionId == id))
            {
                return Conflict(new { message = "Students have checked in to this session, so it can't be deleted." });
            }

            _context.TblCourseSessions.Remove(session);

            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("{id}/qrcode")]
        public async Task<IActionResult> GenerateQRCode(int id)
        {
            var session = await _context.TblCourseSessions.FirstOrDefaultAsync(s => s.CSessionId == id);

            if (session == null)
            {
                return NotFound();
            }


            string qrContent = session.CSessionQrtoken;
            string qrUrl = $"http://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(qrContent)}";
            //http(s)://api.qrserver.com/v1/create-qr-code/?data=[URL-encoded-text]&size=[pixels]x[pixels]
            //http://api.qrserver.com/v1/create-qr-code/?data=HelloWorld!&size=100x100




            var imageBytes = await _httpClient.GetByteArrayAsync(qrUrl);

            return File(imageBytes, "image/png");

        }



    }
}
