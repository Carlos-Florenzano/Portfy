using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Portfy.Services;

/// Configuração de emissão e validação do JWT (ADR-010).
///
/// A chave de assinatura NÃO fica no repositório (arch §15, item 5): vem de
/// `Auth:Jwt:Chave`, normalmente a variável de ambiente `Auth__Jwt__Chave` ou
/// `dotnet user-secrets`. Ausente ou curta demais, o startup falha em vez de
/// operar com segredo fraco ou placeholder.
public sealed class JwtConfiguracao
{
    public const int TamanhoMinimoChaveEmBytes = 32;

    private JwtConfiguracao(
        string emissor,
        string audiencia,
        SymmetricSecurityKey chaveDeAssinatura,
        int minutosDeValidade)
    {
        Emissor = emissor;
        Audiencia = audiencia;
        ChaveDeAssinatura = chaveDeAssinatura;
        MinutosDeValidade = minutosDeValidade;
    }

    public string Emissor { get; }

    public string Audiencia { get; }

    public SymmetricSecurityKey ChaveDeAssinatura { get; }

    public int MinutosDeValidade { get; }

    public static JwtConfiguracao Ler(IConfiguration configuracao)
    {
        string emissor = configuracao["Auth:Jwt:Emissor"] ?? "Portfy.Api";
        string audiencia = configuracao["Auth:Jwt:Audiencia"] ?? "Portfy.Spa";
        string chave = configuracao["Auth:Jwt:Chave"] ?? string.Empty;
        int minutosDeValidade = configuracao.GetValue("Auth:Jwt:MinutosDeValidade", 60);

        if (Encoding.UTF8.GetByteCount(chave) < TamanhoMinimoChaveEmBytes)
        {
            throw new InvalidOperationException(
                "Auth:Jwt:Chave não está configurada ou tem menos de 32 bytes. " +
                "Defina a variável de ambiente Auth__Jwt__Chave (ou use dotnet user-secrets).");
        }

        if (minutosDeValidade <= 0)
        {
            throw new InvalidOperationException(
                "Auth:Jwt:MinutosDeValidade deve ser um número positivo de minutos.");
        }

        return new JwtConfiguracao(
            emissor,
            audiencia,
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            minutosDeValidade);
    }
}
