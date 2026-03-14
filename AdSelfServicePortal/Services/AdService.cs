using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using AdSelfServicePortal.Constants;
using AdSelfServicePortal.Models;
using Serilog;

namespace AdSelfServicePortal.Services
{
    public class AdService
    {
        private readonly AuditService _auditService;

        public AdService(AuditService auditService)
        {
            _auditService = auditService;
        }

        public List<AuditLogModel> GetAuditLogs()
        {
            return _auditService.GetRecentLogs();
        }

        public DashboardViewModel GetDashboardStats()
        {
            var model = new DashboardViewModel();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var qbeUser = new UserPrincipal(context);
                    var searcher = new PrincipalSearcher(qbeUser);

                    foreach (var result in searcher.FindAll())
                    {
                        if (result is UserPrincipal user)
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
            catch (Exception ex)
            {
                Log.Error(ex, "Dashboard istatistikleri alınırken hata oluştu");
            }

            var dbStats = _auditService.GetStats();
            model.TotalResets = dbStats.TotalPasswordResets;
            model.TotalUnlocks = dbStats.TotalAccountUnlocks;
            model.TotalChanges = dbStats.TotalPasswordChanges;

            return model;
        }

        public string ValidateUserAndGetStatus(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return StatusCodes.EmptyField;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return StatusCodes.UserNotFound;
                    if (user.Enabled == false) return StatusCodes.AccountDisabled;
                    if (user.IsAccountLockedOut()) return StatusCodes.AccountLocked;

                    bool isValid = context.ValidateCredentials(username, password);
                    return isValid ? StatusCodes.Success : StatusCodes.WrongPassword;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Kullanıcı doğrulama sırasında hata: {Username}", username);
                return "Sistem hatası oluştu. Lütfen tekrar deneyin.";
            }
        }

        public string CheckUserAvailability(string username)
        {
            if (string.IsNullOrEmpty(username)) return StatusCodes.EmptyField;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return StatusCodes.UserNotFound;
                    if (user.Enabled == false) return StatusCodes.AccountDisabled;
                    return StatusCodes.Available;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Kullanıcı durumu kontrol edilirken hata: {Username}", username);
                return "Sistem hatası oluştu. Lütfen tekrar deneyin.";
            }
        }

        public string ChangeUserPassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return StatusCodes.UserNotFound;
                    user.ChangePassword(oldPassword, newPassword);
                    user.Save();
                    _auditService.LogChange(username);
                    return StatusCodes.Success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Şifre değiştirme sırasında hata: {Username}", username);
                return "Şifre değiştirilemedi. Şifre politikasına uygun olduğundan emin olun.";
            }
        }

        public string ForceResetPassword(string username, string newPassword, bool mustChangeAtNextLogon = false, bool unlockAccount = false)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return StatusCodes.UserNotFound;
                    user.SetPassword(newPassword);
                    if (mustChangeAtNextLogon) user.ExpirePasswordNow();
                    if (unlockAccount && user.IsAccountLockedOut())
                    {
                        user.UnlockAccount();
                        _auditService.LogUnlock(username);
                    }
                    user.Save();
                    _auditService.LogReset(username);
                    return StatusCodes.Success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Şifre sıfırlama sırasında hata: {Username}", username);
                return "Şifre sıfırlanamadı. Şifre politikasına uygun olduğundan emin olun.";
            }
        }

        public string UnlockUserAccount(string username)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user == null) return StatusCodes.UserNotFound;
                    if (user.IsAccountLockedOut())
                    {
                        user.UnlockAccount();
                        user.Save();
                        _auditService.LogUnlock(username);
                        return StatusCodes.Success;
                    }
                    return StatusCodes.AccountNotLocked;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Hesap kilidi açılırken hata: {Username}", username);
                return "Hesap kilidi açılamadı. Lütfen tekrar deneyin.";
            }
        }

        public List<AdUserModel> SearchUsers(string searchText)
        {
            var userList = new List<AdUserModel>();
            if (string.IsNullOrEmpty(searchText)) return userList;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var qbeUser = new UserPrincipal(context) { SamAccountName = "*" + searchText + "*" };
                    var searcher = new PrincipalSearcher(qbeUser);

                    foreach (var result in searcher.FindAll())
                    {
                        if (result is UserPrincipal user)
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
            catch (Exception ex)
            {
                Log.Error(ex, "Kullanıcı arama sırasında hata: {SearchText}", searchText);
            }
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
            catch (Exception ex)
            {
                Log.Error(ex, "Grup üyeliği kontrol edilirken hata: {Username}, {GroupName}", username, groupName);
                return false;
            }
            return false;
        }
    }
}
