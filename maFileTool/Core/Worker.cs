using MailKit.Net.Imap;
using MailKit;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using maFileTool.Model;
using maFileTool.Services.Api;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System.Collections.Specialized;
using System.Diagnostics.Eventing.Reader;
using maFileTool.Services;
using System.Collections;
using System.Net.Http;
using System.Security.Cryptography;
using System.Drawing;
using Org.BouncyCastle.Asn1.X509;
using System.Drawing.Imaging;
using MailKit.Net.Pop3;

namespace maFileTool.Core
{
    public class Worker
    {
        public static Settings settings = JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText(String.Format("{0}\\Settings.json", Environment.CurrentDirectory)));

        private readonly string _login;
        private readonly string _password;
        private readonly string _emailLogin;
        private readonly string _emailPassword;

        private string _activationId = String.Empty;
        private string _phoneNumber = String.Empty;
        private string _revocationCode = String.Empty;

        private int priorityCounter = 0;

        public Worker(string login, string password, string emailLogin, string emailPassword)
        {
            _login = login;
            _password = password;
            _emailLogin = emailLogin;
            _emailPassword = emailPassword;
        }

        public void DoWork()
        {
            #region Checks

            if (priorityCounter >= settings.Priority.Count()) 
            {
                Log("It looks like the available sms services are over. Try again later.");
                Program.quit = true;
                return;
            }

            string SmsService = settings.Priority.ElementAt(priorityCounter);

            if (String.IsNullOrEmpty(SmsService) || String.IsNullOrWhiteSpace(SmsService))
            {
                Log("It looks like no SMS service is set in the priorities.");
                Program.quit = true;
                return;
            }

            SmsService smsService = new SmsService("");

            switch (SmsService)
            {
                case "GetSms":
                    smsService = new SmsService(settings.GetSmsApiKey);
                    
                    if (String.IsNullOrEmpty(settings.GetSmsApiKey) || String.IsNullOrWhiteSpace(settings.GetSmsApiKey)) 
                    {
                        Log("GetSms apikey is not set! Specify the apikey in Settings.json");
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    smsService.BaseUrl = settings.GetSmsBaseUrl; //А вдруг
                    smsService.Country = settings.GetSmsCountry;
                    if (String.IsNullOrEmpty(smsService.BaseUrl) || String.IsNullOrWhiteSpace(smsService.BaseUrl)) smsService.BaseUrl = "getsms.online";
                    if (String.IsNullOrEmpty(smsService.Country) || String.IsNullOrWhiteSpace(smsService.Country)) smsService.Country = "or";
                    break;
                case "GiveSms":
                    smsService = new SmsService(settings.GiveSmsApiKey);
                    if (String.IsNullOrEmpty(settings.GiveSmsApiKey) || String.IsNullOrWhiteSpace(settings.GiveSmsApiKey))
                    {
                        Log("GiveSms apikey is not set! Specify the apikey in Settings.json");
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    smsService.BaseUrl = settings.GiveSmsBaseUrl;
                    smsService.Country = settings.GiveSmsCountry;
                    if (String.IsNullOrEmpty(smsService.BaseUrl) || String.IsNullOrWhiteSpace(smsService.BaseUrl)) smsService.BaseUrl = "give-sms.com";
                    if (String.IsNullOrEmpty(smsService.Country) || String.IsNullOrWhiteSpace(smsService.Country)) smsService.Country = "0";
                    break;
                case "OnlineSim":
                    smsService = new SmsService(settings.OnlineSimApiKey);
                    if (String.IsNullOrEmpty(settings.OnlineSimApiKey) || String.IsNullOrWhiteSpace(settings.OnlineSimApiKey))
                    {
                        Log("OnlineSim apikey is not set! Specify the apikey in Settings.json");
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    smsService.BaseUrl = settings.OnlineSimBaseUrl;
                    smsService.Country = settings.OnlineSimCountry;
                    if (String.IsNullOrEmpty(smsService.BaseUrl) || String.IsNullOrWhiteSpace(smsService.BaseUrl)) smsService.BaseUrl = "onlinesim.io";
                    if (String.IsNullOrEmpty(smsService.Country) || String.IsNullOrWhiteSpace(smsService.Country)) smsService.Country = "7";
                    break;
                case "SmsActivate":
                    smsService = new SmsService(settings.SmsActivateApiKey);
                    if (String.IsNullOrEmpty(settings.SmsActivateApiKey) || String.IsNullOrWhiteSpace(settings.SmsActivateApiKey))
                    {
                        Log("SmsActivate apikey is not set! Specify the apikey in Settings.json");
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    smsService.BaseUrl = settings.SmsActivateBaseUrl;
                    smsService.Country = settings.SmsActivateCountry;
                    if (String.IsNullOrEmpty(smsService.BaseUrl) || String.IsNullOrWhiteSpace(smsService.BaseUrl)) smsService.BaseUrl = "sms-activate.org";
                    if (String.IsNullOrEmpty(smsService.Country) || String.IsNullOrWhiteSpace(smsService.Country)) smsService.Country = "0";
                    break;
                case "VakSms":
                    smsService = new SmsService(settings.VakSmsApiKey);
                    if (String.IsNullOrEmpty(settings.VakSmsApiKey) || String.IsNullOrWhiteSpace(settings.VakSmsApiKey))
                    {
                        Log("VakSms apikey is not set! Specify the apikey in Settings.json");
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    smsService.BaseUrl = settings.VakSmsBaseUrl;
                    smsService.Country = settings.VakSmsCountry;
                    if (String.IsNullOrEmpty(smsService.BaseUrl) || String.IsNullOrWhiteSpace(smsService.BaseUrl)) smsService.BaseUrl = "vak-sms.com";
                    if (String.IsNullOrEmpty(smsService.Country) || String.IsNullOrWhiteSpace(smsService.Country)) smsService.Country = "0";
                    break;
            }

            #endregion 

            try
            {
                Log(String.Format("Balance {0} - {1}", smsService.BaseUrl, smsService.Balance().Result));

                UserLogin userLogin = new UserLogin(_login, _password);
                LoginResult response = LoginResult.BadCredentials;

                while ((response = userLogin.DoLogin()) != LoginResult.LoginOkay)
                {
                    switch (response)
                    {
                        case LoginResult.NeedEmail:
                            Log("Waiting login code");
                            var emailCode = GetLoginCodeFromEmail(settings.MailServer, Int32.Parse(settings.MailPort));
                            userLogin.EmailCode = emailCode;
                            break;

                        case LoginResult.NeedCaptcha:

                            Log("Сaptcha needed!");
                            TwoCaptcha.TwoCaptcha solver = new TwoCaptcha.TwoCaptcha(settings.CaptchaApiKey);
                            solver.DefaultTimeout = 60 * 1000;

                            Log(String.Format("Balance rucaptcha.com - {0:F2}", solver.Balance().Result).Replace(',', '.'));

                            //string url = String.Format("https://store.steampowered.com/login/rendercaptcha?gid={0}", userLogin.CaptchaGID);
                            string url = String.Format("https://steamcommunity.com/public/captcha.php?gid={0}", userLogin.CaptchaGID);

                            string base64 = string.Empty;
                            using (WebClient webClient = new WebClient()) 
                            {
                                byte[] data = webClient.DownloadData(url);
                                base64 = Convert.ToBase64String(data);
                                //Если нужно просмотреть капчу
                                /*using (MemoryStream mem = new MemoryStream(data)) 
                                {
                                    using (var img = Image.FromStream(mem))
                                    {
                                        // If you want it as Png
                                        img.Save(Environment.CurrentDirectory + "\\image.png", ImageFormat.Png);
                                    }
                                }*/
                            }

                            TwoCaptcha.Captcha.Normal captcha = new TwoCaptcha.Captcha.Normal();
                            captcha.SetBase64(base64);
                            captcha.SetMinLen(6);
                            captcha.SetMaxLen(6);
                            captcha.SetCaseSensitive(false);
                            captcha.SetLang("en");

                            Log("Trying to solve a captcha!");

                            try
                            {
                                solver.Solve(captcha).Wait();
                                string code = captcha.Code.ToUpper();
                                Log(String.Format("Captcha solved: {0}", code));
                                userLogin.CaptchaText = code;
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error occurred: " + e.Message);
                                return;
                            }

                        case LoginResult.Need2FA:
                            Log("Already 2FA protected");
                            return;

                        case LoginResult.BadRSA:
                            Log("Error logging in: Steam returned \"BadRSA\"");
                            return;

                        case LoginResult.BadCredentials:
                            Log("Wrong username or password");
                            return;

                        case LoginResult.TooManyFailedLogins:
                            Log("IP banned");
                            Program.quit = true;
                            return;

                        case LoginResult.GeneralFailure:
                            Log("Steam GeneralFailture :(");
                            return;
                    }
                }

                SessionData session = userLogin.Session;

                while (true)
                {
                    string result = smsService.GetNumber(smsService.Country).Result;

                    if (result == "NO_MEANS" || result == "NO_BALANCE")
                    {
                        if(settings.Priority.Last() == SmsService) Program.quit = true;
                        Log("The balance of the SMS service has ended.");
                        Log("Sleep 1 min before switching to the next service.");
                        System.Threading.Thread.Sleep(60 * 1000);
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    else if (result == "NO_NUMBER" || result == "NO_NUMBERS")
                    {
                        if (settings.Priority.Last() == SmsService) Program.quit = true;
                        Log("The SMS service numbers out of stock.");
                        Log("Sleep 1 min before switching to the next service.");
                        System.Threading.Thread.Sleep(60 * 1000);
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    else if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
                    {
                        if (settings.Priority.Last() == SmsService) Program.quit = true;
                        Log("Unknown error.");
                        Log("Sleep 1 min before switching to the next service.");
                        System.Threading.Thread.Sleep(60 * 1000);
                        priorityCounter++;
                        DoWork();
                        return;
                    }
                    else
                    {
                        _activationId = result.Split(':')[1];
                        _phoneNumber = result.Split(':')[2];
                        Log(String.Format("Got a number {0}, ActivationId {1}", _phoneNumber, _activationId));
                    }

                    string is_valid = PhoneValidate(session);

                    if (is_valid == "valid") break;
                    else if (is_valid == "invalid")
                    {
                        Log(String.Format("Bad number {0}", _phoneNumber));
                        smsService.SetStatus(_activationId, "10"); //Уведомление, что номер уже занят
                        continue;
                    }
                    else
                    {
                        Program.quit = true;
                        break;
                    }
                }

                if (Program.quit) return;

                Log(String.Format("Number {0} accepted, waiting email from steam.", _phoneNumber));

                var linker = new AuthenticatorLinker(session);
                var linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;

                while ((linkResponse = linker.AddAuthenticator()) != AuthenticatorLinker.LinkResult.AwaitingFinalization)
                {
                    switch (linkResponse)
                    {
                        case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
                            //var phoneNumber = getSmsOnline.GetNewPhoneNumber(settings.SmsApiKey);
                            var phoneNumber = FilterPhoneNumber(_phoneNumber);
                            linker.PhoneNumber = phoneNumber;
                            break;

                        case AuthenticatorLinker.LinkResult.MustRemovePhoneNumber:
                            linker.PhoneNumber = null;
                            break;

                        case AuthenticatorLinker.LinkResult.MustConfirmEmail:
                            ConfirmEmailForAdd(settings.MailServer, Int32.Parse(settings.MailPort));
                            break;

                        case AuthenticatorLinker.LinkResult.GeneralFailure:
                            Log("Steam GeneralFailture :(");
                            if(settings.Mode == "EXCEL")
                                SaveToExcel("Week", "Week");
                            return;
                    }
                }

                smsService.SetStatus(_activationId, "1"); //Уведомление, что SMS отправлена

                var finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;
                while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
                {
                    Dictionary<string, int> waitOptions = new Dictionary<string, int>();
                    waitOptions.Add("timeout", Int32.Parse(settings.SMSTimeout) * 60);
                    waitOptions.Add("pollingInterval", 5);
                    var smsCode = smsService.WaitForResult(_activationId, _login, waitOptions).Result;

                    if (string.IsNullOrEmpty(smsCode))
                    {
                        Log("SMS not received");
                        smsService.SetStatus(_activationId, "-1"); //Отмена активации
                        DoWork();
                        return;
                    }

                    Log(String.Format("Received SMS code {0}", smsCode));
                    finalizeResponse = linker.FinalizeAddAuthenticator(smsCode);

                    switch (finalizeResponse)
                    {
                        case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                            Log("SMS code incorrect");
                            return;

                        case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                            Log("Unable to generate the proper codes to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                            return;

                        case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                            Log("Steam GeneralFailture :(");
                            if (settings.Mode == "EXCEL")
                                SaveToExcel("Week", "Week");
                            return;
                    }
                }

                _revocationCode = linker.LinkedAccount.RevocationCode;

                SaveAccount(linker.LinkedAccount);
                Log($"{_login}:{_password}:{_emailLogin}:{_emailPassword}:{_phoneNumber}:{linker.LinkedAccount.RevocationCode}");
                LogToFile($"{_login}:{_password}:{_emailLogin}:{_emailPassword}:{_phoneNumber}:{linker.LinkedAccount.RevocationCode}");

                smsService.SetStatus(_activationId, "6"); //Код верный, завершение активации
                if (settings.Mode == "EXCEL")
                    SaveToExcel(_phoneNumber, _revocationCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private string PhoneValidate(SessionData session)
        {
            string url = "https://store.steampowered.com/phone/validate";

            NameValueCollection data = new NameValueCollection();
            data.Add("sessionID", session.SessionID);
            
            if(_phoneNumber.Contains("+"))
                data.Add("phoneNumber", _phoneNumber);
            else
                data.Add("phoneNumber", String.Format("+{0}", _phoneNumber));

            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept-Language", "en-US,en;q=0.7");
            headers.Add("Accept-Encoding", "gzip, deflate, br");
            headers.Add("X-Requested-With", "XMLHttpRequest");
            headers.Add("Origin", "https://store.steampowered.com");
            headers.Add("Upgrade-Insecure-Requests", "1");

            CookieContainer cookies = new CookieContainer();
            cookies.Add(new Cookie("sessionid", session.SessionID, "/", ".steampowered.com"));
            cookies.Add(new Cookie("steamLoginSecure", session.SteamLoginSecure, "/", ".steampowered.com"));

            try
            {
                string response = SteamWeb.Request(url, "POST", data, cookies, headers, "https://store.steampowered.com/phone/add");
                if (response.Contains("DOCTYPE")) 
                {
                    response = SteamWeb.Request(url, "POST", data, cookies, headers, "https://store.steampowered.com/phone/add");
                }

                JObject obj = JObject.Parse(response);
                string success = obj["success"].ToString();
                string is_valid = obj["is_valid"].ToString();

                if (success == "True")
                {
                    if (is_valid == "True")
                    {
                        return "valid";
                    }
                    else
                    {
                        return "invalid";
                    }
                }
                else
                {
                    return "error";
                }
            }
            catch (Exception e)
            {
                return "error";
            }
        }

        public void SaveToExcel(string phone, string revocationCode)
        {
            Account ac = Program.accounts.First(t => t.Login == _login);
            ac.Phone = phone;
            ac.RevocationCode = revocationCode;
            Excel excel = new Excel();
            bool showed = false;
            int ext = 0;
            while (true)
            {
                try
                {
                    excel.WriteRowToExcel(Program.steam, ac, Int32.Parse(ac.Id));
                    break;
                }
                catch (InvalidOperationException)
                {
                    if (!showed)
                    {
                        Log("Couldn't access excel file. Please close excel.");
                        Log("Please close all excel processes for further work!");
                        showed = true;
                    }
                    ext++;
                }
                if (ext >= 60) 
                {
                    Log("The error occurred because excel blocked the file.");
                    Program.quit = true;
                    break;
                }
                Thread.Sleep(1 * 1000);
            }
        }

        private string GetLoginCodeFromEmail(string host, int port)
        {
            Thread.Sleep(10000);

            var loginCode = string.Empty;

            if (settings.MailProtocol == "IMAP")
            {
                using (var client = new ImapClient())
                {
                    client.CheckCertificateRevocation = false;
                    client.Connect(host, port, true);
                    client.Authenticate(_emailLogin, _emailPassword);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);

                    for (var i = inbox.Count - 1; i >= 0; i--)
                    {
                        var message = inbox.GetMessage(i);

                        var code = Regex.Match(message.HtmlBody, "class=([\"])title-48 c-blue1 fw-b a-center([^>]+)([>])([^<]+)").Groups[4].Value;
                        if (string.IsNullOrEmpty(code)) continue;

                        loginCode = code.Trim();
                        Log($"Login code: {loginCode}");
                        break;
                    }
                }
            }
            else if (settings.MailProtocol == "POP3")
            {
                //Not tested
                using (var client = new Pop3Client()) 
                {
                    client.CheckCertificateRevocation = false;
                    client.Connect(host, port, true);
                    client.Authenticate(_emailLogin, _emailPassword);

                    int count = client.GetMessageCount();
                    for (int i = count - 1; i >= 0; i--) 
                    {
                        var message = client.GetMessage(i);
                        if (message.Subject.Contains("store.steampowered.com")) 
                        {
                            var code = Regex.Match(message.HtmlBody, "class=([\"])title-48 c-blue1 fw-b a-center([^>]+)([>])([^<]+)").Groups[4].Value;
                            if (string.IsNullOrEmpty(code)) continue;

                            loginCode = code.Trim();
                            Log($"Login code: {loginCode}");
                            client.Disconnect(true);
                            break;
                        }
                    }
                }
            }

            return loginCode;
        }

        private void ConfirmEmailForAdd(string host, int port)
        {
            Thread.Sleep(10000);

            if (settings.MailProtocol == "IMAP")
            {
                using (var client = new ImapClient())
                {
                    client.CheckCertificateRevocation = false;
                    client.Connect(host, port, true);
                    client.Authenticate(_emailLogin, _emailPassword);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);

                    for (var i = inbox.Count - 1; i >= 0; i--)
                    {
                        var message = inbox.GetMessage(i);
                        var link = Regex.Match(message.HtmlBody, "store([.])steampowered([.])com([\\/])phone([\\/])ConfirmEmailForAdd([?])stoken=([^\"]+)").Groups[0].Value;
                        if (string.IsNullOrEmpty(link)) continue;

                        new WebClient().DownloadString("https://" + link);
                        Log("Email confirmed.");
                        break;
                    }

                    client.Disconnect(true);
                }
            }
            else if (settings.MailProtocol == "POP3") 
            {
                //Not tested
                using (var client = new Pop3Client())
                {
                    client.CheckCertificateRevocation = false;
                    client.Connect(host, port, true);
                    client.Authenticate(_emailLogin, _emailPassword);

                    int count = client.GetMessageCount();
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var message = client.GetMessage(i);
                        if (message.Subject.Contains("store.steampowered.com"))
                        {
                            var link = Regex.Match(message.HtmlBody, "store([.])steampowered([.])com([\\/])phone([\\/])ConfirmEmailForAdd([?])stoken=([^\"]+)").Groups[0].Value;
                            if (string.IsNullOrEmpty(link)) continue;

                            new WebClient().DownloadString("https://" + link);
                            Log("Email confirmed.");
                            client.Disconnect(true);
                            break;
                        }
                    }
                }
            }

            Thread.Sleep(2000);
        }

        private static string FilterPhoneNumber(string phoneNumber)
        {
            phoneNumber = phoneNumber.Replace("-", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
            if (!phoneNumber.Contains("+") || !phoneNumber.Contains(Uri.EscapeDataString("+")))
                phoneNumber = String.Format("+{0}", phoneNumber);
            return phoneNumber;
        }
        private static void SaveAccount(SteamGuardAccount account)
        {
            var filename = account.Session.SteamID.ToString() + ".maFile";
            var jsonAccount = JsonConvert.SerializeObject(account);
            string path = String.Format("{0}\\maFiles", Environment.CurrentDirectory);
            if (!Directory.Exists(path)) Directory.CreateDirectory("maFiles");
            File.WriteAllText(String.Format("{0}\\{1}", path, filename), jsonAccount);
        }

        private void Log(string message) 
        {
            int index = Program.accounts.FindIndex(t => t.Login == _login);
            Console.WriteLine($"[{_login}][{(index + 1)}/{Program.accounts.Count}] - {message}"); 
        }
        private static void LogToFile(string message) => File.AppendAllText("result.log", message + "\n");
    }
}