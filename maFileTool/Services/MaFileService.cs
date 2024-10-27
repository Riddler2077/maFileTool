using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Net;
using maFileTool.Services.SteamAuth;
using maFileTool.Interfaces;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit;
using Polly.Retry;
using Polly;
using SteamKit2.Authentication;
using SteamKit2.Internal;
using SteamKit2;
using Serilog;
using maFileTool.Utilities;
using maFileTool.Model;
using Microsoft.Extensions.DependencyInjection;
using MailKit.Net.Proxy;

namespace maFileTool.Services
{
    public class MaFileService : IMaFileService
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1); // Позволяет одному потоку работать одновременно

        private readonly IEnumerable<TimeSpan> sleepDurations = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16),];
        private AsyncRetryPolicy WaitAndRetryAsync(
        IEnumerable<TimeSpan> retryTimes,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0) => Policy.Handle<Exception>().WaitAndRetryAsync(retryTimes, (ex, timespan, retryAttempt, context) =>
        {
            if (context.ContainsKey("Login"))
            {
                if (context["Login"] is string login)
                {
                    Log.Logger.Error($"{login} => Retrying due to: {ex.Message}. Wait time: {timespan}. Attempt: {retryAttempt}.");
                }
            }
            else
            {
                Log.Logger.Error($"Retrying due to: {ex.Message}. Wait time: {timespan}. Attempt: {retryAttempt}.");
            }

            //Log.Logger.Error(/*ex, */$"Повторная попытка {retryAttempt}，Ожидание {timespan.TotalSeconds} секунд, MemberName：{memberName}，FilePath：{sourceFilePath}，LineNumber：{sourceLineNumber}");
        });

        string Login { get; set; } = string.Empty;
        string Password { get; set; } = string.Empty;
        string Email { get; set; } = string.Empty;
        string EmailPassword { get; set; } = string.Empty;
        string Proxy { get; set; } = string.Empty;
        string Phone { get; set; } = "Email";
        string RevocationCode { get; set; } = string.Empty;

        private static readonly object Locker = new object();

        HttpClient httpClient;
        SteamGuardAccount steamGuardAccount;
        SessionData sessionData;
        public MaFileService(string Login, string Password, string Email, string EmailPassword, string Proxy, HttpClient httpClient)
        {
            this.Login = Login;
            this.Password = Password;
            this.Email = Email;
            this.EmailPassword = EmailPassword;
            this.Proxy = Proxy;
            this.httpClient = httpClient;
            this.steamGuardAccount = new SteamGuardAccount();
            this.sessionData = new SessionData();
        }

        public async Task GetIP(CancellationToken cancellationToken = default)
        {
        //На данном этапе должны отсеиваться все плохие прокси

        CheckConnection:

            try
            {
                Context context = new Context();
                context["Login"] = Login;

                await WaitAndRetryAsync(sleepDurations).ExecuteAsync(async (ctx) =>
                {
                    string url = $"https://api.ipify.org?format=text";
                    //string url = $"https://ipv4-internet.yandex.net/api/v0/ip";

                    var response = await httpClient.GetAsync(url, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    string result = response.Content.ReadAsStringAsync().Result;

                    Log.Logger.Warning("{0} => {1}", Login, result);

                }, context);
            }
            catch (HttpRequestException ex)
            {
                Log.Logger.Error("{0} => Ошибка при загрузке страницы : {1}", Login, ex.Message);

                if (Convert.ToBoolean(Globals.Settings.UseProxyListAutoClean.ToLower()))
                    await ProxyManager.RemoveProxy(this.Proxy);

                var httpClientFactory = Program.ServiceProvider!.GetRequiredService<IHttpClientFactory>();
                var randomProxy = Globals.Proxies.Count >= 1 ? Globals.Proxies[new Random().Next(Globals.Proxies.Count)] : "Default";
                this.httpClient = httpClientFactory.CreateClient(randomProxy);
                this.Proxy = randomProxy;
                Log.Logger.Warning("{0} => Retrying with another client!", Login);
                goto CheckConnection;
            }
        }

        public async Task Authorization(CancellationToken cancellationToken = default) 
        {
            Log.Logger.Information("{0} => Authorization.", Login);

            steamGuardAccount.AccountName = Login;

            // Start a new SteamClient instance
            SteamClient steamClient = new SteamClient();

            // Connect to Steam
            Connect:

            // Connect to Steam
            steamClient.Connect();

            int i = 0;

            // Really basic way to wait until Steam is connected
            while (!steamClient.IsConnected)
            {
                if (i >= 30) //15 sec max
                    break;
                await Task.Delay(1 * 500);
                i++;
            }

            if (!steamClient.IsConnected)
                goto Connect;

            // Create a new auth session
            CredentialsAuthSession authSession;

            try
            {
                AuthSessionDetails authSessionDetails = new AuthSessionDetails();
                authSessionDetails.Username = this.Login;
                authSessionDetails.Password = this.Password;
                authSessionDetails.IsPersistentSession = false;
                authSessionDetails.PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp;
                authSessionDetails.ClientOSType = EOSType.Android9;
                authSessionDetails.Authenticator = new UserFormAuthenticator(steamGuardAccount);

                authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(authSessionDetails);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("{0} => Steam Login Error {1}", Login, ex);
                return;
            }

            // Starting polling Steam for authentication response
            AuthPollResult pollResponse;
            try
            {
                pollResponse = await authSession.PollingWaitForResultAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("{0} => Steam Login Error2 {1}", Login, ex);
                return;
            }

            // Build a SessionData object
            SessionData sessionData = new SessionData()
            {
                SteamID = authSession.SteamID.ConvertToUInt64(),
                AccessToken = pollResponse.AccessToken,
                RefreshToken = pollResponse.RefreshToken,
            };

            sessionData.SessionID = sessionData.GetCookies().GetCookies(new Uri("http://steamcommunity.com")).Cast<Cookie>().First(c => c.Name == "sessionid").Value;

            this.sessionData = sessionData;

            Log.Logger.Information("{0} => Steam account login succeeded.", Login);
        }

        public async Task LinkAuthenticator(CancellationToken cancellationToken = default)
        {
            AuthenticatorLinker linker = new AuthenticatorLinker(this.sessionData);

            AuthenticatorLinker.LinkResult linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;
            while (linkResponse != AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                try
                {
                    linkResponse = await linker.AddAuthenticator();
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("{0} => Error adding your authenticator: {1}", Login, ex.Message);
                    return;
                }

                switch (linkResponse)
                {
                    case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
                        break;

                    case AuthenticatorLinker.LinkResult.AuthenticatorPresent:
                        Log.Logger.Warning("{0} => This account already has an authenticator linked. You must remove that authenticator to add SDA as your authenticator.", Login);
                        return;

                    case AuthenticatorLinker.LinkResult.FailureAddingPhone:
                        Log.Logger.Error("{0} => Failed to add your phone number. Please try again or use a different phone number.", Login);
                        linker.PhoneNumber = null!;
                        break;

                    case AuthenticatorLinker.LinkResult.MustRemovePhoneNumber:
                        linker.PhoneNumber = null!;
                        break;

                    case AuthenticatorLinker.LinkResult.MustConfirmEmail:
                        break;

                    case AuthenticatorLinker.LinkResult.GeneralFailure:
                        Log.Logger.Error("{0} => Error adding your authenticator.", Login);
                        //SaveAccountData();
                        return;
                }
            } // End while loop checking for AwaitingFinalization

            Log.Logger.Information("{0} => Waiting authenticator code from email.", Login);

            AuthenticatorLinker.FinalizeResult finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;
            while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
            {
                AuthenticatorCode:

                var authenticatorCode = await GetAuthenticatorCodeFromEmail(Globals.Settings.MailServer, Int32.Parse(Globals.Settings.MailPort));

                if (string.IsNullOrEmpty(authenticatorCode))
                {
                    Log.Logger.Error("{0} => Authenticator code not received!", Login);
                    Log.Logger.Error("{0} => Retrying getting authenticator code from email!", Login);
                    goto AuthenticatorCode;
                    //SaveAccountData();
                    //return;
                }

                Log.Logger.Information("{0} => Sending Authenticator code.", Login);

                finalizeResponse = await linker.FinalizeAddAuthenticator(authenticatorCode);

                switch (finalizeResponse)
                {
                    case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                        Log.Logger.Error("{0} => Code {1} incorrect", Login, authenticatorCode);
                        Log.Logger.Error("{0} => Retrying getting authenticator code from email!", Login);
                        goto AuthenticatorCode;
                        //return;

                    case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                        Log.Logger.Error("{0} => Unable to generate the proper codes to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: {1}", Login, linker.LinkedAccount?.RevocationCode);
                        return;

                    case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                        Log.Logger.Error("{0} => Unable to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: {1}", Login, linker.LinkedAccount?.RevocationCode);
                        //SaveAccountData();
                        return;
                }

                if (linker.LinkedAccount is SteamGuardAccount account) 
                {
                    if (account.RevocationCode is string rcode)
                        RevocationCode = rcode;

                    await SaveMaFile(account);

                    Log.Logger.Warning("{0}:{1}:{2}:{3}:{4}:{5}", Login, Password, Email, EmailPassword, "Email", RevocationCode);

                    await SaveToExcel();
                }
            }

            async Task SaveMaFile(SteamGuardAccount account)
            {
                string filename = Path.Combine(Globals.MaFilesFolder, String.Format("{0}.maFile", account.Session?.SteamID));
                await Json.Document.WriteJsonAsync(account, filename);
            }

            async Task SaveToExcel()
            {
                if (Globals.Accounts.FirstOrDefault(a => a.Login == this.Login) is Account account)
                {
                    account.Phone = this.Phone;
                    account.RevocationCode = this.RevocationCode;
                    await semaphoreSlim.WaitAsync(); // Асинхронное ожидание
                    try
                    {
                        await Excel.WriteAccountToExcel(Globals.ExcelFilePath, account, Int32.Parse(account.Id));
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error("{0} => Ошибка сохранения: {1}", Login, ex.Message);
                    }
                    finally
                    {
                        semaphoreSlim.Release(); // Освобождаем семафор после выполнения
                    }
                }
            }
        }

        public async Task<string?> GetAuthenticatorCodeFromEmail(string host, int port, CancellationToken cancellationToken = default)
        {
            string authenticatorCode = string.Empty;

            await Task.Delay(Int32.Parse(Globals.Settings.DelayBeforeMailCheck) * 1000, cancellationToken);

            switch (Globals.Settings.MailProtocol.ToLower())
            {
                case "imap":
                    using (var client = new ImapClient())
                    {
                        client.CheckCertificateRevocation = false;

                        //Use Proxy For Email connection
                        if(Convert.ToBoolean(Globals.Settings.UseProxyForEmailClient.ToLower()))
                            if (this.Proxy != "Default")
                                if (ProxyManager.ConvertProxy(this.Proxy) is WebProxy webProxy)
                                    if (ProxyManager.MailProxyClient(webProxy) is IProxyClient proxyClient)
                                        client.ProxyClient = proxyClient;

                        Log.Logger.Information("{0} => MailClient started with proxy: {1}:{2}", Login, client.ProxyClient.ProxyHost, client.ProxyClient.ProxyPort);

                        try
                        {
                            client.Connect(host, port, Convert.ToBoolean(Globals.Settings.UseSSL.ToLower()), cancellationToken);
                            client.Authenticate(Email, EmailPassword, cancellationToken);
                        }
                        catch (MailKit.Security.AuthenticationException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }
                        catch (MailKit.Security.SslHandshakeException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }

                        var inbox = client.Inbox;
                        inbox.Open(FolderAccess.ReadOnly);

                        for (var i = inbox.Count - 1; i >= 0; i--)
                        {
                            var message = inbox.GetMessage(i, cancellationToken);

                            if (!message.From.ToString().Contains("<noreply@steampowered.com>"))
                                continue;

                            var code = Regex.Match(message.HtmlBody, "class=([\"])title-48 c-blue1 fw-b a-center([^>]+)([>])([^<]+)").Groups[4].Value;

                            if (string.IsNullOrEmpty(code))
                                continue;

                            authenticatorCode = code.Trim();

                            Log.Logger.Information("{0} => Authenticator code is: {1}", Login, authenticatorCode);

                            break;
                        }
                    }
                    break;
                case "pop3":
                    //Not tested
                    using (var client = new Pop3Client())
                    {
                        client.CheckCertificateRevocation = false;

                        //Use Proxy For Email connection
                        if (Convert.ToBoolean(Globals.Settings.UseProxyForEmailClient.ToLower()))
                            if (this.Proxy != "Default")
                                if (ProxyManager.ConvertProxy(this.Proxy) is WebProxy webProxy)
                                    if (ProxyManager.MailProxyClient(webProxy) is IProxyClient proxyClient)
                                        client.ProxyClient = proxyClient;

                        Log.Logger.Information("{0} => MailClient started with proxy: {1}:{2}", Login, client.ProxyClient.ProxyHost, client.ProxyClient.ProxyPort);

                        try
                        {
                            client.Connect(host, port, Convert.ToBoolean(Globals.Settings.UseSSL.ToLower()), cancellationToken);
                            client.Authenticate(Email, EmailPassword, cancellationToken);
                        }
                        catch (MailKit.Security.AuthenticationException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }
                        catch (MailKit.Security.SslHandshakeException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }

                        int count = client.GetMessageCount();

                        for (int i = count - 1; i >= 0; i--)
                        {
                            var message = client.GetMessage(i, cancellationToken);

                            if (!message.From.ToString().Contains("<noreply@steampowered.com>"))
                                continue;

                            var code = Regex.Match(message.HtmlBody, "class=([\"])title-48 c-blue1 fw-b a-center([^>]+)([>])([^<]+)").Groups[4].Value;

                            if (string.IsNullOrEmpty(code))
                                continue;

                            authenticatorCode = code.Trim();

                            Log.Logger.Information("{0} => Authenticator code is: {1}", Login, authenticatorCode);

                            break;
                        }
                    }
                    break;
                default:
                    return null;
            }

            return authenticatorCode;
        }

        public async Task<string?> GetLoginCodeFromEmail(string host, int port, CancellationToken cancellationToken = default)
        {
            var loginCode = string.Empty;

            await Task.Delay(Int32.Parse(Globals.Settings.DelayBeforeMailCheck) * 1000, cancellationToken);

            switch (Globals.Settings.MailProtocol.ToLower())
            {
                case "imap":
                    using (var client = new ImapClient())
                    {
                        client.CheckCertificateRevocation = false;

                        //Use Proxy For Email connection
                        if (Convert.ToBoolean(Globals.Settings.UseProxyForEmailClient.ToLower()))
                            if (this.Proxy != "Default")
                                if (ProxyManager.ConvertProxy(this.Proxy) is WebProxy webProxy)
                                    if (ProxyManager.MailProxyClient(webProxy) is IProxyClient proxyClient)
                                        client.ProxyClient = proxyClient;

                        Log.Logger.Information("{0} => MailClient started with proxy: {1}:{2}", Login, client.ProxyClient.ProxyHost, client.ProxyClient.ProxyPort);

                        try
                        {
                            client.Connect(host, port, Convert.ToBoolean(Globals.Settings.UseSSL.ToLower()), cancellationToken);
                            client.Authenticate(Email, EmailPassword, cancellationToken);
                        }
                        catch (MailKit.Security.AuthenticationException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }
                        catch (MailKit.Security.SslHandshakeException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }

                        var inbox = client.Inbox;
                        inbox.Open(FolderAccess.ReadOnly);

                        for (var i = inbox.Count - 1; i >= 0; i--)
                        {
                            var message = inbox.GetMessage(i, cancellationToken);

                            if (!message.From.ToString().Contains("<noreply@steampowered.com>"))
                                continue;

                            if (!message.HtmlBody.ToString().Contains("Steam Guard"))
                                continue;

                            var code = Regex.Match(message.HtmlBody, "class=([\"])title-48 c-blue1 fw-b a-center([^>]+)([>])([^<]+)").Groups[4].Value;

                            if (string.IsNullOrEmpty(code))
                                continue;

                            loginCode = code.Trim();

                            Log.Logger.Information("{0} => Login code is: {1}", Login, loginCode);

                            break;
                        }
                    }
                    break;
                case "pop3":
                    //Not tested
                    using (var client = new Pop3Client())
                    {
                        client.CheckCertificateRevocation = false;

                        //Use Proxy For Email connection
                        if (Convert.ToBoolean(Globals.Settings.UseProxyForEmailClient.ToLower()))
                            if (this.Proxy != "Default")
                                if (ProxyManager.ConvertProxy(this.Proxy) is WebProxy webProxy)
                                    if (ProxyManager.MailProxyClient(webProxy) is IProxyClient proxyClient)
                                        client.ProxyClient = proxyClient;

                        Log.Logger.Information("{0} => MailClient started with proxy: {1}:{2}", Login, client.ProxyClient.ProxyHost, client.ProxyClient.ProxyPort);

                        try
                        {
                            client.Connect(host, port, Convert.ToBoolean(Globals.Settings.UseSSL.ToLower()), cancellationToken);
                            client.Authenticate(Email, EmailPassword, cancellationToken);
                        }
                        catch (MailKit.Security.AuthenticationException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }
                        catch (MailKit.Security.SslHandshakeException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }

                        int count = client.GetMessageCount();

                        for (int i = count - 1; i >= 0; i--)
                        {
                            var message = client.GetMessage(i, cancellationToken);

                            if (!message.From.ToString().Contains("<noreply@steampowered.com>"))
                                continue;

                            if (!message.HtmlBody.ToString().Contains("Steam Guard"))
                                continue;

                            var code = Regex.Match(message.HtmlBody, "class=([\"])title-48 c-blue1 fw-b a-center([^>]+)([>])([^<]+)").Groups[4].Value;

                            if (string.IsNullOrEmpty(code))
                                continue;

                            loginCode = code.Trim();

                            Log.Logger.Information("{0} => Login code is: {1}", Login, loginCode);

                            break;
                        }
                    }
                    break;
                default:
                    return null;
            }

            return loginCode;
        }

        public async Task<string?> ConfirmEmailForAdd(string host, int port, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Int32.Parse(Globals.Settings.DelayBeforeMailCheck) * 1000, cancellationToken);

            switch (Globals.Settings.MailProtocol.ToLower())
            {
                case "imap":
                    using (var client = new ImapClient())
                    {
                        client.CheckCertificateRevocation = false;

                        //Use Proxy For Email connection
                        if (Convert.ToBoolean(Globals.Settings.UseProxyForEmailClient.ToLower()))
                            if (this.Proxy != "Default")
                                if (ProxyManager.ConvertProxy(this.Proxy) is WebProxy webProxy)
                                    if (ProxyManager.MailProxyClient(webProxy) is IProxyClient proxyClient)
                                        client.ProxyClient = proxyClient;

                        Log.Logger.Information("{0} => MailClient started with proxy: {1}:{2}", Login, client.ProxyClient.ProxyHost, client.ProxyClient.ProxyPort);

                        try
                        {
                            client.Connect(host, port, Convert.ToBoolean(Globals.Settings.UseSSL.ToLower()), cancellationToken);
                            client.Authenticate(Email, EmailPassword, cancellationToken);
                        }
                        catch (MailKit.Security.AuthenticationException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }
                        catch (MailKit.Security.SslHandshakeException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }

                        var inbox = client.Inbox;
                        inbox.Open(FolderAccess.ReadOnly);

                        for (var i = inbox.Count - 1; i >= 0; i--)
                        {
                            var message = inbox.GetMessage(i, cancellationToken);

                            var link = Regex.Match(message.HtmlBody, "store([.])steampowered([.])com([\\/])phone([\\/])ConfirmEmailForAdd([?])stoken=([^\"]+)").Groups[0].Value;

                            if (string.IsNullOrEmpty(link))
                                continue;

                            string url = String.Format("https://{0}", link);
                            if (await Confirm(url, cancellationToken) is string s)
                            {
                                //Unable to add phone
                                //There have been too many attempts to add a phone number to this account. Please only add phones that you own and try again in 1 week.
                                if (s.Contains("1 week"))
                                {
                                    string time = DateTime.Now.AddDays(7).ToString("dd.MM.yy HH:mm");
                                    //SaveAccountData(string.Empty, string.Empty, time);
                                    Log.Logger.Error("Too many attempts to add a phone number to this account. Please try again in 1 week.");
                                    break;
                                }

                                Log.Logger.Information("{0} => Email confirmed.", Login);
                            }

                            break;
                        }
                    }
                    break;
                case "pop3":
                    //Not tested
                    using (var client = new Pop3Client())
                    {
                        client.CheckCertificateRevocation = false;

                        //Use Proxy For Email connection
                        if (Convert.ToBoolean(Globals.Settings.UseProxyForEmailClient.ToLower()))
                            if (this.Proxy != "Default")
                                if (ProxyManager.ConvertProxy(this.Proxy) is WebProxy webProxy)
                                    if (ProxyManager.MailProxyClient(webProxy) is IProxyClient proxyClient)
                                        client.ProxyClient = proxyClient;

                        Log.Logger.Information("{0} => MailClient started with proxy: {1}:{2}", Login, client.ProxyClient.ProxyHost, client.ProxyClient.ProxyPort);

                        try
                        {
                            client.Connect(host, port, Convert.ToBoolean(Globals.Settings.UseSSL.ToLower()), cancellationToken);
                            client.Authenticate(Email, EmailPassword, cancellationToken);
                        }
                        catch (MailKit.Security.AuthenticationException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }
                        catch (MailKit.Security.SslHandshakeException ex)
                        {
                            Log.Logger.Error("{0} => Email error: {1}", Login, ex.Message);
                            return null;
                        }

                        int count = client.GetMessageCount();

                        for (int i = count - 1; i >= 0; i--)
                        {
                            var message = client.GetMessage(i, cancellationToken);

                            var link = Regex.Match(message.HtmlBody, "store([.])steampowered([.])com([\\/])phone([\\/])ConfirmEmailForAdd([?])stoken=([^\"]+)").Groups[0].Value;

                            if (string.IsNullOrEmpty(link))
                                continue;

                            string url = String.Format("https://{0}", link);
                            if (await Confirm(url, cancellationToken) is string s)
                            {
                                //Unable to add phone
                                //There have been too many attempts to add a phone number to this account. Please only add phones that you own and try again in 1 week.
                                if (s.Contains("1 week"))
                                {
                                    string time = DateTime.Now.AddDays(7).ToString("dd.MM.yy HH:mm");
                                    //SaveAccountData(string.Empty, string.Empty, time);
                                    Log.Logger.Error("Too many attempts to add a phone number to this account. Please try again in 1 week.");
                                    break;
                                }

                                Log.Logger.Information("{0} => Email confirmed.", Login);
                            }

                            break;
                        }
                    }
                    break;
                default:
                    return null;
            }

            return null;

            async Task<string?> Confirm(string url, CancellationToken cancellationToken = default)
            {
                try
                {
                    Context context = new Context();
                    context["Login"] = Login;

                    return await WaitAndRetryAsync(sleepDurations).ExecuteAsync(async (ctx) =>
                    {
                        var response = await httpClient.GetAsync(url, cancellationToken);

                        response.EnsureSuccessStatusCode();

                        string result = response.Content.ReadAsStringAsync().Result;

                        return result;

                    }, context);
                }
                catch (HttpRequestException ex)
                {
                    Log.Logger.Error("{0} => Ошибка при загрузке страницы : {1}", Email, ex.Message);
                    return null;
                }
            }
        }
    }
}
