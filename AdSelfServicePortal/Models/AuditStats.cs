namespace AdSelfServicePortal.Models
{
    public class AuditStats
    {
        public int TotalPasswordResets { get; set; }
        public int TotalAccountUnlocks { get; set; }
        public int TotalPasswordChanges { get; set; }
    }
}