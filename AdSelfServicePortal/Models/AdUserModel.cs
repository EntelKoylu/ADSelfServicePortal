using System;

namespace AdSelfServicePortal.Models
{
    public class AdUserModel
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool IsLocked { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime? LastPasswordSet { get; set; }
    }
}
