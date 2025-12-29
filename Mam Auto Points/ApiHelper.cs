using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAMAutoPoints
{
    public static class ApiHelper
    {
        private static HttpClient CreateClient(CookieContainer cookies)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AutomaticDecompression = DecompressionMethods.All
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MAMAutoPoints");
            return client;
        }

        public static async Task<Dictionary<string, JsonElement>> GetUserSummaryAsync(CookieContainer cookies)
        {
            using var client = CreateClient(cookies);
            var json = await client.GetStringAsync("https://www.myanonamouse.net/json/userstats.php");
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        }

        public static async Task<string> GetSessionIdAsync(CookieContainer cookies)
        {
            using var client = CreateClient(cookies);
            var html = await client.GetStringAsync("https://www.myanonamouse.net/");
            return html.Contains("logout.php") ? "valid" : "";
        }

        public static async Task<int> GetSeedBonusAsync(CookieContainer cookies, string _)
        {
            using var client = CreateClient(cookies);
            var json = await client.GetStringAsync("https://www.myanonamouse.net/json/seedbonus.php");
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("points").GetInt32();
        }

        public static async Task<DateTime> GetVipExpiryAsync(CookieContainer cookies)
        {
            var data = await GetUserSummaryAsync(cookies);
            if (data.TryGetValue("vip_until", out var v) &&
                DateTime.TryParse(v.GetString(), out var dt))
                return dt;

            return DateTime.MinValue;
        }

        public static async Task<bool> BuyVipAsync(CookieContainer cookies)
        {
            using var client = CreateClient(cookies);
            var res = await client.GetStringAsync("https://www.myanonamouse.net/json/buy_vip.php");
            return res.Contains("\"success\":true");
        }

        public static async Task<bool> BuyFreeleechWedgeAsync(CookieContainer cookies)
        {
            using var client = CreateClient(cookies);
            var res = await client.GetStringAsync("https://www.myanonamouse.net/json/buy_freeleech.php");
            return res.Contains("\"success\":true");
        }

        public static async Task BuyUploadCreditAsync(CookieContainer cookies, int gb)
        {
            using var client = CreateClient(cookies);
            await client.GetStringAsync(
                $"https://www.myanonamouse.net/json/buy_upload.php?amount={gb}");
        }
    }
}
