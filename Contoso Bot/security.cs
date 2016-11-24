using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;


namespace Contoso_Bot
{
    public class security
    {
        public static string genuser()
        {
            string username = "";
            Random rng = new Random();
            for (int i = 0; i < 8; i++)
            {
                username = string.Concat(username + rng.Next(0, 10).ToString());
            }
            return username;
        }

        public static string gensalt()
        {
            Random rng = new Random();
            string salt = "";
            string chars = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";

            for (int i = 0; i<32; i++)
            {
                char add = chars[rng.Next(0, 62)];
                salt = string.Concat(salt + add);
            }
            return salt;
            
        }
        public static string genhash(string pass, string salt)
        {
            MD5 hasher = MD5.Create();
            pass = string.Concat(pass + salt);
            byte[] data = hasher.ComputeHash(Encoding.UTF8.GetBytes(pass));

            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static bool verifyhash(string pass, string salt, string hash)
        {
            string passhash = genhash(pass, salt);

            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(passhash, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}

