using System.ComponentModel.DataAnnotations;

namespace SAtS_MVC_Client.Models
{
    public class Course
    {
        // Caleb McIntyre - 12/11/2026: validation rules for the Add/Edit Course forms
        // (they match the database: courseID is up to 5 characters, courseName up to 50)
        [Required(ErrorMessage = "Please enter a course ID.")]
        [StringLength(5, ErrorMessage = "The course ID can be at most 5 characters.")]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "The course ID can only contain letters and numbers.")]
        [Display(Name = "Course ID")]
        public string CourseId { get; set; } = "";

        [Required(ErrorMessage = "Please enter a course name.")]
        [StringLength(50, ErrorMessage = "The course name can be at most 50 characters.")]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = "";
    }
}
