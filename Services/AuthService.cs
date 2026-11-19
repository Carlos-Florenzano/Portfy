using System.Globalization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Emissão do JWT de acesso (ADR-010). Sem estado além da configuração,
/// por isso Singleton em Program.cs.
public sealed class AuthService : IAuthService
{
    private readonly JwtConfiguracao _configuracao;

    public AuthService(JwtConfiguracao configuracao)
    {
        _configuracao = configuracao ?? throw new ArgumentNullException(nameof(configuracao));
    }

    public TokenAcesso GerarToken(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        DateTime expiraEm = DateTime.UtcNow.AddMinutes(_configuracao.MinutosDeValidade);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _configuracao.Emissor,
            Audience = _configuracao.Audiencia,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(
                _configuracao.ChaveDeAssinatura,
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [ClaimsPortfy.Identificador] = usuario.Id.ToString(CultureInfo.InvariantCulture),
                [ClaimsPortfy.Login] = usuario.Login,
                [ClaimsPortfy.Nome] = usuario.Nome,
                [ClaimsPortfy.Jti] = Guid.NewGuid().ToString("N"),
            },
        };

        string token = new JsonWebTokenHandler().CreateToken(descritor);

        return new TokenAcesso(token, expiraEm);
    }
}
