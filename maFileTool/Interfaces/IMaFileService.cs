namespace maFileTool.Interfaces
{
    public interface IMaFileService
    {
        Task GetIP(CancellationToken cancellationToken = default);
        Task Authorization(CancellationToken cancellationToken = default);
        Task LinkAuthenticator(CancellationToken cancellationToken = default);
        Task<string?> GetAuthenticatorCodeFromEmail(string host, int port, CancellationToken cancellationToken = default);
        Task<string?> GetLoginCodeFromEmail(string host, int port, CancellationToken cancellationToken = default);
        Task<string?> ConfirmEmailForAdd(string host, int port, CancellationToken cancellationToken = default);
    }
}
