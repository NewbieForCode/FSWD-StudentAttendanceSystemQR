using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;
using System.Net.Http.Json;

namespace SAtS_MVC_Client.Controllers
{
    public class CourseController : Controller
    {
        private readonly HttpClient _httpClient;

        public CourseController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> Index()
        {
            var course = await _httpClient.GetFromJsonAsync<List<Course>>
                ("https://localhost:7147/api/Course");

            var sessions = await _httpClient.GetFromJsonAsync<List<CourseSession>>
                ("https://localhost:7147/api/CourseSession");

            var viewModel = course.Select(course => new CourseViewModel
            {
                Course = course,

                Sessions = sessions
                    .Where(session => session.CourseId == course.CourseId)
                    .ToList()

            }).ToList();

            return View(viewModel);
        }
    }
}
