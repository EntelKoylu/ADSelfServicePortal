using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using AdSelfServicePortal.Models;

namespace AdSelfServicePortal.Services
{
    public class AdService
    {
        private readonly AuditService _auditService;

        public AdService(AuditService auditService)
        {
            _auditService = auditService;
        }

        // 1. LOGLARI GETİR (Admin İçin) - YENİ
        public List<AuditLogModel> GetAuditLogs()
        {
            return _auditService.GetRecentLogs();
        }

        // 2. DASHBOARD İSTATİSTİKLERİ
        public DashboardViewModel GetDashboardStats()
        {
            var model = new DashboardViewModel(); // Constructor sayesinde listeler boş olarak gelir

            // AD Verileri
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    UserPrincipal qbeUser = new UserPrincipal(context);
                    PrincipalSearcher searcher = new PrincipalSearcher(qbeUser);

                    foreach (var result in searcher.FindAll())
                    {
                        var user = result as UserPrincipal;
                        if (user != null)
                        {
                            model.TotalUsersCount++;
                            if (user.Enabled == false) model.DisabledUsersCount++;
                            if (user.IsAccountLockedOut())
                            {
                                model.LockedUsersCount++;
                                model.LockedUsers.Add(new AdUserModel
                                {
                                    Username = user.SamAccountName,
                                    DisplayName = user.DisplayName,
                                    IsLocked = true,
                                    IsEnabled = true,
                                    Email = user.EmailAddress
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            // DB Verileri
            var dbStats = _auditService.GetStats();
            model.Stats_TotalResets = dbStats.TotalPasswordResets;
            model.Stats_TotalUnlocks = dbStats.TotalAccountUnlocks;
            model.Stats_TotalChanges = dbStats.TotalPasswordChanges;

            return model;
        }

        // 3. GİRİŞ KONTROLÜ
        public string ValidateUserAndGetStatus(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return "Boş Alan";
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return "Kullanıcı Bulunamadı";
                    if (user.Enabled == false) return "Hesap Devre Dışı";
                    if (user.IsAccountLockedOut()) return "Hesap Kilitli";

                    bool sifreDogru = context.ValidateCredentials(username, password);
                    return sifreDogru ? "Basarili" : "Şifre Yanlış";
                }
            }
            catch (Exception ex) { return "Sistem Hatası: " + ex.Message; }
        }

        // 4. DİĞER İŞLEMLER
        public string CheckUserAvailability(string username)
        {
            if (string.IsNullOrEmpty(username)) return "Boş Alan";
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return "Kullanıcı Bulunamadı";
                    if (user.Enabled == false) return "Hesap Devre Dışı";
                    return "OK";
                }
            }
            catch (Exception ex) { return "Hata: " + ex.Message; }
        }

        public string ChangeUserPassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return "Kullanıcı bulunamadı!";
                    user.ChangePassword(oldPassword, newPassword);
                    user.Save();
                    _auditService.LogChange(username);
                    return "Basarili";
                }
            }
            catch (Exception ex) { return "Hata: " + ex.Message; }
        }

        public string ForceResetPassword(string username, string newPassword, bool mustChangeAtNextLogon = false, bool unlockAccount = false)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return "Kullanıcı bulunamadı!";
                    user.SetPassword(newPassword);
                    if (mustChangeAtNextLogon) user.ExpirePasswordNow();
                    if (unlockAccount && user.IsAccountLockedOut())
                    {
                        user.UnlockAccount();
                        _auditService.LogUnlock(username);
                    }
                    user.Save();
                    _auditService.LogReset(username);
                    return "Basarili";
                }
            }
            catch (Exception ex) { return "Hata: " + ex.Message; }
        }

        public string UnlockUserAccount(string username)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return "Kullanıcı bulunamadı!";
                    if (user.IsAccountLockedOut())
                    {
                        user.UnlockAccount();
                        user.Save();
                        _auditService.LogUnlock(username);
                        return "Basarili";
                    }
                    return "Hesabınız kilitli görünmüyor.";
                }
            }
            catch (Exception ex) { return "Hata: " + ex.Message; }
        }

        public List<AdUserModel> SearchUsers(string searchText)
        {
            var userList = new List<AdUserModel>();
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    UserPrincipal qbeUser = new UserPrincipal(context);
                    if (!string.IsNullOrEmpty(searchText)) qbeUser.SamAccountName = "*" + searchText + "*";
                    else return userList;

                    PrincipalSearcher searcher = new PrincipalSearcher(qbeUser);
                    foreach (var result in searcher.FindAll())
                    {
                        var user = result as UserPrincipal;
                        if (user != null)
                        {
                            userList.Add(new AdUserModel
                            {
                                Username = user.SamAccountName,
                                DisplayName = user.DisplayName ?? user.Name,
                                Email = user.EmailAddress ?? "Yok",
                                IsLocked = user.IsAccountLockedOut(),
                                IsEnabled = user.Enabled ?? false,
                                LastPasswordSet = user.LastPasswordSet
                            });
                        }
                    }
                }
            }
            catch { }
            return userList;
        }

        public bool IsUserInGroup(string username, string groupName)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    var group = GroupPrincipal.FindByIdentity(context, groupName);
                    if (user != null && group != null) return user.IsMemberOf(group);
                }
            }
            catch { return false; }
            return false;
        }
    }
}