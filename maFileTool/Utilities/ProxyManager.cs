using MailKit.Net.Proxy;
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

        public static IProxyClient? MailProxyClient(WebProxy webProxy)
        {
            if (webProxy.Address is Uri uri)
            {
                string protocol = uri.Scheme.ToLower();

                if (webProxy.Credentials is not null)
                {
                    if (webProxy.Credentials is NetworkCredential creds)
                    {
                        return protocol switch
                        {
                            "http" or "https" => new HttpProxyClient(uri.Host, uri.Port, creds),
                            "socks4" => new Socks4Client(uri.Host, uri.Port, creds),
                            "socks5" => new Socks5Client(uri.Host, uri.Port, creds),
                            _ => throw new NotImplementedException($"Протокол {protocol} не поддерживается."),
                        };
                    }
                }
                else
                {
                    return protocol switch
                    {
                        "http" or "https" => new HttpProxyClient(uri.Host, uri.Port),
                        "socks4" => new Socks4Client(uri.Host, uri.Port),
                        "socks5" => new Socks5Client(uri.Host, uri.Port),
                        _ => throw new NotImplementedException($"Протокол {protocol} не поддерживается."),
                    };
                }
            }

            return null;
        }

        //string protocol = Regex.Match(this.Proxy, "^[a-zA-Z0-9]+(?=://)").Value;
        //string host = Regex.Match(this.Proxy, "(?<=://)[^:]+(?=:)").Value;
        //string port = Regex.Match(this.Proxy, "(?<=://[^:]+:)\\d+(?=:)").Value;
        //string username = Regex.Match(this.Proxy, "(?<=:\\d+:)[^:]+(?=:)").Value;
        //string password = Regex.Match(this.Proxy, "(?<=:\\d+:[^:]+:)[^:]+$").Value;
    }
}
