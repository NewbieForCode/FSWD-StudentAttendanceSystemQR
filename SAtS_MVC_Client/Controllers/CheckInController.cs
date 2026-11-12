using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
