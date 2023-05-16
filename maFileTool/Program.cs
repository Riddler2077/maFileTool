using maFileTool.Core;
using maFileTool.Model;
using maFileTool.Services;
using maFileTool.Services.Api;
using Newtonsoft.Json;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace maFileTool
{
    public class Program
    {
        public static string steam = Environment.CurrentDirectory + "\\Steam.xlsx";
        public static List<Account> accounts = new List<Account>();
        public static bool quit = false;
        static bool b = false;
        static void Main(string[] args)
        {
            if(!System.IO.File.Exists(String.Format("{0}\\Settings.json", Environment.CurrentDirectory))) new Utils().SaveSettings();

            accounts = new Excel().ReadFromExcel(steam);
            accounts.RemoveAll(t => String.IsNullOrEmpty(t.Login) || t.Login == "Логин" || !string.IsNullOrEmpty(t.Phone));
            Console.WriteLine("Loaded - {0} accounts", accounts.Count());
            
            foreach (var account in accounts) 
            {
                b = false;

                new Worker(account.Login, account.Password, account.Email, account.EmailPassword).DoWork();

                if (quit) break;

                int waiting = Int32.Parse(Worker.settings.BindingTimeout) * 60;

                while (waiting > 0) 
                {
                    if (b)
                    {
                        Console.SetCursorPosition(String.Format("Sleep {0} seconds before linking new account.", (waiting + 1)).Length, Console.CursorTop - 1);
                        do { Console.Write("\b \b"); } while (Console.CursorLeft > 0);
                        Console.WriteLine("Sleep {0} seconds before linking new account.", waiting);
                    }
                    else
                    {
                        b = true;
                        Console.WriteLine("Sleep {0} seconds before linking new account.", waiting);
                    }

                    Thread.Sleep(1 * 1000);
                    waiting--;
                }

                Console.SetCursorPosition(String.Format("Sleep {0} seconds before linking new account.", waiting).Length, Console.CursorTop - 1);
                do { Console.Write("\b \b"); } while (Console.CursorLeft > 0);
            }

            if (quit) Console.WriteLine("Exit due to an error. Goodbye.");
            else Console.WriteLine("All tasks have been completed successfully. Goodbye.");
            
            Console.ReadLine();
        }
    }
}
