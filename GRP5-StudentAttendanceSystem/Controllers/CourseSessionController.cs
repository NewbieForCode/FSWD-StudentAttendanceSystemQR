using GRP5_StudentAttendanceSystem.Models;
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

        public CourseSessionController(SAtS_Context context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var coursesession = await _context.TblCourseSessions.ToListAsync();

            return Ok(coursesession);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, [FromBody] TblCourseSession session)
        {
            var existingSession = await _context.TblCourseSessions
                .FindAsync(id);

            if (existingSession == null)
            {
                return NotFound();
            }

            existingSession.CSessionDate = session.CSessionDate;
            existingSession.CSessionQrtoken = session.CSessionQrtoken;
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
                return NotFound();
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
