using System;
using System.IO;
using System.Net;

namespace MAMAutoPoints
{
    public static class CookieManager
    {
        public static CookieContainer LoadCookies(string cookieFilePath)
        {
            if (!File.Exists(cookieFilePath))
                throw new FileNotFoundException("Cookie file not found.");

            string mamId = File.ReadAllText(cookieFilePath).Trim();

            if (string.IsNullOrWhiteSpace(mamId))
                throw new Exception("Cookie file is empty.");

            var cookies = new CookieContainer();
            cookies.Add(new Cookie(
                "mam_id",
                mamId,
                "/",
                ".myanonamouse.net"));

            return cookies;
        }
    }
}
