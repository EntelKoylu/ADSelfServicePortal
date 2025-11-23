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

        // --- DÜZELTİLEN KISIM BURASI ---
        public IActionResult Index()
        {
            // 1. Eğer ADMIN giriş yapmışsa -> Admin Paneline yönlendir
            if (HttpContext.Session.GetString("AdminUser") != null)
            {
                return RedirectToAction("Index", "Admin");
            }

            // 2. Eğer NORMAL KULLANICI giriş yapmışsa -> Şifre Ekranına yönlendir
            if (HttpContext.Session.GetString("User") != null)
            {
                return RedirectToAction("ChangePassword");
            }

            // 3. Kimse yoksa -> Giriş Formunu göster
            return View();
        }
        // -------------------------------

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            string durum = _adService.ValidateUserAndGetStatus(username, password);

            if (durum == "Basarili")
            {
                // Burada normal kullanıcı oturumu açılıyor
                HttpContext.Session.SetString("User", username);
                return RedirectToAction("ChangePassword");
            }

            if (durum == "Boş Alan") ViewBag.Message = "Alanları doldurunuz.";
            else if (durum == "Kullanıcı Bulunamadı") ViewBag.Message = "Kullanıcı bulunamadı.";
            else if (durum == "Hesap Devre Dışı") ViewBag.Message = "ParamTech IT Ekibi ile görüşülmesi gerekmektedir.";
            else if (durum == "Hesap Kilitli") ViewBag.Message = "Hesabınız kilitlenmiş! Aşağıdaki buton ile kilidi açabilirsiniz.";
            else if (durum == "Şifre Yanlış") ViewBag.Message = "Şifre hatalı.";
            else ViewBag.Message = "Hata: " + durum;

            ViewBag.Renk = (durum == "Boş Alan" || durum == "Hesabınız kilitli görünmüyor.") ? "warning" : "danger";
            return View("Index");
        }

        public IActionResult Logout()
        {
            // Hem kullanıcı hem admin oturumunu temizle (Garanti olsun)
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult ChangePassword()
        {
            var user = HttpContext.Session.GetString("User");
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

        public IActionResult ForgotPassword() { return View(); }

        [HttpPost]
        public IActionResult ForgotPassword(string username)
        {
            string kontrol = _adService.CheckUserAvailability(username);
            if (kontrol == "OK") return RedirectToAction("SetNewPassword", new { username = username });

            if (kontrol == "Kullanıcı Bulunamadı") ViewBag.Message = "Böyle bir kullanıcı hesabı bulunamadı.";
            else if (kontrol == "Hesap Devre Dışı") ViewBag.Message = "Bu hesap devre dışı bırakılmıştır.";
            else ViewBag.Message = "Hata: " + kontrol;

            ViewBag.Renk = "danger";
            return View();
        }

        public IActionResult SetNewPassword(string username)
        {
            ViewBag.Username = username;
            return View();
        }

        [HttpPost]
        public IActionResult SetNewPassword(string username, string newPassword)
        {
            string sonuc = _adService.ForceResetPassword(username, newPassword);
            if (sonuc == "Basarili")
            {
                ViewBag.Message = "Şifreniz başarıyla sıfırlandı! Giriş yapabilirsiniz.";
                ViewBag.Renk = "success";
            }
            else
            {
                ViewBag.Message = sonuc; ViewBag.Renk = "danger";
            }
            ViewBag.Username = username;
            return View();
        }

        public IActionResult UnlockAccount() { return View(); }

        [HttpPost]
        public IActionResult UnlockAccount(string username)
        {
            string sonuc = _adService.UnlockUserAccount(username);
            SetViewMessage(sonuc, "Hesap kilidiniz kaldırıldı!");
            return View();
        }

        public IActionResult Privacy() { return View(); }

        private void SetViewMessage(string result, string successMsg)
        {
            if (result == "Basarili") { ViewBag.Message = successMsg; ViewBag.Renk = "success"; }
            else { ViewBag.Message = result; ViewBag.Renk = "danger"; }
        }
    }
}