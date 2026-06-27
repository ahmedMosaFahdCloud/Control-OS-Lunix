namespace Control_OS_Lunix.Backend.Interfaces;

public interface ICredentialProtector
{
    string Protect(string value);

    string Unprotect(string protectedValue);
}
