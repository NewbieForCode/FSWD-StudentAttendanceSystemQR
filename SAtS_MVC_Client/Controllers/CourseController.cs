using Microsoft.AspNetCore.Mvc;
using SAtS_MVC_Client.Models;

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

            return View(course);
        }
    }
}
