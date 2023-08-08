using maFileTool.Core;
using maFileTool.Model;
using maFileTool.Services;
using maFileTool.Services.Api;
using maFileTool.Services.SteamAuth;
using Newtonsoft.Json;
using OfficeOpenXml.Style;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace maFileTool
{
    public class Program
    {
        public static string steam = Environment.CurrentDirectory + "\\Steam.xlsx";
        public static string steamtxt = Environment.CurrentDirectory + "\\Steam.txt";
        public static List<Account> accounts = new List<Account>();
        public static bool quit = false;
        static bool b = false;
        static void Main(string[] args)
        {
            string tedonstore = "Powered by tedonstore.com";
            Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (tedonstore.Length / 2)) + "}", tedonstore));

            #region Checks

            if (!System.IO.File.Exists(String.Format("{0}\\Settings.json", Environment.CurrentDirectory)))
            {
                new Utils().SaveSettings();
                Console.WriteLine("Please specify the settings in Settings.json");
                Console.ReadLine();
                return;
            }

            if (String.IsNullOrEmpty(Worker.settings.MailServer) || String.IsNullOrWhiteSpace(Worker.settings.MailServer))
            {
                Console.WriteLine("Please specify the MailServer in Settings.json");
                Console.ReadLine();
                return;
            }

            if (String.IsNullOrEmpty(Worker.settings.MailPort) || String.IsNullOrWhiteSpace(Worker.settings.MailPort))
            {
                Console.WriteLine("Please specify the MailPort in Settings.json");
                Console.ReadLine();
                return;
            }

            if (String.IsNullOrEmpty(Worker.settings.MailProtocol) || String.IsNullOrWhiteSpace(Worker.settings.MailProtocol))
            {
                Console.WriteLine("Please specify the MailProtocol in Settings.json");
                Console.ReadLine();
                return;
            }

            if (!System.IO.File.Exists(steam))
            {
                Console.WriteLine("Сan't find Steam.xlsx");
                Console.ReadLine();
                return;
            }

            if (!System.IO.File.Exists(steamtxt))
            {
                Console.WriteLine("Сan't find Steam.txt");
                Console.ReadLine();
                return;
            }

            #endregion

            string mode = Worker.settings.Mode;

            switch (mode)
            {
                case "EXCEL":
                    accounts = new Excel().ReadFromExcel(steam);
                    accounts.RemoveAll(t => String.IsNullOrEmpty(t.Login) || t.Login == "Логин" || t.Login == "Login");

                    int count = accounts.Count;
                    for (int i = 0; i < count; i++) 
                    {
                        Account account = accounts[i];
                        string date = account.Phone;
                        try
                        {
                            DateTime accDate = DateTime.ParseExact(date, "dd.MM.yy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                            if (accDate > DateTime.Now)
                            {
                                accounts.Remove(account);
                                i--; count--;
                            }
                        }
                        catch (FormatException)
                        {
                            accounts.Remove(account);
                            i--; count--;
                        }
                        catch (ArgumentNullException)
                        {
                            //Пустые оставляем
                        }
                    }

                    Console.WriteLine("Loaded - {0} accounts. Press enter to start.", accounts.Count());
                    Console.ReadLine();
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    break;
                case "TXT":
                    string[] acs = System.IO.File.ReadAllLines(steamtxt);
                    acs = acs.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    acs = acs.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    int id = 0;
                    foreach(var a in acs) 
                    {
                        if (!a.Contains(':')) continue;
                        id++;
                        Account account = new Account();
                        account.Id = id.ToString();
                        account.Login = a.Split(':')[0];
                        account.Password = a.Split(':')[1];
                        account.Email = a.Split(':')[2];
                        account.EmailPassword = a.Split(':')[3];
                        accounts.Add(account);
                    }
                    Console.WriteLine("Loaded - {0} accounts. Press enter to start.", accounts.Count());
                    Console.ReadLine();
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    break;
                default:
                    Console.WriteLine("Please specify the Mode in Settings.json");
                    Console.ReadLine();
                    return;
            }
            
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
