using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRP5_StudentAttendanceSystem.Models;

namespace GRP5_StudentAttendanceSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : Controller
    {
        private readonly SAtS_Context _context;

        public StudentController(SAtS_Context context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _context.TblStudentDetails.ToListAsync();

            return Ok(students);
        }

        // GET api/Student/ByEmail?email=someone@example.com
        // Returns the one student whose email matches, or 404 if there isn't one
        [HttpGet("ByEmail")]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var student = await _context.TblStudentDetails
                .FirstOrDefaultAsync(s => s.StudentEmail == email);

            if (student == null)
            {
                return NotFound();
            }

            return Ok(student);
        }


    }
}
