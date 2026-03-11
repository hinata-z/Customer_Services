using System.Security.Cryptography;
using System.Text;

namespace Customer.WebApi.Filter
{
    public class HMACSHA256Utils
    {

        public static string CalculateSignature(String key, string paramString)
        {
            paramString = paramString.ToUpper();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var paramStringBytes = Encoding.UTF8.GetBytes(paramString);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(paramStringBytes);

            return Convert.ToBase64String(hashBytes);

        }

        public static string CalculateSignatureSha1(String key, string paramString)
        {
            paramString = paramString.ToUpper();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var paramStringBytes = Encoding.UTF8.GetBytes(paramString);

            using var hmac = new HMACSHA1(keyBytes);
            var hashBytes = hmac.ComputeHash(paramStringBytes);

            return Convert.ToBase64String(hashBytes);

        }
    }
}
