using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAMAutoPoints
{
    public static class AutomationService
    {
        public class UserSummary
        {
            public string? Username { get; set; }
            public string VipExpires { get; set; } = "N/A";
            public string Downloaded { get; set; } = "N/A";
            public string Uploaded { get; set; } = "N/A";
            public string Ratio { get; set; } = "N/A";
        }

        public static async Task RunAutomationAsync(
            string cookieFilePath,
            int pointsBuffer,
            bool buyVip,
            bool buyFreeleech,
            bool buyOnlyFreeleech,
            int nextRunHours,
            Action<string> log,
            Action<UserSummary> updateUserInfo,
            Action<int, int> updateTotals)
        {
            try
            {
                log("Starting automation process.");

                CookieContainer cookies = CookieManager.LoadCookies(cookieFilePath);
                if (cookies == null)
                {
                    log("Failed to load cookies.");
                    return;
                }

                var userSummaryDict = await ApiHelper.GetUserSummaryAsync(cookies);
                var summary = new UserSummary
                {
                    Username = userSummaryDict.TryGetValue("username", out var u) ? u.GetString() : "N/A",
                    VipExpires = userSummaryDict.TryGetValue("vip_until", out var v) ? v.GetString() ?? "N/A" : "N/A",
                    Downloaded = userSummaryDict.TryGetValue("downloaded", out var d) ? d.GetString() ?? "N/A" : "N/A",
                    Uploaded = userSummaryDict.TryGetValue("uploaded", out var up) ? up.GetString() ?? "N/A" : "N/A",
                    Ratio = userSummaryDict.TryGetValue("ratio", out var r) ? r.ToString() : "N/A"
                };
                updateUserInfo(summary);

                string mamUid = await ApiHelper.GetSessionIdAsync(cookies);
                if (string.IsNullOrEmpty(mamUid))
                {
                    log("Session invalid.");
                    return;
                }

                int points = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                int initialPoints = points;

                log($"Current points: {points}");

                bool vipPurchased = false;

                if (buyVip)
                {
                    DateTime vipExpiry = await ApiHelper.GetVipExpiryAsync(cookies);
                    if ((vipExpiry - DateTime.Now).TotalDays <= 83)
                    {
                        var vipResult = await ApiHelper.BuyVipAsync(cookies);
                        if (vipResult)
                        {
                            vipPurchased = true;
                            log("VIP purchased.");
                        }
                    }
                }

                if (buyFreeleech)
                {
                    bool wedgeBought = await ApiHelper.BuyFreeleechWedgeAsync(cookies);
                    if (wedgeBought)
                    {
                        log("Freeleech wedge purchased.");
                    }
                    else
                    {
                        log("Freeleech wedge not purchased.");
                    }

                    if (buyOnlyFreeleech)
                    {
                        log("Buy-only-freeleech enabled. Skipping upload credit.");
                        return;
                    }
                }

                int spendablePoints = points - pointsBuffer;
                int gbToBuy = spendablePoints / 500;

                if (gbToBuy < 50)
                {
                    log("Not enough points to buy minimum 50 GiB.");
                    return;
                }

                await ApiHelper.BuyUploadCreditAsync(cookies, gbToBuy);

                int newPoints = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                int pointsSpent = initialPoints - newPoints;

                if (pointsSpent > 0)
                {
                    updateTotals(0, pointsSpent);
                    log($"Upload purchased: {gbToBuy} GiB");
                }
            }
            catch (Exception ex)
            {
                log("Automation error: " + ex.Message);
            }
        }
    }
}
