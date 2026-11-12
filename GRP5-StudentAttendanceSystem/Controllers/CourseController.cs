using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRP5_StudentAttendanceSystem.Models;

namespace GRP5_StudentAttendanceSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : Controller
    {
        private readonly SAtS_Context _context;

        public CourseController(SAtS_Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var courses = await _context.TblCourses.ToListAsync();

            return Ok(courses);
        }

        //------------------------------------------------------------
        // Caleb McIntyre - 12/11/2026
        // Create, read one, update and delete courses
        // (the MVC site only lets Admins use these).
        //------------------------------------------------------------

        // GET api/Course/FS101
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(string id)
        {
            var course = await _context.TblCourses.FindAsync(id);
            if (course == null)
            {
                return NotFound(new { message = "That course doesn't exist." });
            }

            return Ok(new { course.CourseId, course.CourseName });
        }

        // POST api/Course
        // Body: { "courseId": "FS101", "courseName": "Full Stack Web Development" }
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] TblCourse course)
        {
            course.CourseId = course.CourseId?.Trim().ToUpper() ?? "";
            course.CourseName = course.CourseName?.Trim() ?? "";

            //Course IDs must be unique
            if (await _context.TblCourses.AnyAsync(c => c.CourseId == course.CourseId))
            {
                return Conflict(new { message = $"A course with the ID {course.CourseId} already exists." });
            }

            _context.TblCourses.Add(new TblCourse { CourseId = course.CourseId, CourseName = course.CourseName });
            await _context.SaveChangesAsync();

            return Ok(new { course.CourseId, course.CourseName });
        }

        // PUT api/Course/FS101
        // Changes the course name (the course ID can't be changed)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(string id, [FromBody] TblCourse course)
        {
            var existing = await _context.TblCourses.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "That course doesn't exist." });
            }

            existing.CourseName = course.CourseName?.Trim() ?? "";
            await _context.SaveChangesAsync();

            return Ok(new { existing.CourseId, existing.CourseName });
        }

        // DELETE api/Course/FS101
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(string id)
        {
            var course = await _context.TblCourses.FindAsync(id);
            if (course == null)
            {
                return NotFound(new { message = "That course doesn't exist." });
            }

            //A course with sessions can't be deleted, or the sessions would be left without a course
            if (await _context.TblCourseSessions.AnyAsync(s => s.CourseId == id))
            {
                return Conflict(new { message = $"{course.CourseId} has course sessions, so it can't be deleted. Delete its sessions first." });
            }

            _context.TblCourses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
