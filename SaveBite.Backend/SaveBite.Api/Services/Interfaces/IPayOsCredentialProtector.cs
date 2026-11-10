using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;
public interface IPayOsCredentialProtector
{
    string Protect(PlatformPayOsCredentials credentials);
    PlatformPayOsCredentials Unprotect(string protectedCredentials);
}
