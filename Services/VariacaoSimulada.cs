using Portfy.Models;

namespace Portfy.Services;

/// Resultado de uma simulação de variação de preço: o ativo já atualizado
/// e o preço de antes, para o controller montar a resposta sem refazer a conta.
public sealed record VariacaoSimulada(Ativo Ativo, decimal PrecoAnterior);
