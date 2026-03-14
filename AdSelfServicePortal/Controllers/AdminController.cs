using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdSelfServicePortal.Models;
using AdSelfServicePortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdSelfServicePortal.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdService _adService;
        private const string AdminSessionKey = "AdminUser";
        private const string AdminGroupName = "Domain Admins";

        // Kullanıcı adı doğrulama: sadece alfanümerik, nokta, tire ve alt çizgi
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9._\-]{1,64}$", RegexOptions.Compiled);

        public AdminController(AdService adService)
        {
            _adService = adService;
        }

        private bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username) && UsernameRegex.IsMatch(username);
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) != null) return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (!IsValidUsername(username))
            {
                ViewBag.Error = "Geçersiz kullanıcı adı formatı.";
                return View();
            }

            string durum = _adService.ValidateUserAndGetStatus(username, password);
            if (durum == "Basarili")
            {
                if (_adService.IsUserInGroup(username, AdminGroupName))
                {
                    // Oturum sabitleme saldırısını önlemek için yeni oturum oluştur
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetString(AdminSessionKey, username);
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Yetkisiz Erişim: Admin grubunda değilsiniz.";
                    return View();
                }
            }
            ViewBag.Error = "Giriş başarısız. Lütfen bilgilerinizi kontrol edin.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(AdminSessionKey);
            return RedirectToAction("Login");
        }

        // --- MENÜ 1: ANA SAYFA (DASHBOARD) ---
        public IActionResult Index(string search)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return RedirectToAction("Login");

            // Şimdilik burası da arama yapıyor ama ilerde sadece özet göstereceğiz
            var model = new DashboardViewModel();

            // Dashboard verilerini çek
            var stats = _adService.GetDashboardStats();
            model.TotalUsersCount = stats.TotalUsersCount;
            model.LockedUsersCount = stats.LockedUsersCount;
            model.DisabledUsersCount = stats.DisabledUsersCount;
            model.Stats_TotalResets = stats.Stats_TotalResets;
            model.Stats_TotalUnlocks = stats.Stats_TotalUnlocks;
            model.Stats_TotalChanges = stats.Stats_TotalChanges;
            model.LockedUsers = stats.LockedUsers;

            if (!string.IsNullOrEmpty(search))
            {
                model.SearchTerm = search;
                model.SearchResults = _adService.SearchUsers(search);
            }
            return View(model);
        }

        // --- MENÜ 2: KULLANICI ARAMA (YENİ SAYFA) ---
        public IActionResult UserSearch(string search)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return RedirectToAction("Login");

            var model = new DashboardViewModel();

            // Üstteki kartlar yine görünsün diye verileri çekiyoruz
            var stats = _adService.GetDashboardStats();
            model.TotalUsersCount = stats.TotalUsersCount;
            model.LockedUsersCount = stats.LockedUsersCount;
            model.DisabledUsersCount = stats.DisabledUsersCount;
            model.Stats_TotalResets = stats.Stats_TotalResets;
            model.Stats_TotalUnlocks = stats.Stats_TotalUnlocks;
            model.Stats_TotalChanges = stats.Stats_TotalChanges;
            model.LockedUsers = stats.LockedUsers;

            if (!string.IsNullOrEmpty(search))
            {
                model.SearchTerm = search;
                model.SearchResults = _adService.SearchUsers(search);
            }
            return View(model);
        }

        // --- MENÜ 3: AKTİVİTE GEÇMİŞİ ---
        public IActionResult ActivityLogs()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return RedirectToAction("Login");
            var logs = _adService.GetAuditLogs();
            return View(logs);
        }

        [HttpGet]
        public IActionResult GetLiveStats()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return Unauthorized();
            return Json(_adService.GetDashboardStats());
        }

        [HttpPost]
        public IActionResult UnlockUser(string username, string returnSearch, string returnTo)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return RedirectToAction("Login");

            if (!IsValidUsername(username))
            {
                TempData["Error"] = "Geçersiz kullanıcı adı.";
                return RedirectToAction("Index");
            }

            string sonuc = _adService.UnlockUserAccount(username);
            TempData[sonuc == "Basarili" ? "Message" : "Error"] = sonuc == "Basarili" ? "Kilit açıldı!" : sonuc;

            // Güvenli yönlendirme - sadece bilinen action'lara izin ver (Open Redirect koruması)
            if (returnTo == "UserSearch") return RedirectToAction("UserSearch", new { search = returnSearch });

            return RedirectToAction("Index", new { search = returnSearch });
        }

        public IActionResult ResetUserPassword(string username)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return RedirectToAction("Login");
            if (!IsValidUsername(username)) return RedirectToAction("Index");

            var users = _adService.SearchUsers(username);
            var currentUser = users.FirstOrDefault(u => u.Username == username);
            if (currentUser == null) return RedirectToAction("Index");
            return View(currentUser);
        }

        [HttpPost]
        public IActionResult ResetUserPassword(string username, string newPassword, bool mustChange, bool unlock)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null) return RedirectToAction("Login");
            if (!IsValidUsername(username))
            {
                TempData["Error"] = "Geçersiz kullanıcı adı.";
                return RedirectToAction("Index");
            }

            string sonuc = _adService.ForceResetPassword(username, newPassword, mustChange, unlock);
            if (sonuc == "Basarili")
            {
                TempData["Message"] = "Şifre güncellendi.";
                return RedirectToAction("UserSearch", new { search = username });
            }
            TempData["Error"] = sonuc;
            return RedirectToAction("ResetUserPassword", new { username = username });
        }
    }
}