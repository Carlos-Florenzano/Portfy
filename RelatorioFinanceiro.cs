using System;
using System.Collections.Generic; // Fornece estruturas de dados genéricas,
// como List<T>, Dictionary<TKey, TValue> e outras coleções.
// Neste código, é utilizado para trabalhar com List<Orcamento>.
using GestaoSalarioOrcamento;
using PortfyModuloInvestimentos;
using PortifyModulo1;

namespace AnaliseApresentacao;

public class RelatorioFinanceiro
{
    private readonly Usuario _usuario;
    private readonly CarteiraSimulada _carteira;
    private readonly List<Orcamento> _orcamentos;

    public RelatorioFinanceiro(
        Usuario usuario,
        CarteiraSimulada carteira,
        List<Orcamento> orcamentos)
    {
        _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        _carteira = carteira ?? throw new ArgumentNullException(nameof(carteira));
        _orcamentos = orcamentos ?? new List<Orcamento>();
    }
}