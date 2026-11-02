namespace Portfy.Services.Contracts;

/// Falha de uma fonte externa de dados (rede, timeout, credencial ausente ou
/// limite de uso). O middleware traduz esta exceção para 503, preservando a
/// diferença entre "o dado não existe" (404) e "não foi possível perguntar" (503).
///
/// A mensagem nunca contém URL: as fontes recebem a credencial na query string.
public sealed class ServicoExternoIndisponivelException : Exception
{
    public ServicoExternoIndisponivelException()
        : base("O serviço externo está indisponível.")
    {
    }

    public ServicoExternoIndisponivelException(string message)
        : base(message)
    {
    }

    public ServicoExternoIndisponivelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
