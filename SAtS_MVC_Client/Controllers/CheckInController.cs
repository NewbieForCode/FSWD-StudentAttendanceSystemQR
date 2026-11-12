using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAtS_MVC_Client.Models;

namespace SAtS_MVC_Client.Controllers
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // QR check-in:
    //  - Teachers/Admins pick a session and show its QR code on screen.
    //    The QR image comes from the external goQR.me API.
    //  - Students scan the QR code, which opens the check-in page,
    //    and press "Check in" (sent with AJAX) to record attendance.
    //------------------------------------------------------------
    public class CheckInController : Controller
    {
        private const string ApiBaseUrl = "https://localhost:7147/api/";

        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckInController(IHttpClientFactory httpClientFactory, UserManager<ApplicationUser> userManager)
        {
            _httpClient = httpClientFactory.CreateClient();
            _userManager = userManager;
        }

        //GET: /CheckIn/Sessions
        //List of sessions for a teacher to pick from
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> Sessions()
        {
            var sessions = await _httpClient.GetFromJsonAsync<List<SessionInfo>>(ApiBaseUrl + "StudentAttendance/Sessions");
            return View(sessions ?? new List<SessionInfo>());
        }

        //GET: /CheckIn/ShowQr/5
        //Shows the QR code for one session
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> ShowQr(int id)
        {
            var response = await _httpClient.GetAsync(ApiBaseUrl + "StudentAttendance/Session/" + id);
            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var session = await response.Content.ReadFromJsonAsync<SessionInfo>();
            if (session == null)
            {
                return NotFound();
            }

            //The full web address of this session's check-in page
            var checkInUrl = Url.Action("Index", "CheckIn", new { token = session.CSessionQrToken }, Request.Scheme) ?? "";

            var model = new SessionQrViewModel
            {
                Session = session,
                CheckInUrl = checkInUrl,
                //External API: goQR.me turns the check-in link into a QR code image
                QrImageUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" + Uri.EscapeDataString(checkInUrl)
            };

            return View(model);
        }

        //------------------------------------------------------------
        // Caleb McIntyre - 12/11/2026
        // Create a course session, and view who attended a session
        //------------------------------------------------------------

        //GET: /CheckIn/CreateSession
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> CreateSession()
        {
            var model = new CreateSessionViewModel();
            model.Courses = await GetCourseOptions();
            return View(model);
        }

        //POST: /CheckIn/CreateSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> CreateSession(CreateSessionViewModel model)
        {
            //Server-side validation: the date can't be in the past
            if (model.SessionDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError(nameof(model.SessionDate), "The session date can't be in the past.");
            }

            if (!ModelState.IsValid)
            {
                model.Courses = await GetCourseOptions();
                return View(model);
            }

            var response = await _httpClient.PostAsJsonAsync(ApiBaseUrl + "StudentAttendance/Session",
                new { CourseId = model.CourseId, SessionDate = model.SessionDate });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await ReadApiMessage(response));
                model.Courses = await GetCourseOptions();
                return View(model);
            }

            var session = await response.Content.ReadFromJsonAsync<SessionInfo>();
            TempData["Message"] = "Session created.";

            //Go straight to the new session's QR code
            return RedirectToAction(nameof(ShowQr), new { id = session?.CSessionId });
        }

        //GET: /CheckIn/SessionAttendance/5
        //The students who checked in to one session
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> SessionAttendance(int id)
        {
            var sessionResponse = await _httpClient.GetAsync(ApiBaseUrl + "StudentAttendance/Session/" + id);
            if (!sessionResponse.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var session = await sessionResponse.Content.ReadFromJsonAsync<SessionInfo>();
            var records = await _httpClient.GetFromJsonAsync<List<AttendanceRecordRow>>
                (ApiBaseUrl + "StudentAttendance/Session/" + id + "/Records");

            var model = new SessionAttendanceViewModel
            {
                Session = session ?? new SessionInfo(),
                Records = records ?? new List<AttendanceRecordRow>()
            };

            return View(model);
        }

        //The courses for the Create Session drop-down
        private async Task<List<SelectListItem>> GetCourseOptions()
        {
            var courses = await _httpClient.GetFromJsonAsync<List<Course>>(ApiBaseUrl + "Course") ?? new List<Course>();

            return courses
                .OrderBy(c => c.CourseId)
                .Select(c => new SelectListItem { Value = c.CourseId, Text = c.CourseId + " - " + c.CourseName })
                .ToList();
        }

        //Reads the "message" the API sends back when something fails
        private static async Task<string> ReadApiMessage(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<CheckInResult>();
                if (!string.IsNullOrWhiteSpace(body?.Message))
                {
                    return body.Message;
                }
            }
            catch
            {
                //The response wasn't JSON, so fall through to the general message
            }

            return "Something went wrong. Please try again.";
        }
        //------------------------------------------------------------

        //GET: /CheckIn?token=abc
        //The page a student lands on after scanning the QR code
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Message = "No session was given. Please scan the QR code again.";
                return View();
            }

            var response = await _httpClient.GetAsync(ApiBaseUrl + "StudentAttendance/SessionByToken?token=" + Uri.EscapeDataString(token));
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Message = "This QR code doesn't match any session.";
                return View();
            }

            var session = await response.Content.ReadFromJsonAsync<SessionInfo>();
            return View(session);
        }

        //POST: /CheckIn/Submit
        //Called by AJAX from the check-in page. Returns JSON: { success, message }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new CheckInResult { Success = false, Message = "No session was given." });
            }

            //Find the student record that belongs to the signed-in account
            var student = await FindSignedInStudent();
            if (student == null)
            {
                return BadRequest(new CheckInResult { Success = false, Message = "Your account isn't linked to a student record." });
            }

            //Ask the API to record the attendance
            var response = await _httpClient.PostAsJsonAsync(ApiBaseUrl + "StudentAttendance/CheckIn",
                new { StudentId = student.StudentId, Token = token });

            var result = await response.Content.ReadFromJsonAsync<CheckInResult>()
                         ?? new CheckInResult { Success = false, Message = "Something went wrong. Please try again." };

            //Pass the API's status code through, so the page knows if it worked
            return StatusCode((int)response.StatusCode, result);
        }

        //Looks up the student whose email matches the signed-in account
        private async Task<Student?> FindSignedInStudent()
        {
            var user = await _userManager.GetUserAsync(User);
            var email = user?.Email ?? user?.UserName ?? "";

            var response = await _httpClient.GetAsync(ApiBaseUrl + "Student/ByEmail?email=" + Uri.EscapeDataString(email));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Student>();
        }
    }
}
