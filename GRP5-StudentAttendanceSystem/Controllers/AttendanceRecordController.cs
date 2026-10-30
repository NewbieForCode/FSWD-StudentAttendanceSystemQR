using GRP5_StudentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRP5_StudentAttendanceSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceRecordController : Controller
    {
        private readonly SAtS_Context _context;

        public AttendanceRecordController(SAtS_Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var attendrecord = await _context.TblAttendanceRecords.ToListAsync();

            return Ok(attendrecord);
        }
    }
}
