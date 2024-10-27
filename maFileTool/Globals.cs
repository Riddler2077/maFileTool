using maFileTool.Model;

namespace maFileTool
{
    public class Globals
    {
        public static string SettingsPath = Path.Combine(Program.ExecutablePath, "Settings.json");

        public static string ProxyPath = Path.Combine(Program.ExecutablePath, "Proxy.txt");

        public static string ExcelFilePath = Path.Combine(Program.ExecutablePath, "Steam.xlsx");

        public static string TxtFilePath = Path.Combine(Program.ExecutablePath, "Steam.txt");

        public static string MaFilesFolder = Path.Combine(Program.ExecutablePath, "maFiles");

        public static Settings Settings = new Settings();

        public static List<Account> Accounts = new List<Account>();

        public static List<string> Proxies = new List<string>();
    }
}
