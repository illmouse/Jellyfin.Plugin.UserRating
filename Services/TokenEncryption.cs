using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Plugin.UserRatings.Services
{
    public static class TokenEncryption
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Jellyfin.Plugin.UserRatings.TokenEncryption.v1");
        private static readonly Lazy<byte[]> _key = new(DeriveKey);

        public static string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key.Value;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return string.Empty;

            try
            {
                var data = Convert.FromBase64String(ciphertext);
                using var aes = Aes.Create();
                aes.Key = _key.Value;

                var iv = new byte[16];
                Buffer.BlockCopy(data, 0, iv, 0, 16);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static byte[] DeriveKey()
        {
            var machineId = GetMachineIdentifier();
            return Rfc2898DeriveBytes.Pbkdf2(
                machineId,
                Salt,
                100_000,
                HashAlgorithmName.SHA256,
                32);
        }

        private static string GetMachineIdentifier()
        {
            if (File.Exists("/etc/machine-id"))
            {
                return File.ReadAllText("/etc/machine-id").Trim();
            }

            if (File.Exists("/var/lib/dbus/machine-id"))
            {
                return File.ReadAllText("/var/lib/dbus/machine-id").Trim();
            }

            return Environment.MachineName;
        }
    }
}