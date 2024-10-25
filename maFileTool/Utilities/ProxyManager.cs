using Serilog;
using System.Net;
using System.Text.RegularExpressions;

namespace maFileTool.Utilities
{
    public class ProxyManager
    {
        public static async Task<List<string>?> LoadProxiesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Logger.Error("Файл с прокси не найден!");
                return null;
            }

            var lines = await File.ReadAllLinesAsync(filePath);

            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();
        }

        public static WebProxy? ConvertProxy(string proxy)
        {
            WebProxy webProxy = new WebProxy();

            int type = proxy.Count(c => c == ':');

            switch (type)
            {
                case 2://Proxy without Credentials
                    webProxy = new WebProxy(proxy);
                    break;
                case 4:// Proxy with Credentials
                    string host = Regex.Match(proxy, ".*\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}:\\d{1,5}").Value;
                    string username = Regex.Match(proxy, "(?<=:\\d+:)[^:]+(?=:)").Value;
                    string password = Regex.Match(proxy, "(?<=:\\d+:[^:]+:)[^:]+$").Value;
                    webProxy = new WebProxy(host);
                    webProxy.Credentials = new NetworkCredential(username, password);
                    break;
                default:
                    return null;
            }

            return webProxy;
        }
    }
}
