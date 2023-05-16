namespace maFileTool.Model
{
    public class Settings
    {
        public string Mode { get; set; } = string.Empty;
        public string BindingTimeout { get; set; } = string.Empty;
        public string SMSTimeout { get; set; } = string.Empty;
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
        public string MailServer { get; set; } = string.Empty;
        public string MailPort { get; set; } = string.Empty;
        public string MailType { get; set; } = string.Empty;
    }
}
