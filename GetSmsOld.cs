using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maFileTool.Services.Sms
{
    public class GetSmsOld
    {
        public string GetBalance(string apikey) 
        {
            using (var wc = new WebClient())
            {
                var response = wc.DownloadString($"http://api.getsms.online/stubs/handler_api.php?api_key={apikey}&action=getBalance");

                if (response.Contains("ACCESS_BALANCE")) return String.Format("Balance getsms.online - {0} RUB", response.Split(':')[1]);
                else return string.Empty;
            }
        }

        public string GetNewPhoneNumber(string apikey) 
        {
            using (var wc = new WebClient())
            {
                var response = wc.DownloadString($"http://api.getsms.online/stubs/handler_api.php?api_key={apikey}&action=getNumber&service=sm");

                if (response.Contains("ACCESS_NUMBER")) return String.Format("{0}:{1}", response.Split(':')[1], response.Split(':')[2]);
                else if (response.Contains("NO_MEANS")) return "BALANCE_OUT";
                else if (response.Contains("NO_NUMBER")) return "NUMBERS_OUT";
                else return string.Empty;
            }
        }

        public string SetActivationStatus(string apikey, string id, string status) 
        {
            using (var wc = new WebClient())
            {
                var response = wc.DownloadString($"http://api.getsms.online/stubs/handler_api.php?api_key={apikey}&action=setStatus&id={id}&status={status}");

                //Система уведомлена о том, что SMS отправлена
                if (response.Contains("ACCESS_READY")) return "ACCESS_READY";
                //Активация успешно завершена
                else if (response.Contains("ACCESS_ACTIVATION")) return "ACCESS_ACTIVATION";
                //Номер отмечен как использованный
                else if (response.Contains("ACCESS_ERROR_NUMBER_GET")) return "ACCESS_ERROR_NUMBER_GET";
                //Активация успешно отменена
                else if (response.Contains("ACCESS_CANCEL")) return "ACCESS_CANCEL";
                else return string.Empty;
            }
        }

        public string WaitSmsCode(string login, string apikey, string id)
        {
            int waiting = 1;
            int max = 60;
            bool b = false;
            while (waiting <= max)
            {
                if (b)
                {
                    int index = Program.accounts.FindIndex(t => t.Login == login);
                    Console.SetCursorPosition(String.Format("[{0}][{1}/{2}] - Waiting SMS code - {3} sec.", login, (index + 1), Program.accounts.Count, (waiting + 1)).Length, Console.CursorTop - 1);
                    do { Console.Write("\b \b"); } while (Console.CursorLeft > 0);
                    Console.WriteLine("[{0}][{1}/{2}] - Waiting SMS code - {3}/{4} sec.", login, (index + 1), Program.accounts.Count, waiting, max);
                }
                else 
                {
                    b = true;
                    int index = Program.accounts.FindIndex(t => t.Login == login);
                    Console.WriteLine("[{0}][{1}/{2}] - Waiting SMS code - {3}/{4} sec.", login, (index + 1), Program.accounts.Count, waiting, max);
                }

                Thread.Sleep(1 * 1000);
                waiting++;

                using (var wc = new WebClient())
                {
                    var response = wc.DownloadString($"http://api.getsms.online/stubs/handler_api.php?api_key={apikey}&action=getStatus&id={id}");

                    if (response.Contains("STATUS_OK")) return response.Split(':')[1];
                    else if (response.Contains("STATUS_WAIT_READY")) return "WAIT_READY";
                    else if (response.Contains("STATUS_WAIT_CODE")) continue;
                    else if (response.Contains("STATUS_ERROR_NUMBER")) return "ERROR_NUMBER";
                    else if (response.Contains("STATUS_ERROR_SERVICE")) return "ERROR_SERVICE";
                    else if (response.Contains("STATUS_ERROR")) return "ERROR";
                    else return string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
