using maFileTool.Model;
using System.Numerics;

namespace maFileTool.Utilities
{
    public class Txt
    {
        public static async Task<List<Account>> ReadAccountsFromTxt(string filename)
        {
            string[] lines = await File.ReadAllLinesAsync(filename);
            
            List<Account> accounts = new List<Account>();

            foreach (string line in lines)
            {
                var parts = line.Split(':');

                accounts.Add(new Account()
                {
                    Id = Array.IndexOf(lines, line).ToString(),
                    Login = parts.Length > 0 ? parts[0] : string.Empty,
                    Password = parts.Length > 1 ? parts[1] : string.Empty,
                    Email = parts.Length > 2 ? parts[2] : string.Empty,
                    EmailPassword = parts.Length > 3 ? parts[3] : string.Empty,
                    Phone = parts.Length > 4 ? parts[4] : string.Empty,
                    RevocationCode = parts.Length > 5 ? parts[5] : string.Empty
                });
            }

            return accounts;
        }

        public static async Task LineChanger(string newText, string filename, int lineToEdit)
        {
            string[] array = await File.ReadAllLinesAsync(filename);
            array[lineToEdit] = newText;
            await File.WriteAllLinesAsync(filename, array);
        }
    }
}
