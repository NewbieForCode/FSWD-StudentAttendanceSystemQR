using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;

using System;
using System.Linq;
using System.Net.Http.Json;

namespace SAtS_MVC_Client.Controllers
{
    public class TeacherController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(IHttpClientFactory httpClientFactory, UserManager<ApplicationUser> userManager)
        {
            _httpClient = httpClientFactory.CreateClient();
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Index()
        {
            var teachers = await _httpClient.GetFromJsonAsync<List<Teacher>>
                ("https://localhost:7147/api/Teacher") ?? new List<Teacher>();


            return View(teachers);
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Teacher());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Teacher teacher)
        {
            if (!ModelState.IsValid)
            {
                return View(teacher);
            }

            var response = await _httpClient.PostAsJsonAsync
                ("https://localhost:7147/api/Teacher", teacher);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Failed to create teacher.");
                return View(teacher);
            }
            return RedirectToAction(nameof(Index));
        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _httpClient.GetFromJsonAsync<Teacher>
                ($"https://localhost:7147/api/Teacher/{id}");

            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Teacher teacher)
        {
            if (id != teacher.TeacherId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(teacher);
            }

            var response = await _httpClient.PutAsJsonAsync
                ($"https://localhost:7147/api/Teacher/{id}", teacher);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Failed to update teacher.");
                return View(teacher);
            }

            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await _httpClient.GetFromJsonAsync<Teacher>
                ($"https://localhost:7147/api/Teacher/{id}");

            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _httpClient.DeleteAsync
                ($"https://localhost:7147/api/Teacher/{id}");
            // Fix: show whether the delete worked (the error was being lost on redirect)
            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Teacher deleted.";
            }
            else
            {
                TempData["Error"] = await ReadApiMessage(response, "Failed to delete teacher.");
            }
            return RedirectToAction(nameof(Index));
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


    }
}
