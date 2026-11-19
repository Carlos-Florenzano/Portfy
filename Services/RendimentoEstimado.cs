using Portfy.Models;

namespace Portfy.Services;

/// Resultado do rendimento mensal estimado de uma quantidade de um ativo.
/// O valor vem da implementação do subtipo (chamada polimórfica sobre `Ativo`),
/// não de um `switch` no serviço: é isso que prova o contrato polimórfico em uso.
public sealed record RendimentoEstimado(Ativo Ativo, int Quantidade, decimal Valor);
