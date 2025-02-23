namespace ScrapperHttpFunction.Helpers;

using System.Security.Cryptography;
using System.Text;

public class HashHelper
{
    public static string GetHashMd5(string[] input)
    {
        StringBuilder builder = new StringBuilder();
        using (MD5 md5Hash = MD5.Create())
        {
            byte[] bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(String.Join('-', input)));

            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2")); // Convert to hexadecimal string
            }
        }

        return builder.ToString();
    }
}