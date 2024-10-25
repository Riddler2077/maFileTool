using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using maFileTool.Services;
using maFileTool.Interfaces;
using maFileTool.Model;
using maFileTool.Utilities;
using System.Net;
using System.Collections.Concurrent;
using System.Reflection;

namespace maFileTool
{
    public class Program
    {
        public static readonly string ExecutablePath = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        public static IServiceProvider? ServiceProvider;

        private static readonly CancellationTokenSource cancellationTokenSource = new();

        private static ConcurrentDictionary<int, Task> runningTasks = new ConcurrentDictionary<int, Task>();

        private static bool shouldStop = false;

        private static int offsetCounter = 0;

        static async Task Main(string[] args)
        {
            //Инициализация
            await Initialization();

            // Настройка DI контейнера
            ServiceProvider = RegisterServicesAsync();

            Log.Logger.Information("Loaded - {0} accounts. Press any key to start.", Globals.Accounts.Count);
            Console.ReadKey();

            _ = Task.Run(() => MonitorUserInput(), cancellationTokenSource.Token);

            // Параллельное выполнение задач
            Enumerable.Range(0, Int32.Parse(Globals.Settings.ThreadCount)).AsParallel().ForAll(x =>
            {
                StartUp(x);
            });

            await Task.Delay(-1);
        }

        static void StartUp(int taskId, long oldOffset = 0)
        {
            int currentOffset = Interlocked.Increment(ref offsetCounter) - 1; // Получаем уникальное смещение
            int maxOffset = Globals.Accounts.Count;

            // Проверка на достижение максимального смещения
            if (currentOffset >= maxOffset)
            {
                //Log.Logger.Warning($"Достигнуто максимальное смещение {maxOffset}. Остановка всех задач.");
                shouldStop = true;
                return;
            }

            string login = Globals.Accounts[currentOffset].Login;

            Task task = Task.Run(async () =>
            {
                await Run(login);
            });

            // Сохраняем запущенную задачу в коллекции
            runningTasks[taskId] = task;

            task.ContinueWith(t =>
            {
                // Если задача завершилась успешно (без ошибок)
                if (!t.IsFaulted && !t.IsCanceled)
                {
                    if (!shouldStop) // Проверяем, можно ли запускать новые задачи
                        StartUp(taskId); //Рекурсивно запускаем следующий аккаунт
                    else
                    {
                        // Удаляем задачу из словаря, если она завершена и новые не запускаются
                        runningTasks.TryRemove(taskId, out _);

                        // Если все задачи завершены, выведем информацию об этом
                        if (runningTasks.IsEmpty)
                        {
                            Log.Logger.Information("Все задачи остановлены.");
                        }
                    }
                }
                else
                {
                    Log.Logger.Error("{0} => Ошибка! {1}", login, t.Exception);
                }
            });
        }

        private static void MonitorUserInput()
        {
            // Перехватываем Ctrl+C для плавной остановки
            Console.CancelKeyPress += (sender, e) =>
            {
                Log.Logger.Warning("Сочетание Ctrl+C нажато. Плавное завершение работы приложения.");
                e.Cancel = true; // Отменяет стандартное завершение по Ctrl+C
                shouldStop = true; // Запускает плавную остановку
            };

            // Цикл для непрерывного мониторинга других клавиш или условий
            while (!cancellationTokenSource.Token.IsCancellationRequested || shouldStop)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);

                    // Проверка на сочетание Ctrl+Q
                    if (key.Key == ConsoleKey.Q && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        Console.WriteLine("Сочетание Ctrl+Q нажато. Завершение работы приложения.");
                        cancellationTokenSource.Cancel(); // Запускает плавную остановку
                        break;
                    }
                }
            }
        }

        private static IServiceProvider RegisterServicesAsync()
        {
            ServiceCollection serviceCollection = new ServiceCollection();

            ConfigureHttpClients(serviceCollection);

            ConfigureMaFileServices(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureHttpClients(IServiceCollection services)
        {
            // Используем либо список прокси, либо один "пустой" элемент для стандартного клиента
            var proxies = Globals.Proxies.Count >= 1 ? Globals.Proxies : new List<string>() { "Default" };

            foreach (string proxy in proxies)
            {
                services.AddHttpClient(proxy, httpClient =>
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    Proxy = ProxyManager.ConvertProxy(proxy),
                    UseProxy = Globals.Proxies.Count >= 1 ? true : false,
                });
            }
        }

        private static void ConfigureMaFileServices(IServiceCollection services)
        {
            foreach (Account account in Globals.Accounts)
            {
                // Регистрация MaFileService с параметрами
                services.AddKeyedTransient<IMaFileService, MaFileService>(account.Login, (provider, _) =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var randomProxy = Globals.Proxies.Count >= 1 ? Globals.Proxies[new Random().Next(Globals.Proxies.Count)] : "Default";
                    var httpClient = httpClientFactory.CreateClient(randomProxy);
                    return new MaFileService(account.Login, account.Password, account.Email, account.EmailPassword, httpClient);
                });
            }
        }

        private static async Task Initialization()
        {
            // Конфигурация Serilog для логирования
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(
                    path: "Logs/log-.txt",         // Путь к файлу с шаблоном для имени
                    rollingInterval: RollingInterval.Day, // Ротация файлов ежедневно
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}" // Формат вывода
                )
                .CreateLogger();


            Log.Logger.Warning("Powered by Baby Yoda.");

            Log.Logger.Information("Initialization started.");

            // Получаем все сборки в текущем домене
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Получаем тип из библиотеки, версию которой нужно узнать
            //var assembly = typeof(SteamKit2.SteamClient).Assembly;
            var assembly = Assembly.GetExecutingAssembly();

            // Получаем информацию о версии сборки
            var name = assembly.GetName().Name;
            var version = assembly.GetName().Version;
            //var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version?.ToString();

            Log.Logger.Information("{0} {1} running under .NET {2}", name, version, Environment.Version);

            if (!Directory.Exists(Globals.MaFilesFolder))
                Directory.CreateDirectory(Globals.MaFilesFolder);

            if (await Json.Document.ReadJsonAsync<Settings>(Globals.SettingsPath) is Settings settings)
            {
                Globals.Settings = settings;
                Log.Logger.Information("Settings file loaded.");
            }
            else
            {
                Log.Logger.Error("Settings file not found!");
                return;
            }

            if ((await Excel.ReadAccountsFromExcel(Globals.ExcelFilePath))
                .Where(a => a.Email is not null)
                .Where(a => a.RevocationCode is null)
                .ToList() is List<Account> accounts)
            {
                Globals.Accounts = accounts;
                Log.Logger.Information("Excel file loaded.");
            }
            else
            {
                Log.Logger.Error("Excel file not found!");
                return;
            }

            if (File.Exists(Globals.ProxyPath) && new FileInfo(Globals.ProxyPath).Length > 0)
                if (await ProxyManager.LoadProxiesAsync(Globals.ProxyPath) is List<string> proxies)
                {
                    Globals.Proxies = proxies;
                    Log.Logger.Information("Loaded - {0} proxies.", Globals.Proxies.Count);
                }
        }

        private static async Task Run(string login)
        {
            if (Program.ServiceProvider!.GetKeyedService<IMaFileService>(login) is IMaFileService maFileService)
            {
                await maFileService.GetIP(cancellationTokenSource.Token);
                await maFileService.Authorization(cancellationTokenSource.Token);
                await maFileService.LinkAuthenticator(cancellationTokenSource.Token);
            }
        }
    }
}
