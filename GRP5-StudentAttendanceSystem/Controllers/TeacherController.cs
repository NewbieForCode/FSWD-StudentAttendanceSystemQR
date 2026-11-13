using GRP5_StudentAttendanceSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRP5_StudentAttendanceSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly SAtS_Context _context;
        public TeacherController (SAtS_Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.TblTeacherDetails.ToListAsync();

            return Ok(teachers);
        }

        //-----Create, Edit, Delete
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.TblTeacherDetails.FindAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            return Ok(teacher);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeacher([FromBody] TblTeacherDetail teacher)
        {
            var currentLgeId = await _context.TblTeacherDetails.Select(t => (int?)t.TeacherId).MaxAsync() ?? 0;

            teacher.TeacherId = currentLgeId + 1;

            _context.TblTeacherDetails.Add(teacher);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTeacher), new { id = teacher.TeacherId }, teacher);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromBody] TblTeacherDetail teacher)
        {
            var teacherChange = await _context.TblTeacherDetails.FindAsync(id);
            if (teacherChange == null)
            {
                return NotFound();
            }

            teacherChange.TeacherFname = teacher.TeacherFname;
            teacherChange.TeacherLname = teacher.TeacherLname;
            teacherChange.TeacherPhone = teacher.TeacherPhone;
            teacherChange.TeacherEmail = teacher.TeacherEmail;
            teacherChange.TeacherDepartment = teacher.TeacherDepartment;


            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.TblTeacherDetails.FindAsync(id);
            if (teacher == null)
            {
                return NotFound(new { message = "That teacher doesn't exist." });
            }

            // Fix: a teacher who still has courses can't be deleted (the database would refuse)
            if (await _context.TblCourses.AnyAsync(c => c.TeacherId == id))
            {
                return Conflict(new { message = $"{teacher.TeacherFname} {teacher.TeacherLname} still teaches a course. Give their courses to another teacher first." });
            }

            _context.TblTeacherDetails.Remove(teacher);
            await _context.SaveChangesAsync();
            return NoContent();
        }


    }
}
