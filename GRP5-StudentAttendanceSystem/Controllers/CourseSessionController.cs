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

        public CourseSessionController(SAtS_Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var coursesession = await _context.TblCourseSessions.ToListAsync();

            return Ok(coursesession);
        }
    }
}
