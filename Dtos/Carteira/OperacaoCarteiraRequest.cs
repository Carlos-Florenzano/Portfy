namespace Portfy.Dtos.Carteira;

/// Corpo de POST api/carteira/compras e POST api/carteira/vendas.
/// O dono da carteira vem do token; o frontend não envia id de usuário.
public sealed class OperacaoCarteiraRequest
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}
