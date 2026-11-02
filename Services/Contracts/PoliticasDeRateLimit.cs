namespace Portfy.Services.Contracts;

/// Nomes das políticas de rate limiting registradas em Program.cs e aplicadas
/// por atributo nos controllers. Constante única evita divergência de string.
public static class PoliticasDeRateLimit
{
    public const string Login = "login";
}
