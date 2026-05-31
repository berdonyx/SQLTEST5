using System;
using System.Security.Cryptography;
using System.Text;

namespace ManufacturingApp.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash  = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }
    }
}
