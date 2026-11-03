using System.Globalization;
using System.Text;
using Portfy.Dtos.Relatorio;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Consolida, em um único retrato, os dados que já vivem no SQLite para o usuário
/// autenticado. Não guarda estado e não cria tabela nova: relatório é leitura.
/// Registrado como Scoped porque depende dos serviços que usam o AppDbContext.
public sealed class RelatorioService : IRelatorioService
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICarteiraService _carteiraService;
    private readonly IOrcamentoService _orcamentoService;
    private readonly IAporteService _aporteService;

    public RelatorioService(
        IUsuarioService usuarioService,
        ICarteiraService carteiraService,
        IOrcamentoService orcamentoService,
        IAporteService aporteService)
    {
        _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
        _carteiraService = carteiraService ?? throw new ArgumentNullException(nameof(carteiraService));
        _orcamentoService = orcamentoService ?? throw new ArgumentNullException(nameof(orcamentoService));
        _aporteService = aporteService ?? throw new ArgumentNullException(nameof(aporteService));
    }

    public RelatorioResponse Gerar(int usuarioId, DateOnly? inicio, DateOnly? fim)
    {
        if (inicio is not null && fim is not null && fim < inicio)
            throw new ArgumentException(
                "A data final do período não pode ser anterior à inicial.",
                nameof(fim));

        Usuario usuario = _usuarioService.ObterPorId(usuarioId)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        CarteiraSimulada carteira = _carteiraService.ObterCarteira(usuarioId);
        IReadOnlyCollection<Orcamento> orcamentos = _orcamentoService.ObterTodos(usuarioId);
        HistoricoDeAportes historico = _aporteService.ObterHistorico(usuarioId, inicio, fim);

        List<PosicaoRelatorioResponse> posicoes = carteira.Posicoes
            .Select(MapearPosicao)
            .ToList();

        decimal totalGasto = orcamentos.Sum(o => o.ValorGastoAtual);
        decimal totalOrcado = orcamentos.Sum(o => o.LimiteDefinido);

        return new RelatorioResponse
        {
            NomeUsuario = usuario.Nome,
            Inicio = inicio,
            Fim = fim,
            SalarioMensal = usuario.SalarioMensal,
            SaldoDisponivel = carteira.SaldoDisponivel,
            PatrimonioTotal = carteira.CalcularPatrimonioTotal(),
            TotalInvestido = posicoes.Sum(p => p.ValorAtual),
            TotalAportado = historico.Total,
            QuantidadeAportes = historico.Aportes.Count,
            TotalOrcado = totalOrcado,
            TotalGasto = totalGasto,
            SaldoOrcamentos = orcamentos.Sum(o => o.ObterSaldoRestante()),
            RendaDisponivel = usuario.CalcularRendaDisponivel(totalGasto),
            Aportes = historico.Aportes
                .Select(a => new AporteRelatorioResponse
                {
                    Id = a.Id,
                    DataAporte = a.DataAporte,
                    ValorAportado = a.ValorAportado,
                })
                .ToList(),
            Posicoes = posicoes,
            Orcamentos = orcamentos
                .Select(o => new OrcamentoRelatorioResponse
                {
                    Id = o.Id,
                    NomeCategoria = o.NomeCategoria,
                    LimiteDefinido = o.LimiteDefinido,
                    ValorGastoAtual = o.ValorGastoAtual,
                    SaldoRestante = o.ObterSaldoRestante(),
                    PercentualDoSalario = o.PercentualDoSalario,
                    Estourado = o.ValidarEstouroOrcamento(),
                    EhFixo = o.EhFixo,
                })
                .ToList(),
        };
    }

    public string GerarCsv(int usuarioId, DateOnly? inicio, DateOnly? fim)
    {
        RelatorioResponse relatorio = Gerar(usuarioId, inicio, fim);

        var csv = new StringBuilder();

        void Linha(string campo, string valor) =>
            csv.AppendLine($"{Campo(campo)};{Campo(valor)}");

        void Cabecalho(string titulo)
        {
            csv.AppendLine();
            csv.AppendLine(Campo(titulo));
        }

        Linha("Relatório financeiro Portfy", relatorio.NomeUsuario);
        Linha("Período", DescreverPeriodo(relatorio.Inicio, relatorio.Fim));

        Cabecalho("Resumo");
        Linha("Salário mensal", Dinheiro(relatorio.SalarioMensal));
        Linha("Saldo disponível", Dinheiro(relatorio.SaldoDisponivel));
        Linha("Patrimônio total", Dinheiro(relatorio.PatrimonioTotal));
        Linha("Total investido", Dinheiro(relatorio.TotalInvestido));
        Linha("Total aportado no período", Dinheiro(relatorio.TotalAportado));
        Linha("Quantidade de aportes", relatorio.QuantidadeAportes.ToString(CultureInfo.InvariantCulture));
        Linha("Total orçado", Dinheiro(relatorio.TotalOrcado));
        Linha("Total gasto", Dinheiro(relatorio.TotalGasto));
        Linha("Saldo dos orçamentos", Dinheiro(relatorio.SaldoOrcamentos));
        Linha("Renda disponível", Dinheiro(relatorio.RendaDisponivel));

        Cabecalho("Aportes");
        csv.AppendLine("Id;Data;Valor aportado");

        foreach (AporteRelatorioResponse aporte in relatorio.Aportes)
        {
            csv.AppendLine(string.Join(
                ';',
                aporte.Id.ToString(CultureInfo.InvariantCulture),
                Campo(aporte.DataAporte.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                Dinheiro(aporte.ValorAportado)));
        }

        Cabecalho("Investimentos");
        csv.AppendLine(
            "Ticker;Nome;Tipo;Quantidade;Preço médio;Preço atual;Valor investido;Valor atual;Resultado;Rentabilidade (%)");

        foreach (PosicaoRelatorioResponse posicao in relatorio.Posicoes)
        {
            csv.AppendLine(string.Join(
                ';',
                Campo(posicao.Ticker),
                Campo(posicao.Nome),
                Campo(posicao.Tipo),
                posicao.Quantidade.ToString(CultureInfo.InvariantCulture),
                Dinheiro(posicao.PrecoMedio),
                Dinheiro(posicao.PrecoAtual),
                Dinheiro(posicao.ValorInvestido),
                Dinheiro(posicao.ValorAtual),
                Dinheiro(posicao.Resultado),
                Numero(posicao.RentabilidadePercentual)));
        }

        Cabecalho("Orçamentos");
        csv.AppendLine(
            "Categoria;Limite definido;Valor gasto;Saldo restante;% do salário;Estourado;Fixo");

        foreach (OrcamentoRelatorioResponse orcamento in relatorio.Orcamentos)
        {
            csv.AppendLine(string.Join(
                ';',
                Campo(orcamento.NomeCategoria),
                Dinheiro(orcamento.LimiteDefinido),
                Dinheiro(orcamento.ValorGastoAtual),
                Dinheiro(orcamento.SaldoRestante),
                Numero(orcamento.PercentualDoSalario),
                orcamento.Estourado ? "Sim" : "Não",
                orcamento.EhFixo ? "Sim" : "Não"));
        }

        return csv.ToString();
    }

    private static PosicaoRelatorioResponse MapearPosicao(Posicao posicao)
    {
        decimal valorInvestido = Math.Round(
            posicao.Quantidade * posicao.PrecoMedio,
            2,
            MidpointRounding.AwayFromZero);

        decimal valorAtual = Math.Round(
            posicao.Quantidade * posicao.Ativo.PrecoAtual,
            2,
            MidpointRounding.AwayFromZero);

        decimal resultado = valorAtual - valorInvestido;

        // Posição com custo zero não tem percentual definido; zero evita divisão inválida.
        decimal rentabilidade = valorInvestido > 0
            ? Math.Round((resultado / valorInvestido) * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new PosicaoRelatorioResponse
        {
            Ticker = posicao.Ativo.Ticker,
            Nome = posicao.Ativo.Nome,
            Tipo = posicao.Ativo.Tipo.ToString(),
            Quantidade = posicao.Quantidade,
            PrecoMedio = posicao.PrecoMedio,
            PrecoAtual = posicao.Ativo.PrecoAtual,
            ValorInvestido = valorInvestido,
            ValorAtual = valorAtual,
            Resultado = resultado,
            RentabilidadePercentual = rentabilidade,
        };
    }

    private static string DescreverPeriodo(DateOnly? inicio, DateOnly? fim) => (inicio, fim) switch
    {
        (null, null) => "Completo",
        (not null, null) => $"A partir de {inicio:dd/MM/yyyy}",
        (null, not null) => $"Até {fim:dd/MM/yyyy}",
        _ => $"{inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}",
    };

    private static string Dinheiro(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    private static string Numero(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    // Escapa aspas e ponto e vírgula para o CSV não quebrar colunas.
    private static string Campo(string valor) =>
        valor.Contains(';') || valor.Contains('"')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
}
