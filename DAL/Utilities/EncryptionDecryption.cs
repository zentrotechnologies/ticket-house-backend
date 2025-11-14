using MODEL.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public interface IEncryptionDecryption
    {
        Task<string> Encryption(string plainText);
        Task<string> Decryption(string cipherText);
    }
    public class EncryptionDecryption: IEncryptionDecryption
    {
        private readonly THConfiguration _configuration;

        public EncryptionDecryption(THConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> Encryption(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_configuration.EncryptionKey.PadRight(32).Substring(0, 32));
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                await streamWriter.WriteAsync(plainText);
                            }

                            array = memoryStream.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(array);
            }
            catch (Exception)
            {
                return plainText;
            }
        }

        public async Task<string> Decryption(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_configuration.EncryptionKey.PadRight(32).Substring(0, 32));
                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                return await streamReader.ReadToEndAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return cipherText;
            }
        }
    }
}
