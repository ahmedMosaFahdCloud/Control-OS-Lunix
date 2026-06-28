namespace ControlOS.Api.Backend.Interfaces;

public interface ICredentialProtector
{
    string Protect(string value);

    string Unprotect(string protectedValue);
}
