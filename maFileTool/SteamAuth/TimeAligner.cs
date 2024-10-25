using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace maFileTool.Services.SteamAuth
{
    /// <summary>
    /// Class to help align system time with the Steam server time. Not super advanced; probably not taking some things into account that it should.
    /// Necessary to generate up-to-date codes. In general, this will have an error of less than a second, assuming Steam is operational.
    /// </summary>
    public class TimeAligner
    {
        private static bool _aligned = false;
        private static int _timeDifference = 0;

        public static long GetSteamTime()
        {
            if (!TimeAligner._aligned)
            {
                TimeAligner.AlignTime();
            }
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _timeDifference;
        }

        public static async Task<long> GetSteamTimeAsync()
        {
            if (!TimeAligner._aligned)
            {
                await TimeAligner.AlignTimeAsync();
            }
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _timeDifference;
        }

        public static void AlignTime()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            #pragma warning disable SYSLIB0014 // Тип или член устарел
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                try
                {
                    string response = client.UploadString(APIEndpoints.TWO_FACTOR_TIME_QUERY, "steamid=0");
                    TimeQuery? query = Json.Document.DeserializeJson<TimeQuery>(response);
                    TimeAligner._timeDifference = (int)(query!.Response!.ServerTime - currentTime);
                    TimeAligner._aligned = true;
                }
                catch (WebException)
                {
                    return;
                }
            }
            #pragma warning restore SYSLIB0014 // Тип или член устарел
        }

        public static async Task AlignTimeAsync()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            #pragma warning disable SYSLIB0014 // Тип или член устарел
            WebClient client = new WebClient();
            #pragma warning restore SYSLIB0014 // Тип или член устарел
            try
            {
                client.Encoding = Encoding.UTF8;
                string response = await client.UploadStringTaskAsync(new Uri(APIEndpoints.TWO_FACTOR_TIME_QUERY), "steamid=0");
                TimeQuery? query = Json.Document.DeserializeJson<TimeQuery>(response);
                TimeAligner._timeDifference = (int)(query!.Response!.ServerTime - currentTime);
                TimeAligner._aligned = true;
            }
            catch (WebException)
            {
                return;
            }
        }

        public class TimeQuery
        {
            [JsonPropertyName("response")]
            public TimeQueryResponse? Response { get; set; }

            public class TimeQueryResponse
            {
                [JsonPropertyName("server_time")]
                public long ServerTime { get; set; }

                [JsonPropertyName("skew_tolerance_seconds")]
                public int SkewToleranceSeconds { get; set; }

                [JsonPropertyName("large_time_jink")]
                public long LargeTimeJink { get; set; }

                [JsonPropertyName("probe_frequency_seconds")]
                public int ProbeFrequencySeconds { get; set; }

                [JsonPropertyName("adjusted_time_probe_frequency_seconds")]
                public int AdjustedTimeProbeFrequencySeconds { get; set; }

                [JsonPropertyName("hint_probe_frequency_seconds")]
                public int HintProbeFrequencySeconds { get; set; }

                [JsonPropertyName("sync_timeout")]
                public int SyncTimeout { get; set; }

                [JsonPropertyName("try_again_seconds")]
                public int TryAgainSeconds { get; set; }

                [JsonPropertyName("max_attempts")]
                public int MaxAttempts { get; set; }
            }
        }
    }
}
