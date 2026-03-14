using System;
using AdSelfServicePortal.Constants;
using AdSelfServicePortal.Helpers;
using AdSelfServicePortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdSelfServicePortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly AdService _adService;

        public HomeController(AdService adService)
        {
            _adService = adService;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString(SessionKeys.AdminUser) != null)
                return RedirectToAction("Index", "Admin");

            if (HttpContext.Session.GetString(SessionKeys.User) != null)
                return RedirectToAction("ChangePassword");

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (!UsernameValidator.IsValid(username))
            {
                ViewBag.Message = "Geçersiz kullanıcı adı formatı.";
                ViewBag.Renk = "warning";
                return View("Index");
            }

            string durum = _adService.ValidateUserAndGetStatus(username, password);

            if (durum == StatusCodes.Success)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString(SessionKeys.User, username);
                return RedirectToAction("ChangePassword");
            }

            ViewBag.Message = durum switch
            {
                StatusCodes.EmptyField => "Alanları doldurunuz.",
                StatusCodes.UserNotFound => "Kullanıcı bulunamadı.",
                StatusCodes.AccountDisabled => "ParamTech IT Ekibi ile görüşülmesi gerekmektedir.",
                StatusCodes.AccountLocked => "Hesabınız kilitlenmiş! Aşağıdaki buton ile kilidi açabilirsiniz.",
                StatusCodes.WrongPassword => "Şifre hatalı.",
                _ => "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin."
            };

            ViewBag.Renk = durum is StatusCodes.EmptyField or StatusCodes.AccountNotLocked
                ? "warning" : "danger";

            return View("Index");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult ChangePassword()
        {
            var user = HttpContext.Session.GetString(SessionKeys.User);
            if (user == null) return RedirectToAction("Index");
            ViewBag.PreFilledUsername = user;
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string username, string oldPassword, string newPassword)
        {
            string sonuc = _adService.ChangeUserPassword(username, oldPassword, newPassword);
            SetViewMessage(sonuc, "Şifreniz başarıyla güncellendi!");
            return View();
        }

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ForgotPassword(string username)
        {
            if (!UsernameValidator.IsValid(username))
            {
                ViewBag.Message = "Geçersiz kullanıcı adı formatı.";
                ViewBag.Renk = "danger";
                return View();
            }

            string kontrol = _adService.CheckUserAvailability(username);
            if (kontrol == StatusCodes.Available)
            {
                var token = Guid.NewGuid().ToString();
                HttpContext.Session.SetString(SessionKeys.ResetToken, token);
                HttpContext.Session.SetString(SessionKeys.ResetUsername, username);
                return RedirectToAction("SetNewPassword", new { token });
            }

            ViewBag.Message = kontrol switch
            {
                StatusCodes.UserNotFound => "Böyle bir kullanıcı hesabı bulunamadı.",
                StatusCodes.AccountDisabled => "Bu hesap devre dışı bırakılmıştır.",
                _ => "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin."
            };

            ViewBag.Renk = "danger";
            return View();
        }

        public IActionResult SetNewPassword(string token)
        {
            var sessionToken = HttpContext.Session.GetString(SessionKeys.ResetToken);
            var sessionUsername = HttpContext.Session.GetString(SessionKeys.ResetUsername);

            if (!IsValidResetToken(token, sessionToken, sessionUsername))
                return RedirectToAction("ForgotPassword");

            ViewBag.Username = sessionUsername;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult SetNewPassword(string token, string newPassword)
        {
            var sessionToken = HttpContext.Session.GetString(SessionKeys.ResetToken);
            var sessionUsername = HttpContext.Session.GetString(SessionKeys.ResetUsername);

            if (!IsValidResetToken(token, sessionToken, sessionUsername))
                return RedirectToAction("ForgotPassword");

            string sonuc = _adService.ForceResetPassword(sessionUsername, newPassword);
            if (sonuc == StatusCodes.Success)
            {
                HttpContext.Session.Remove(SessionKeys.ResetToken);
                HttpContext.Session.Remove(SessionKeys.ResetUsername);
                ViewBag.Message = "Şifreniz başarıyla sıfırlandı! Giriş yapabilirsiniz.";
                ViewBag.Renk = "success";
            }
            else
            {
                ViewBag.Message = sonuc;
                ViewBag.Renk = "danger";
            }

            ViewBag.Username = sessionUsername;
            ViewBag.Token = token;
            return View();
        }

        public IActionResult UnlockAccount() => View();

        [HttpPost]
        public IActionResult UnlockAccount(string username)
        {
            if (!UsernameValidator.IsValid(username))
            {
                ViewBag.Message = "Geçersiz kullanıcı adı formatı.";
                ViewBag.Renk = "warning";
                return View();
            }

            string sonuc = _adService.UnlockUserAccount(username);
            SetViewMessage(sonuc, "Hesap kilidiniz kaldırıldı!");
            return View();
        }

        public IActionResult Privacy() => View();

        private void SetViewMessage(string result, string successMsg)
        {
            if (result == StatusCodes.Success)
            {
                ViewBag.Message = successMsg;
                ViewBag.Renk = "success";
            }
            else
            {
                ViewBag.Message = result;
                ViewBag.Renk = "danger";
            }
        }

        private static bool IsValidResetToken(string token, string sessionToken, string sessionUsername)
        {
            return !string.IsNullOrEmpty(token) &&
                   !string.IsNullOrEmpty(sessionToken) &&
                   !string.IsNullOrEmpty(sessionUsername) &&
                   token == sessionToken;
        }
    }
}
