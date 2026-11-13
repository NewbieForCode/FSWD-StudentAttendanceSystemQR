using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;

using System;
using System.Linq;
using System.Net.Http.Json;

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
        public async Task<IActionResult> Index(string? searchInput)
        {
            var students = await _httpClient.GetFromJsonAsync<List<Student>>
                ("https://localhost:7147/api/Student") ?? new List<Student>();

            if (!string.IsNullOrEmpty(searchInput))
            {
                students = students.Where(s =>
                    s.StudentFName.Contains(searchInput, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentLName.Contains(searchInput, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentSuburb.Contains(searchInput, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentCity.Contains(searchInput, StringComparison.OrdinalIgnoreCase) 
                ).ToList();
            }

            return View(students ?? new List<Student>());
        }

        // Delete, Edit and Create new student records
        //---Create new student record
        [Authorize(Roles = "Admin, Teacher")]
        public IActionResult Create()
        {
            return View(new Student());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Create(Student student)
        {
            if (!ModelState.IsValid)
            {
                return View(student);
            }

            var response = await _httpClient.PostAsJsonAsync
                ("https://localhost:7147/api/Student", student);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Failed to create student.");
                return View(student);
            }
            return RedirectToAction(nameof(Index));
        }


        //----Edit student record
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _httpClient.GetFromJsonAsync<Student>
                ($"https://localhost:7147/api/Student/{id}");

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(student);
            }

            var response = await _httpClient.PutAsJsonAsync
                ($"https://localhost:7147/api/Student/{id}", student);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Failed to update student.");
                return View(student);
            }

            return RedirectToAction(nameof(Index));
        }


        //----Delete student record
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _httpClient.GetFromJsonAsync<Student>
                ($"https://localhost:7147/api/Student/{id}");

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _httpClient.DeleteAsync
                ($"https://localhost:7147/api/Student/{id}");
            // Fix: show whether the delete worked (the error was being lost on redirect)
            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Student deleted.";
            }
            else
            {
                TempData["Error"] = await ReadApiMessage(response, "Failed to delete student.");
            }
            return RedirectToAction(nameof(Index));
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

        // Fix: reads the "message" the API sends back when something fails
        private static async Task<string> ReadApiMessage(HttpResponseMessage response, string fallback)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<ApiMessage>();
                if (!string.IsNullOrWhiteSpace(body?.Message))
                {
                    return body.Message;
                }
            }
            catch
            {
                //The response wasn't JSON, so use the general message
            }

            return fallback;
        }

        private class ApiMessage
        {
            public string? Message { get; set; }
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
