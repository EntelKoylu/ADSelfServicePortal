using System.Linq;
using AdSelfServicePortal.Constants;
using AdSelfServicePortal.Filters;
using AdSelfServicePortal.Helpers;
using AdSelfServicePortal.Models;
using AdSelfServicePortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdSelfServicePortal.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdService _adService;
        private const string AdminGroupName = "Domain Admins";

        public AdminController(AdService adService)
        {
            _adService = adService;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString(SessionKeys.AdminUser) != null)
                return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (!UsernameValidator.IsValid(username))
            {
                ViewBag.Error = "Geçersiz kullanıcı adı formatı.";
                return View();
            }

            string durum = _adService.ValidateUserAndGetStatus(username, password);
            if (durum == StatusCodes.Success)
            {
                if (_adService.IsUserInGroup(username, AdminGroupName))
                {
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetString(SessionKeys.AdminUser, username);
                    return RedirectToAction("Index");
                }

                ViewBag.Error = "Yetkisiz Erişim: Admin grubunda değilsiniz.";
                return View();
            }

            ViewBag.Error = "Giriş başarısız. Lütfen bilgilerinizi kontrol edin.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SessionKeys.AdminUser);
            return RedirectToAction("Login");
        }

        [ServiceFilter(typeof(AdminAuthFilter))]
        public IActionResult Index()
        {
            var model = _adService.GetDashboardStats();
            return View(model);
        }

        [ServiceFilter(typeof(AdminAuthFilter))]
        public IActionResult UserSearch(string search)
        {
            var model = _adService.GetDashboardStats();

            if (!string.IsNullOrEmpty(search))
            {
                model.SearchTerm = search;
                model.SearchResults = _adService.SearchUsers(search);
            }

            return View(model);
        }

        [ServiceFilter(typeof(AdminAuthFilter))]
        public IActionResult ActivityLogs()
        {
            var logs = _adService.GetAuditLogs();
            return View(logs);
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult GetLiveStats()
        {
            if (HttpContext.Session.GetString(SessionKeys.AdminUser) == null)
                return Unauthorized();
            return Json(_adService.GetDashboardStats());
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthFilter))]
        public IActionResult UnlockUser(string username, string returnSearch, string returnTo)
        {
            if (!UsernameValidator.IsValid(username))
            {
                TempData["Error"] = "Geçersiz kullanıcı adı.";
                return RedirectToAction("Index");
            }

            string sonuc = _adService.UnlockUserAccount(username);
            TempData[sonuc == StatusCodes.Success ? "Message" : "Error"] =
                sonuc == StatusCodes.Success ? "Kilit açıldı!" : sonuc;

            if (returnTo == "UserSearch")
                return RedirectToAction("UserSearch", new { search = returnSearch });

            return RedirectToAction("Index", new { search = returnSearch });
        }

        [ServiceFilter(typeof(AdminAuthFilter))]
        public IActionResult ResetUserPassword(string username)
        {
            if (!UsernameValidator.IsValid(username)) return RedirectToAction("Index");

            var users = _adService.SearchUsers(username);
            var currentUser = users.FirstOrDefault(u => u.Username == username);
            if (currentUser == null) return RedirectToAction("Index");

            return View(currentUser);
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthFilter))]
        public IActionResult ResetUserPassword(string username, string newPassword, bool mustChange, bool unlock)
        {
            if (!UsernameValidator.IsValid(username))
            {
                TempData["Error"] = "Geçersiz kullanıcı adı.";
                return RedirectToAction("Index");
            }

            string sonuc = _adService.ForceResetPassword(username, newPassword, mustChange, unlock);
            if (sonuc == StatusCodes.Success)
            {
                TempData["Message"] = "Şifre güncellendi.";
                return RedirectToAction("UserSearch", new { search = username });
            }

            TempData["Error"] = sonuc;
            return RedirectToAction("ResetUserPassword", new { username });
        }
    }
}
