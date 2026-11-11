using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;

namespace SAtS_MVC_Client.Controllers
{
    public class StudentController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(IHttpClientFactory httpClientFactory, UserManager<IdentityUser> userManager)
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
        //A signed-in student sees only their own record.
        //The record is matched by the email address of the signed-in account.
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyRecord()
        {
            var user = await _userManager.GetUserAsync(User);
            var email = user?.Email ?? user?.UserName ?? "";

            var response = await _httpClient.GetAsync
                ("https://localhost:7147/api/Student/ByEmail?email=" + Uri.EscapeDataString(email));

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Message = $"No student record matches the email {email}.";
                return View();
            }

            var student = await response.Content.ReadFromJsonAsync<Student>();
            return View(student);
        }
        //------------------------------------------------------------
    }
}
