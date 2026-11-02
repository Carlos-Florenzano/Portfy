namespace Portfy.Services.Contracts;

/// Nomes das políticas de rate limiting registradas em Program.cs e aplicadas
/// por atributo nos controllers. Constante única evita divergência de string.
public static class PoliticasDeRateLimit
{
    public const string Login = "login";

    /// Consultas de mercado: o cache interno já reduz a ida às fontes externas, e
    /// o limite por IP protege a cota do plano gratuito de quem consome a API.
    public const string Mercado = "mercado";
}
