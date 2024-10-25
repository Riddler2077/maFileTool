using maFileTool.Model;
using System.Text.Json.Serialization;
using maFileTool.Services.SteamAuth;

namespace maFileTool.Json
{
    [JsonSerializable(typeof(Settings))]
    [JsonSerializable(typeof(SteamGuardAccount))]
    [JsonSerializable(typeof(SessionData))]
    [JsonSerializable(typeof(TimeAligner))]
    [JsonSerializable(typeof(TimeAligner.TimeQuery))]
    [JsonSerializable(typeof(AuthenticatorLinker))]
    [JsonSerializable(typeof(AuthenticatorLinker.GetUserCountryResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.GetUserCountryResponseResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.SetAccountPhoneNumberResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.SetAccountPhoneNumberResponseResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.IsAccountWaitingForEmailConfirmationResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.IsAccountWaitingForEmailConfirmationResponseResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.AddAuthenticatorResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.FinalizeAuthenticatorResponse))]
    [JsonSerializable(typeof(AuthenticatorLinker.FinalizeAuthenticatorResponse.FinalizeAuthenticatorInternalResponse))]
    public partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
