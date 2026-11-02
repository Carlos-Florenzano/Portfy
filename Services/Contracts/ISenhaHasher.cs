namespace Portfy.Services.Contracts;

/// Contrato de hash de senha (ADR-013): o modelo guarda o hash pronto, quem
/// calcula e verifica é este serviço. `PrecisaAtualizar` existe para migrar
/// hashes legados no primeiro login bem-sucedido, sem bloquear usuários antigos.
public interface ISenhaHasher
{
    string CriarHash(string senha);

    bool Verificar(string senha, string hashArmazenado);

    bool PrecisaAtualizar(string hashArmazenado);
}
