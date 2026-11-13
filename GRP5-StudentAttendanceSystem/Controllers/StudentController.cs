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


        //-----Create, Edit, Delete
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.TblStudentDetails.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return Ok(student);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] TblStudentDetail student)
        {
            var currentLgeId = await _context.TblStudentDetails.Select(s => (int?)s.StudentId).MaxAsync() ?? 0;

            student.StudentId = currentLgeId + 1;

            _context.TblStudentDetails.Add(student);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetStudent), new { id = student.StudentId }, student);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] TblStudentDetail student)
        {
            var studentChange = await _context.TblStudentDetails.FindAsync(id);
            if (studentChange == null)
            {
                return NotFound();
            }

            // Update the student properties
            studentChange.StudentFname = student.StudentFname;
            studentChange.StudentLname = student.StudentLname;
            studentChange.StudentPhone = student.StudentPhone;
            studentChange.StudentEmail = student.StudentEmail;

            studentChange.StudentStreetAddress = student.StudentStreetAddress;
            studentChange.StudentSuburb = student.StudentSuburb;
            studentChange.StudentCity = student.StudentCity;
            // Fix: date of birth and postal code weren't being saved
            studentChange.StudentDob = student.StudentDob;
            studentChange.StudentPostalCode = student.StudentPostalCode;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.TblStudentDetails.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { message = "That student doesn't exist." });
            }

            // Fix: a student with attendance records can't be deleted (the database would refuse)
            if (await _context.TblAttendanceRecords.AnyAsync(r => r.StudentId == id))
            {
                return Conflict(new { message = $"{student.StudentFname} {student.StudentLname} has attendance records, so they can't be deleted." });
            }

            _context.TblStudentDetails.Remove(student);
            await _context.SaveChangesAsync();
            return NoContent(); 
        }

        // Caleb McIntyre - 11/11/2026
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
