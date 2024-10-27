using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace maFileTool.Model
{
    public class Settings
    {
        [JsonPropertyName("Mode")]
        public string Mode { get; set; } = "EXCEL";

        [JsonPropertyName("ThreadCount")]
        public string ThreadCount { get; set; } = "1";

        [JsonPropertyName("MailServer")]
        public string MailServer { get; set; } = string.Empty;

        [JsonPropertyName("MailPort")]
        public string MailPort { get; set; } = "993";

        [JsonPropertyName("MailProtocol")]
        public string MailProtocol { get; set; } = "IMAP";

        [JsonPropertyName("UseSSL")]
        public string UseSSL { get; set; } = "true";

        [JsonPropertyName("UseProxyForEmailClient")]
        public string UseProxyForEmailClient { get; set; } = "true";

        [JsonPropertyName("UseProxyListAutoClean")]
        public string UseProxyListAutoClean { get; set; } = "true";

        [JsonPropertyName("DelayBeforeMailCheck")]
        public string DelayBeforeMailCheck { get; set; } = "60";

        public Settings() { }

        [JsonConstructor]
        [DynamicDependency(nameof(Mode))]
        [DynamicDependency(nameof(ThreadCount))]
        [DynamicDependency(nameof(MailServer))]
        [DynamicDependency(nameof(MailPort))]
        [DynamicDependency(nameof(MailProtocol))]
        [DynamicDependency(nameof(UseSSL))]
        [DynamicDependency(nameof(UseProxyForEmailClient))]
        [DynamicDependency(nameof(UseProxyListAutoClean))]
        [DynamicDependency(nameof(DelayBeforeMailCheck))]
        public Settings(string Mode, string ThreadCount, string MailServer, string MailPort, string MailProtocol, string UseSSL, string UseProxyForEmailClient, string UseProxyListAutoClean, string DelayBeforeMailCheck)
        {
            this.Mode = Mode;
            this.ThreadCount = ThreadCount;
            this.MailServer = MailServer;
            this.MailPort = MailPort;
            this.MailProtocol = MailProtocol;
            this.UseSSL = UseSSL;
            this.UseProxyForEmailClient = UseProxyForEmailClient;
            this.UseProxyListAutoClean = UseProxyListAutoClean;
            this.DelayBeforeMailCheck = DelayBeforeMailCheck;
        }
    }
}