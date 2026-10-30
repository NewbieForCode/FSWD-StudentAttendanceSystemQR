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


    }
}
