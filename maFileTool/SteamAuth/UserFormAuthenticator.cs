using maFileTool.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SteamKit2.Authentication;
using System.Diagnostics;

namespace maFileTool.Services.SteamAuth
{
    internal class UserFormAuthenticator : IAuthenticator
    {
        private SteamGuardAccount account;
        private int deviceCodesGenerated = 0;

        public UserFormAuthenticator(SteamGuardAccount account)
        {
            this.account = account;
        }

        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            return Task.FromResult(false);
        }

        public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            // If a code fails wait 30 seconds for a new one to regenerate
            if (previousCodeWasIncorrect)
            {
                // After 2 tries tell the user that there seems to be an issue
                if (deviceCodesGenerated > 2)
                    Debug.WriteLine("There seems to be an issue logging into your account with these two factor codes. Are you sure SDA is still your authenticator?");

                await Task.Delay(30 * 1000);
            }

            string deviceCode = await account.GenerateSteamGuardCodeAsync();
            deviceCodesGenerated++;

            return deviceCode;
        }

        public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            /*string message = "Enter the code sent to your email:";
            if (previousCodeWasIncorrect)
            {
                message = "The code you provided was invalid. Enter the code sent to your email:";
            }

            InputForm emailForm = new InputForm(message);
            emailForm.ShowDialog();*/

            Log.Logger.Information("{0} => Waiting login code from email.", account.AccountName);

            if (Program.ServiceProvider!.GetKeyedService<IMaFileService>(account.AccountName) is IMaFileService maFileService) 
            {
                if (await maFileService.GetLoginCodeFromEmail(Globals.Settings.MailServer, Int32.Parse(Globals.Settings.MailPort)) is string loginCode) 
                {
                    return loginCode;
                }
            }

            return "";//emailForm.txtBox.Text);
        }
    }
}
