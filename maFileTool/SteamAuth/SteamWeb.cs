using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace maFileTool.Services.SteamAuth
{
    public class SteamWeb
    {
        public static string MOBILE_APP_USER_AGENT = "Dalvik/2.1.0 (Linux; U; Android 9; Valve Steam App Version/3)";
        public static string BROWSER_APP_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36";

        public static async Task<string> GETRequest(string url, CookieContainer cookies, string referer = null!)
        {
            string response;
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                if(referer is not null)
                    wc.Headers[HttpRequestHeader.Referer] = referer;
                wc.Encoding = Encoding.UTF8;
                wc.CookieContainer = cookies;
                wc.Headers[HttpRequestHeader.UserAgent] = SteamWeb.MOBILE_APP_USER_AGENT;
                response = await wc.DownloadStringTaskAsync(url);
            }
            return response;
        }

        public static async Task<string> POSTRequest(string url, CookieContainer cookies, NameValueCollection body, string referer = null!)
        {
            if (body == null)
                body = new NameValueCollection();

            string response;
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.CookieContainer = cookies;
                wc.Headers[HttpRequestHeader.UserAgent] = SteamWeb.MOBILE_APP_USER_AGENT;
                if (referer is not null)
                    wc.Headers[HttpRequestHeader.Referer] = referer;
                if (url.Contains("steamcommunity.com")) wc.Headers[HttpRequestHeader.Host] = "steamcommunity.com";
                if (url.Contains("steamcommunity.com/tradeoffer/new/send")) wc.Headers[HttpRequestHeader.Referer] = "https://steamcommunity.com/tradeoffer/new/?partner=";
                if (url.Contains("accept")) wc.Headers[HttpRequestHeader.Referer] = "https://steamcommunity.com/tradeoffer/";
                byte[] result = await wc.UploadValuesTaskAsync(new Uri(url), "POST", body);
                response = Encoding.UTF8.GetString(result);
            }
            return response;
        }
    }
}
