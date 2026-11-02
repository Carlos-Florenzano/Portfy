namespace Portfy.Services.Contracts;

/// Contrato de hash de senha (ADR-013): o modelo guarda o hash pronto, quem
/// calcula e verifica é este serviço. Apenas o formato BCrypt é aceito (ADR-018).
public interface ISenhaHasher
{
    string CriarHash(string senha);

    bool Verificar(string senha, string hashArmazenado);
}
