using System;
using System.Collections.Generic;
using AdSelfServicePortal.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;

namespace AdSelfServicePortal.Services
{
    public class AuditService
    {
        private readonly string _connectionString;

        public AuditService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgreConnection");
        }

        private void LogToDb(string actionType, string username)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO AuditLogs (ActionType, Username, ActionDate) VALUES (@type, @user, @date)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("type", actionType);
                        cmd.Parameters.AddWithValue("user", username ?? "Bilinmiyor");
                        cmd.Parameters.AddWithValue("date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex, "Denetim kaydı yazılırken hata: {ActionType}, {Username}", actionType, username); }
        }

        private int GetCount(string actionType)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM AuditLogs WHERE ActionType = @type";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("type", actionType);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "İstatistik sayısı alınırken hata: {ActionType}", actionType);
                return 0;
            }
        }

        // --- YENİ: SON İŞLEMLERİ GETİR (TABLO İÇİN) ---
        public List<AuditLogModel> GetRecentLogs()
        {
            var logs = new List<AuditLogModel>();
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM AuditLogs ORDER BY ActionDate DESC LIMIT 100";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new AuditLogModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ActionType = reader["ActionType"].ToString(),
                                Username = reader["Username"].ToString(),
                                ActionDate = Convert.ToDateTime(reader["ActionDate"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Denetim logları alınırken hata");
            }
            return logs;
        }

        public void LogReset(string username) => LogToDb("Reset", username);
        public void LogUnlock(string username) => LogToDb("Unlock", username);
        public void LogChange(string username) => LogToDb("Change", username);

        public AuditStats GetStats()
        {
            return new AuditStats
            {
                TotalPasswordResets = GetCount("Reset"),
                TotalAccountUnlocks = GetCount("Unlock"),
                TotalPasswordChanges = GetCount("Change")
            };
        }
    }
}