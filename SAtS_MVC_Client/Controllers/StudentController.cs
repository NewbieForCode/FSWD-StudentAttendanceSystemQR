using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;

namespace SAtS_MVC_Client.Controllers
{
    public class StudentController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(IHttpClientFactory httpClientFactory, UserManager<ApplicationUser> userManager)
        {
            _httpClient = httpClientFactory.CreateClient();
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Index()
        {
            var students = await _httpClient.GetFromJsonAsync<List<Student>>
                ("https://localhost:7147/api/Student");

            return View(students);
        }

        //------------------------------------------------------------
        // Caleb McIntyre - 11/11/2026
        //A signed-in student sees only their own record.
        //The record is matched by the email address of the signed-in account.
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyRecord()
        {
            var student = await FindSignedInStudent();

            if (student == null)
            {
                ViewBag.Message = $"No student record matches the email {await GetSignedInEmail()}.";
                return View();
            }

            return View(student);
        }
        //------------------------------------------------------------

        //------------------------------------------------------------
        // Caleb McIntyre - 12/11/2026
        //A signed-in student's attendance report: for each course,
        //how many sessions they attended out of the total.
        //The numbers come from the API's JOIN + GROUP BY query.
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyAttendance()
        {
            var student = await FindSignedInStudent();

            if (student == null)
            {
                ViewBag.Message = $"No student record matches the email {await GetSignedInEmail()}.";
                return View(new MyAttendanceViewModel());
            }

            var courses = await _httpClient.GetFromJsonAsync<List<CourseAttendanceViewModel>>
                ("https://localhost:7147/api/StudentAttendance/Summary/" + student.StudentId);

            var model = new MyAttendanceViewModel
            {
                StudentName = student.StudentFName + " " + student.StudentLName,
                Courses = courses ?? new List<CourseAttendanceViewModel>()
            };

            return View(model);
        }

        //Gets the email of the signed-in account
        private async Task<string> GetSignedInEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Email ?? user?.UserName ?? "";
        }

        //Looks up the student whose email matches the signed-in account
        private async Task<Student?> FindSignedInStudent()
        {
            var email = await GetSignedInEmail();

            var response = await _httpClient.GetAsync
                ("https://localhost:7147/api/Student/ByEmail?email=" + Uri.EscapeDataString(email));

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Student>();
        }
        //------------------------------------------------------------
    }
}
