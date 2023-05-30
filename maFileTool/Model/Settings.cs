namespace maFileTool.Model
{
    public class Settings
    {
        public string Mode { get; set; } = "TXT";
        public string BindingTimeout { get; set; } = "1";
        public string SMSTimeout { get; set; } = "1";
        public string CaptchaApiKey { get; set; } = string.Empty;
        public string GetSmsApiKey { get; set; } = string.Empty;
        public string GiveSmsApiKey { get; set; } = string.Empty;
        public string OnlineSimApiKey {  get; set; } = string.Empty;
        public string SmsActivateApiKey { get; set; } = string.Empty;
        public string VakSmsApiKey { get; set; } = string.Empty;
        public string GetSmsBaseUrl { get; set; } = "getsms.online";
        public string GiveSmsBaseUrl { get; set; } = "give-sms.com";
        public string OnlineSimBaseUrl { get; set; } = "onlinesim.io";
        public string SmsActivateBaseUrl { get; set; } = "sms-activate.org";
        public string VakSmsBaseUrl { get; set; } = "vak-sms.com";
        public string[] Priority { get; set; } = new string[] { "GetSms", "GiveSms", "OnlineSim", "SmsActivate", "VakSms" };
        public string GetSmsCountry { get; set; } = "or";
        public string GiveSmsCountry { get; set; } = "0";
        public string OnlineSimCountry { get; set; } = "7";
        public string SmsActivateCountry { get; set; } = "0";
        public string VakSmsCountry { get; set; } = "0";
        public string MailServer { get; set; } = string.Empty;
        public string MailPort { get; set; } = "993";
        public string MailProtocol { get; set; } = "IMAP";
    }
}
