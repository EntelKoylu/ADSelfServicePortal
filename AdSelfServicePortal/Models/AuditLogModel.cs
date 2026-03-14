using System;

namespace AdSelfServicePortal.Models
{
    public class AuditLogModel
    {
        public int Id { get; set; }
        public string ActionType { get; set; }
        public string Username { get; set; }
        public DateTime ActionDate { get; set; }
    }
}
