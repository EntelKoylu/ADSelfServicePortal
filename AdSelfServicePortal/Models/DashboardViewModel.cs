using System.Collections.Generic;

namespace AdSelfServicePortal.Models
{
    public class DashboardViewModel
    {
        // --- KURUCU METOT (Constructor) --- 
        // Bu sınıf new DashboardViewModel() dendiği an çalışır ve listeleri hazırlar.
        public DashboardViewModel()
        {
            LockedUsers = new List<AdUserModel>();
            ExpiringUsers = new List<AdUserModel>();
            SearchResults = new List<AdUserModel>();
        }

        // --- BÖLÜM 1: ANLIK AD DURUMU ---
        public int TotalUsersCount { get; set; }
        public int LockedUsersCount { get; set; }
        public int DisabledUsersCount { get; set; }

        // --- BÖLÜM 2: GEÇMİŞ İSTATİSTİKLER ---
        public int Stats_TotalResets { get; set; }
        public int Stats_TotalUnlocks { get; set; }
        public int Stats_TotalChanges { get; set; }

        // --- BÖLÜM 3: LİSTELER ---
        public List<AdUserModel> LockedUsers { get; set; }
        public List<AdUserModel> ExpiringUsers { get; set; }
        public List<AdUserModel> SearchResults { get; set; }

        public string SearchTerm { get; set; }
    }
}