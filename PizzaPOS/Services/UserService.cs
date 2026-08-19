// Services/UserService.cs
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;

namespace PizzaPOS.Services
{
    public class UserService
    {
        static SqliteConnection Open() => DatabaseHelper.Open();

        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string HashPin(string pin)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            byte[] combined = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);
            return Convert.ToBase64String(combined);
        }

        public static bool VerifyPin(string pin, string storedHash)
        {
            if (storedHash.Length == 64 && storedHash.All(c => "0123456789abcdef".Contains(c)))
            {
                string legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pin)));
                return string.Equals(storedHash, legacyHash, StringComparison.OrdinalIgnoreCase);
            }
            try
            {
                byte[] combined = Convert.FromBase64String(storedHash);
                if (combined.Length != SaltSize + HashSize) return false;
                byte[] salt = new byte[SaltSize];
                byte[] hash = new byte[HashSize];
                Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(combined, SaltSize, hash, 0, HashSize);
                byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
                return CryptographicOperations.FixedTimeEquals(hash, testHash);
            }
            catch
            {
                return false;
            }
        }

        public User? Login(string username, string pin)
        {
            using var c = Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Username,FullName,PinHash,Role FROM Users WHERE Username=@u AND IsActive=1";
            cmd.Parameters.AddWithValue("@u", username);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            string stored = r.GetString(3);
            if (!VerifyPin(pin, stored)) return null;
            return new User
            {
                Id = r.GetInt32(0),
                Username = r.GetString(1),
                FullName = r.GetString(2),
                PinHash = stored,
                Role = r.GetString(4)
            };
        }
        public List<User> GetAll()
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id,Username,FullName,Role,IsActive FROM Users ORDER BY FullName";
            var list = new List<User>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new User { Id = r.GetInt32(0), Username = r.GetString(1), FullName = r.GetString(2), Role = r.GetString(3), IsActive = r.GetInt32(4) == 1 });
            return list;
        }

        public void Save(User u, string? newPin = null)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            string pinHash = newPin != null ? HashPin(newPin) : u.PinHash;
            if (u.Id == 0)
                cmd.CommandText = "INSERT INTO Users(Username,FullName,PinHash,Role) VALUES(@un,@fn,@ph,@r)";
            else { cmd.CommandText = "UPDATE Users SET Username=@un,FullName=@fn,PinHash=@ph,Role=@r WHERE Id=@id"; cmd.Parameters.AddWithValue("@id", u.Id); }
            cmd.Parameters.AddWithValue("@un", u.Username); cmd.Parameters.AddWithValue("@fn", u.FullName);
            cmd.Parameters.AddWithValue("@ph", pinHash); cmd.Parameters.AddWithValue("@r", u.Role);
            cmd.ExecuteNonQuery();
        }
        public void SetActive(int id, bool active)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsActive=@a WHERE Id=@id";
            cmd.Parameters.AddWithValue("@a", active ? 1 : 0); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
        }
    }
}

