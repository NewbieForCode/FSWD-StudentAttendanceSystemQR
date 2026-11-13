using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAtS_MVC_Client.Models;
using System.Net.Http.Json;

namespace SAtS_MVC_Client.Controllers
{
    public class CourseController : Controller
    {
        private const string CourseApiUrl = "api/Course";

        private readonly HttpClient _httpClient;

        public CourseController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("SAtSApi");
        }

        public async Task<IActionResult> Index(string? courseFilter)
        {
            var course = await _httpClient.GetFromJsonAsync<List<Course>>
                (CourseApiUrl) ?? new List<Course>();


            var teachers = await _httpClient.GetFromJsonAsync<List<Teacher>>
                ("api/Teacher") ?? new List<Teacher>();

            var viewModel = course.Select(course => new CourseViewModel
            {
                Course = course,

                Sessions = new List<CourseSession>(),

                TeacherName = teachers 
                    .Where(t => t.TeacherId == course.TeacherId)
                    .Select(t => t.TeacherFname + " " + t.TeacherLname)
                    .FirstOrDefault() ?? "Unassigned"

            }).ToList();


            //-- filter by ids
            if (!string.IsNullOrWhiteSpace(courseFilter))
            {
                viewModel = viewModel.Where(item => item.Course.CourseId.Contains(courseFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            //-- add to filter with new ids
            var coursePrefixes = course 
                .Select(c => new string(c.CourseId.TakeWhile(char.IsLetter).ToArray()))
                .Where(prefix => !string.IsNullOrEmpty(prefix))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(prefix => prefix)
                .ToList();

            ViewBag.CoursePrefixes = coursePrefixes;
            return View(viewModel);
        }


        //--- add teacher dropdown to create and edit
        private async Task LoadTeachers(int? selectedTeacherId = null)
        {
            var teachers = await _httpClient.GetFromJsonAsync<List<Teacher>>
                ("api/Teacher") ?? new List<Teacher>();

            ViewBag.Teachers = new SelectList(teachers.Select(t => new 
            { 
                t.TeacherId,
                FullName = t.TeacherFname + " " + t.TeacherLname
            }),
                "TeacherId",
                "FullName",
                selectedTeacherId
            );
        }


        // Caleb McIntyre - 13/11/2026: session Edit/Delete moved to CheckInController (Course Sessions page)

        //------------------------------------------------------------
        // Caleb McIntyre - 12/11/2026
        // Add, edit and delete courses. Only Admins can use these.
        //------------------------------------------------------------

        //GET: /Course/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadTeachers(); //New Addition 
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
                await LoadTeachers(course.TeacherId); //New Addition
                return View(course);
            }

            var response = await _httpClient.PostAsJsonAsync(CourseApiUrl, course);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await ReadApiMessage(response));
                await LoadTeachers(course.TeacherId); // Fix: the page needs its teacher list again
                return View(course);
            }

            TempData["Message"] = $"Added the course {course.CourseId.ToUpper()} - {course.CourseName}.";
            await LoadTeachers(course.TeacherId);
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

            await LoadTeachers(course.TeacherId);
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
                await LoadTeachers(course.TeacherId);
                return View(course);
            }

            var response = await _httpClient.PutAsJsonAsync(CourseApiUrl + "/" + Uri.EscapeDataString(id), course);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await ReadApiMessage(response));
                await LoadTeachers(course.TeacherId); // Fix: the page needs its teacher list again
                return View(course);
            }

            TempData["Message"] = $"Updated the course {course.CourseId}.";
            await LoadTeachers(course.TeacherId);

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
