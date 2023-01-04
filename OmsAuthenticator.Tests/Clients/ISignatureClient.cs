namespace OmsAuthenticator.Tests.Clients
{
    public interface ISignatureClient
    {
        Task<SignatureResponse> Sign(string value);
    }
}