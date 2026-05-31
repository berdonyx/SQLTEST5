using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ManufacturingApp.Models;

namespace ManufacturingApp.Helpers
{
    public static class DatabaseHelper
    {
        // !! Измените строку подключения под ваш SQL Server !!
        private static readonly string ConnectionString =
            @"Server=.\SQLEXPRESS;Database=ManufacturingDB;Integrated Security=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        // ─────────────────────────── USERS ───────────────────────────

        public static User GetUserByLogin(string login)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    @"SELECT u.UserID, u.Login, u.PasswordHash, u.RoleID,
                             r.RoleName, u.IsBlocked, u.FailedAttempts
                      FROM   Users u
                      JOIN   Roles r ON r.RoleID = u.RoleID
                      WHERE  u.Login = @login", conn);
                cmd.Parameters.AddWithValue("@login", login);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    return new User
                    {
                        UserID         = rd.GetInt32(0),
                        Login          = rd.GetString(1),
                        PasswordHash   = rd.GetString(2),
                        RoleID         = rd.GetInt32(3),
                        RoleName       = rd.GetString(4),
                        IsBlocked      = rd.GetBoolean(5),
                        FailedAttempts = rd.GetInt32(6)
                    };
                }
            }
        }

        public static void IncrementFailedAttempts(int userId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE Users SET FailedAttempts = FailedAttempts + 1 WHERE UserID = @id",
                    conn);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void BlockUser(int userId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE Users SET IsBlocked = 1 WHERE UserID = @id", conn);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void ResetFailedAttempts(int userId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE Users SET FailedAttempts = 0 WHERE UserID = @id", conn);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    @"SELECT u.UserID, u.Login, u.PasswordHash, u.RoleID,
                             r.RoleName, u.IsBlocked, u.FailedAttempts
                      FROM   Users u
                      JOIN   Roles r ON r.RoleID = u.RoleID
                      ORDER BY u.Login", conn);

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new User
                        {
                            UserID         = rd.GetInt32(0),
                            Login          = rd.GetString(1),
                            PasswordHash   = rd.GetString(2),
                            RoleID         = rd.GetInt32(3),
                            RoleName       = rd.GetString(4),
                            IsBlocked      = rd.GetBoolean(5),
                            FailedAttempts = rd.GetInt32(6)
                        });
                    }
                }
            }
            return list;
        }

        public static bool LoginExists(string login, int excludeUserId = -1)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Login = @login AND UserID <> @excl",
                    conn);
                cmd.Parameters.AddWithValue("@login", login);
                cmd.Parameters.AddWithValue("@excl",  excludeUserId);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static void AddUser(string login, string passwordHash, int roleId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    @"INSERT INTO Users (Login, PasswordHash, RoleID, IsBlocked, FailedAttempts)
                      VALUES (@login, @pw, @role, 0, 0)", conn);
                cmd.Parameters.AddWithValue("@login", login);
                cmd.Parameters.AddWithValue("@pw",    passwordHash);
                cmd.Parameters.AddWithValue("@role",  roleId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateUser(int userId, string login, string passwordHash,
                                      int roleId, bool isBlocked)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string pwPart = string.IsNullOrEmpty(passwordHash)
                    ? "" : ", PasswordHash = @pw";
                var cmd = new SqlCommand(
                    $@"UPDATE Users
                       SET Login = @login, RoleID = @role, IsBlocked = @blocked,
                           FailedAttempts = CASE WHEN @blocked = 0 THEN 0 ELSE FailedAttempts END
                           {pwPart}
                       WHERE UserID = @id", conn);
                cmd.Parameters.AddWithValue("@login",   login);
                cmd.Parameters.AddWithValue("@role",    roleId);
                cmd.Parameters.AddWithValue("@blocked", isBlocked ? 1 : 0);
                cmd.Parameters.AddWithValue("@id",      userId);
                if (!string.IsNullOrEmpty(passwordHash))
                    cmd.Parameters.AddWithValue("@pw", passwordHash);
                cmd.ExecuteNonQuery();
            }
        }

        public static DataTable GetRoles()
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            {
                conn.Open();
                var adapter = new SqlDataAdapter("SELECT RoleID, RoleName FROM Roles", conn);
                adapter.Fill(dt);
            }
            return dt;
        }

        // ─────────────────────── ORDER QUERY ────────────────────────

        public static DataTable GetOrderSummary()
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            {
                conn.Open();
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM vw_OrderSummary ORDER BY OrderID", conn);
                adapter.Fill(dt);
            }
            return dt;
        }

        public static DataTable GetOrderDetails(int orderId)
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            {
                conn.Open();
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM vw_OrderTotalCost WHERE OrderID = @id",
                    conn);
                adapter.SelectCommand.Parameters.AddWithValue("@id", orderId);
                adapter.Fill(dt);
            }
            return dt;
        }
    }
}
