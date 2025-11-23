using System;

namespace AdSelfServicePortal.Models
{
    public class AdUserModel
    {
        public string Username { get; set; }      // Kullanıcı Adı
        public string DisplayName { get; set; }   // Ad Soyad
        public string Email { get; set; }         // Mail
        public bool IsLocked { get; set; }        // Kilitli mi?
        public bool IsEnabled { get; set; }       // Aktif mi?
        public DateTime? LastPasswordSet { get; set; } // Şifre Değişim Tarihi
    }
}