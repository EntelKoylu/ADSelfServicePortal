using System.Collections.Generic;

namespace AdSelfServicePortal.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            LockedUsers = new List<AdUserModel>();
            SearchResults = new List<AdUserModel>();
        }

        public int TotalUsersCount { get; set; }
        public int LockedUsersCount { get; set; }
        public int DisabledUsersCount { get; set; }

        public int TotalResets { get; set; }
        public int TotalUnlocks { get; set; }
        public int TotalChanges { get; set; }

        public List<AdUserModel> LockedUsers { get; set; }
        public List<AdUserModel> SearchResults { get; set; }

        public string SearchTerm { get; set; }
    }
}
