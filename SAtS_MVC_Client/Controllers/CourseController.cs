using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;

namespace SAtS_MVC_Client.Controllers
{
    public class CourseController : Controller
    {
        private const string CourseApiUrl = "https://localhost:7147/api/Course";

        private readonly HttpClient _httpClient;

        public CourseController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> Index()
        {
            var course = await _httpClient.GetFromJsonAsync<List<Course>>
                (CourseApiUrl);

            return View(course);
        }

        //------------------------------------------------------------
        // Caleb McIntyre - 12/11/2026
        // Add, edit and delete courses. Only Admins can use these.
        //------------------------------------------------------------

        //GET: /Course/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Course());
        }

        //POST: /Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Course course)
        {
            //Server-side validation
            if (!ModelState.IsValid)
            {
                return View(course);
            }

            var response = await _httpClient.PostAsJsonAsync(CourseApiUrl, course);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await ReadApiMessage(response));
                return View(course);
            }

            TempData["Message"] = $"Added the course {course.CourseId.ToUpper()} - {course.CourseName}.";
            return RedirectToAction(nameof(Index));
        }

        //GET: /Course/Edit/FS101
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var course = await GetCourse(id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        //POST: /Course/Edit/FS101
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Course course)
        {
            if (id != course.CourseId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            var response = await _httpClient.PutAsJsonAsync(CourseApiUrl + "/" + Uri.EscapeDataString(id), course);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await ReadApiMessage(response));
                return View(course);
            }

            TempData["Message"] = $"Updated the course {course.CourseId}.";
            return RedirectToAction(nameof(Index));
        }

        //GET: /Course/Delete/FS101
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var course = await GetCourse(id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        //POST: /Course/Delete/FS101
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var response = await _httpClient.DeleteAsync(CourseApiUrl + "/" + Uri.EscapeDataString(id));
            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = $"Deleted the course {id}.";
            }
            else
            {
                TempData["Error"] = await ReadApiMessage(response);
            }

            return RedirectToAction(nameof(Index));
        }

        //Gets one course from the API, or null if it doesn't exist
        private async Task<Course?> GetCourse(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var response = await _httpClient.GetAsync(CourseApiUrl + "/" + Uri.EscapeDataString(id));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Course>();
        }

        //Reads the "message" the API sends back when something fails
        private static async Task<string> ReadApiMessage(HttpResponseMessage response)
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
                //The response wasn't JSON, so fall through to the general message
            }

            return "Something went wrong saving the course. Please try again.";
        }

        private class ApiMessage
        {
            public string? Message { get; set; }
        }
    }
}
