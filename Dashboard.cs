using System;
using GestaoSalarioOrcamento;
using PortfyModuloInvestimentos;
using PortifyModulo1;
using SimuladorFinanceiro;

namespace AnaliseApresentacao;

public class Dashboard
{
    private readonly Usuario _usuario;
    private readonly CarteiraSimulada _carteira;
    private readonly List<Orcamento> _orcamentos = new();
    private readonly List<Ativo> _mercadoAtivos = new();
    private int _proximoAporteId = 1;


    // Construtor com Injeção de Dependências (Resolve os erros das imagens 1 e 2)
    public Dashboard(Usuario usuario, CarteiraSimulada carteira)
    {
        _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        _carteira = carteira ?? throw new ArgumentNullException(nameof(carteira));

        InicializarMercadoSimulado();
    }
    private void InicializarMercadoSimulado()
    {
        _mercadoAtivos.Add(new Ativo("PETR4", "Petrobras PN", TipoAtivo.Acao, 38.50m));
        _mercadoAtivos.Add(new Ativo("VALE3", "Vale ON", TipoAtivo.Acao, 62.10m));
        _mercadoAtivos.Add(new Ativo("HGLG11", "CSHG Logística", TipoAtivo.Fii, 164.20m));
        _mercadoAtivos.Add(new Ativo("TD2035", "Tesouro IPCA+ 2035", TipoAtivo.RendaFixa, 1000.00m));
        _mercadoAtivos.Add(new Ativo("BTC", "Bitcoin", TipoAtivo.Cripto, 350000.00m));
    }

    public void IniciarMenuPrincipal()
    {
        bool executar = true;

        while (executar)
        {
            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine($"   PAINEL FINANCEIRO PORTFY - {_usuario.Nome.ToUpper()}");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Salário Mensal:     R$ {_usuario.SalarioMensal:F2}");
            Console.WriteLine($"Saldo em Carteira:  R$ {_carteira.SaldoDisponivel:F2}");
            Console.WriteLine($"Patrimônio Total:   R$ {_carteira.CalcularPatrimonioTotal():F2}");
            Console.WriteLine("==================================================");
            Console.WriteLine("1. Gerenciar Salário / Orçamento");
            Console.WriteLine("2. Realizar Aporte Simulado");
            Console.WriteLine("3. Comprar / Vender Ativos");
            Console.WriteLine("4. Simular Variação de Mercado (Oscilar Preços)");
            Console.WriteLine("5. Exibir Gráfico de Salário e Patrimônio");
            Console.WriteLine("6. Exibir Gráfico de Patrimônio");
            Console.WriteLine("0. Sair");
            Console.WriteLine("==================================================");

            Console.Write("Escolha uma opção: ");

            string opcao = Console.ReadLine() ?? "";

            switch (opcao)
            {
                case "1":
                    // Menu/Ação de Orçamento
                    MenuOrcamentos();
                    break;
                case "2":
                    // Menu/Ação de Aportes
                    MenuAporteSimulado();
                    PressionarParaContinuar();
                    break;
                case "3":
                    // Menu/Ação de Negociação
                    MenuNegociacaoAtivos();
                    break;

                case "4":
                    SimularOscilacaoMercado();
                    break;

                case "5":
                    GerarGraficoSalario();
                    PressionarParaContinuar();
                    break;

                case "6":
                    GerarGraficoPatrimonio();
                    PressionarParaContinuar();
                    break;

                case "0":
                    executar = false;
                    Console.WriteLine("\nSaindo do Portfy... Até logo!");
                    break;
                
                default:
                    Console.WriteLine("\nOpção inválida! Tente novamente.");
                    PressionarParaContinuar();
                    break;
            }
        }
    }

    private void MenuOrcamentos()
    {
        Console.Clear();

        Console.WriteLine("=== GERENCIAR SALÁRIO E ORÇAMENTOS ===");
        Console.WriteLine("1. Criar Nova Categoria de Gasto");
        Console.WriteLine("2. Registrar Despesa em Categoria");
        Console.WriteLine("3. Listar Orçamentos e Saldo Disponível");
        Console.WriteLine("4. Atualizar Salário Mensal");
        Console.WriteLine("0. Voltar");

        Console.Write("Escolha uma opção: ");

        string op = Console.ReadLine() ?? "";

        switch (op)
        {
            case "1":

                Console.Write(
                    "\nNome da Categoria (Ex: Moradia, Lazer): ");

                string nome = Console.ReadLine() ?? "";

                Console.Write("Limite Máximo (R$): ");

                if (decimal.TryParse(
                        Console.ReadLine(),
                        out decimal limite)
                    && limite > 0)
                {
                    decimal percentual =
                        _usuario.SalarioMensal > 0
                            ? (limite / _usuario.SalarioMensal) * 100
                            : 0;

                    try
                    {
                        _orcamentos.Add(
                            new Orcamento(
                                nome,
                                limite,
                                percentual));

                        Console.WriteLine(
                            "\nCategoria criada com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"\nErro: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine(
                        "\nValor de limite inválido.");
                }

                break;

            case "2":

                if (_orcamentos.Count == 0)
                {
                    Console.WriteLine(
                        "\nNenhuma categoria cadastrada.");
                    break;
                }

                Console.WriteLine(
                    "\nSelecione a categoria:");

                for (int i = 0; i < _orcamentos.Count; i++)
                {
                    Console.WriteLine(
                        $"{i + 1}. {_orcamentos[i].NomeCategoria} " +
                        $"(Gasto Atual: R$ {_orcamentos[i].ValorGastoAtual:F2})");
                }

                Console.Write("Número: ");

                if (int.TryParse(
                        Console.ReadLine(),
                        out int idx)
                    && idx > 0
                    && idx <= _orcamentos.Count)
                {
                    Console.Write(
                        "Valor do gasto a adicionar: R$ ");

                    if (decimal.TryParse(
                            Console.ReadLine(),
                            out decimal gasto)
                        && gasto > 0)
                    {
                        var categoria = _orcamentos[idx - 1];

                        categoria.AdicionarDespesa(gasto);

                        if (categoria.ValidarEstouroOrcamento())
                        {
                            Console.WriteLine(
                                $"\n[ALERTA] Limite ultrapassado em " +
                                $"R$ {Math.Abs(categoria.ObterSaldoRestante()):F2}!");
                        }
                        else
                        {
                            Console.WriteLine(
                                $"\nDespesa computada. " +
                                $"Saldo restante: R$ {categoria.ObterSaldoRestante():F2}");
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            "\nValor de gasto inválido.");
                    }
                }
                else
                {
                    Console.WriteLine(
                        "\nCategoria inválida.");
                }

                break;

            case "3":

                Console.WriteLine(
                    "\n--- Categorias Cadastradas ---");

                decimal totalGasto = 0;

                foreach (var orc in _orcamentos)
                {
                    totalGasto += orc.ValorGastoAtual;

                    string status =
                        orc.ValidarEstouroOrcamento()
                            ? "[ESTOURADO]"
                            : "[REGULAR]";

                    Console.WriteLine(
                        $"{orc.NomeCategoria.PadRight(15)} | " +
                        $"Gasto: R$ {orc.ValorGastoAtual:F2} / " +
                        $"Limite: R$ {orc.LimiteDefinido:F2} | " +
                        $"{status}");
                }

                decimal saldoRestanteSalario =
                    _usuario.CalcularRendaDisponivel(totalGasto);

                Console.WriteLine(
                    $"\nRenda Restante do Salário: " +
                    $"R$ {saldoRestanteSalario:F2}");

                break;

            case "4":

                Console.Write(
                    "\nDigite o novo salário mensal: R$ ");

                if (decimal.TryParse(
                        Console.ReadLine(),
                        out decimal novoSalario))
                {
                    try
                    {
                        _usuario.AtualizarSalario(novoSalario);

                        Console.WriteLine(
                            "\nSalário atualizado com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"\nErro: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine(
                        "\nValor de salário inválido.");
                }

                break;

            case "0":
                return;

            default:
                Console.WriteLine(
                    "\nOpção inválida.");
                break;
        }

        PressionarParaContinuar();
    }

    private void MenuAporteSimulado()
    {
        Console.Clear();

        Console.WriteLine("=== REALIZAR APORTE SIMULADO ===");

        Console.Write(
            "Informe o valor a depositar na carteira: R$ ");

        if (decimal.TryParse(
                Console.ReadLine(),
                out decimal valor)
            && valor > 0)
        {
            try
            {
                // AporteSimulado vem do namespace SimuladorFinanceiro.
                var aporte = new AporteSimulado(
                    _proximoAporteId++,
                    valor,
                    _usuario.Id,
                    DateTime.Now
                    );

_carteira.AdicionarAporte(aporte);  
                Console.WriteLine(
                    $"\nAporte de R$ {valor:F2} " +
                    "creditado com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"\nErro ao adicionar aporte: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine(
                "\nValor inválido.");
        }

        PressionarParaContinuar();
    }

    private void MenuNegociacaoAtivos()
    {
        Console.Clear();

        Console.WriteLine("=== NEGOCIAÇÃO DE ATIVOS ===");

        Console.WriteLine(
            $"Saldo Disponível: R$ {_carteira.SaldoDisponivel:F2}\n");

        Console.WriteLine("1. Comprar Ativos");
        Console.WriteLine("2. Vender Ativos");
        Console.WriteLine("3. Ver Posições Atuais");
        Console.WriteLine("0. Voltar");

        Console.Write("Opção: ");

        string op = Console.ReadLine() ?? "";

        switch (op)
        {
            case "1":
                ComprarAtivoFluxo();
                break;

            case "2":
                VenderAtivoFluxo();
                break;

            case "3":
                ListarPosicoes();
                break;

            case "0":
                return;

            default:
                Console.WriteLine(
                    "\nOpção inválida.");
                break;
        }

        PressionarParaContinuar();
    }

    private void ComprarAtivoFluxo()
    {
        Console.WriteLine(
            "\n--- Catálogo de Ativos ---");

        for (int i = 0; i < _mercadoAtivos.Count; i++)
        {
            var ativo = _mercadoAtivos[i];

            Console.WriteLine(
                $"{i + 1}. [{ativo.Ticker}] " +
                $"{ativo.Nome} ({ativo.Tipo}) - " +
                $"Preço: R$ {ativo.PrecoAtual:F2}");
        }

        Console.Write(
            "\nSelecione o número do ativo: ");

        if (int.TryParse(
                Console.ReadLine(),
                out int idx)
            && idx > 0
            && idx <= _mercadoAtivos.Count)
        {
            var selecionado = _mercadoAtivos[idx - 1];

            Console.Write(
                $"Quantidade de {selecionado.Ticker} a comprar: ");

            if (int.TryParse(
                    Console.ReadLine(),
                    out int qtd)
                && qtd > 0)
            {
                try
                {
                    _carteira.ComprarAtivo(
                        selecionado,
                        qtd);

                    Console.WriteLine(
                        $"\nCompra efetuada! " +
                        $"{qtd}x {selecionado.Ticker} " +
                        "adicionados à sua carteira.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"\nFalha na compra: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(
                    "\nQuantidade inválida.");
            }
        }
        else
        {
            Console.WriteLine(
                "\nAtivo inválido.");
        }
    }

    private void VenderAtivoFluxo()
    {
        var posicoes = _carteira.Posicoes.ToList();

        if (posicoes.Count == 0)
        {
            Console.WriteLine(
                "\nVocê não tem ativos em custódia para vender.");

            return;
        }

        Console.WriteLine(
            "\n--- Suas Posições ---");

        for (int i = 0; i < posicoes.Count; i++)
        {
            var pos = posicoes[i];

            Console.WriteLine(
                $"{i + 1}. [{pos.Ativo.Ticker}] " +
                $"Qtd: {pos.Quantidade} | " +
                $"Preço Médio: R$ {pos.PrecoMedio:F2} | " +
                $"Cotação: R$ {pos.Ativo.PrecoAtual:F2}");
        }

        Console.Write(
            "\nSelecione o número da posição a vender: ");

        if (int.TryParse(
                Console.ReadLine(),
                out int idx)
            && idx > 0
            && idx <= posicoes.Count)
        {
            var selecionada = posicoes[idx - 1];

            Console.Write(
                $"Quantidade a vender " +
                $"(Máx: {selecionada.Quantidade}): ");

            if (int.TryParse(
                    Console.ReadLine(),
                    out int qtd)
                && qtd > 0)
            {
                try
                {
                    _carteira.VenderAtivo(
                        selecionada.Ativo,
                        qtd);

                    Console.WriteLine(
                        $"\nVenda efetuada! " +
                        $"{qtd}x {selecionada.Ativo.Ticker} " +
                        "negociados com sucesso.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"\nFalha na venda: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(
                    "\nQuantidade inválida.");
            }
        }
        else
        {
            Console.WriteLine(
                "\nPosição inválida.");
        }
    }

    private void ListarPosicoes()
    {
        Console.WriteLine(
            "\n--- Posições em Carteira ---");

        if (_carteira.Posicoes.Count == 0)
        {
            Console.WriteLine(
                "Nenhum ativo custodiado no momento.");

            return;
        }

        foreach (var posicao in _carteira.Posicoes)
        {
            decimal valorTotalPosicao =
                posicao.Quantidade *
                posicao.Ativo.PrecoAtual;

            Console.WriteLine(
                $"[{posicao.Ativo.Ticker.PadRight(6)}] " +
                $"Qtd: {posicao.Quantidade.ToString().PadRight(4)} | " +
                $"P.Médio: R$ {posicao.PrecoMedio:F2} | " +
                $"Atual: R$ {posicao.Ativo.PrecoAtual:F2} | " +
                $"Subtotal: R$ {valorTotalPosicao:F2}");
        }
    }

    private void SimularOscilacaoMercado()
    {
        Console.Clear();

        Console.WriteLine(
            "=== SIMULAÇÃO DE OSCILAÇÃO DE MERCADO ===");

        Console.WriteLine(
            "Variando os preços dos ativos entre -5% e +5%...\n");

        foreach (var ativo in _mercadoAtivos)
        {
            decimal precoAnterior =
                ativo.PrecoAtual;

            ativo.SimularVariacaoPreco();

            string variacaoFormatada =
                ativo.RentabilidadeSimulada >= 0
                    ? $"+{ativo.RentabilidadeSimulada:F2}%"
                    : $"{ativo.RentabilidadeSimulada:F2}%";

            Console.WriteLine(
                $"[{ativo.Ticker.PadRight(6)}] " +
                $"R$ {precoAnterior:F2} -> " +
                $"R$ {ativo.PrecoAtual:F2} " +
                $"({variacaoFormatada})");
        }

        Console.WriteLine(
            "\nPreços de mercado atualizados!");

        PressionarParaContinuar();
    }


    private void PressionarParaContinuar()
    {
        Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
        Console.ReadKey();
    }

    // Métodos para exibição gráfica descritos na sua arquitetura
    public void GerarGraficoSalario()
    {
        Console.Clear();

        Console.WriteLine(
            "=== GRÁFICO: DISTRIBUIÇÃO DO SALÁRIO ===");

        Console.WriteLine(
            $"Salário Base: R$ {_usuario.SalarioMensal:F2}\n");

        if (_orcamentos.Count == 0)
        {
            Console.WriteLine(
                "Nenhum orçamento configurado para projeção gráfica.");

            return;
        }

        foreach (var item in _orcamentos)
        {
            decimal proporcao =
                item.LimiteDefinido > 0
                    ? item.ValorGastoAtual /
                      item.LimiteDefinido
                    : 0;

            int blocos =
                (int)Math.Min(
                    proporcao * 25,
                    25);

            string barra =
                new string('█', blocos);

            Console.WriteLine(
                $"{item.NomeCategoria.PadRight(15)} " +
                $"[{barra.PadRight(25, '-')}] " +
                $"{(proporcao * 100):F0}% gasto");
        }
    }
    public void GerarGraficoPatrimonio()
    {
        // Implementação dos relatórios visuais
        Console.Clear();

        Console.WriteLine(
            "=== GRÁFICO: COMPOSIÇÃO DO PATRIMÔNIO ===");

        decimal saldo =
            _carteira.SaldoDisponivel;

        decimal patrimonio =
            _carteira.CalcularPatrimonioTotal();

        decimal alocado =
            Math.Max(
                0,
                patrimonio - saldo);

        decimal percSaldo =
            patrimonio > 0
                ? (saldo / patrimonio) * 100
                : 0;

        decimal percAlocado =
            patrimonio > 0
                ? (alocado / patrimonio) * 100
                : 0;

        int blocosSaldo =
            (int)(percSaldo * 0.25m);

        int blocosAlocado =
            (int)(percAlocado * 0.25m);

        Console.WriteLine(
            $"Patrimônio Total: R$ {patrimonio:F2}\n");

        Console.WriteLine(
            $"Saldo Líquido:  " +
            $"[{new string('█', blocosSaldo).PadRight(25, '-')}] " +
            $"{percSaldo:F1}% " +
            $"(R$ {saldo:F2})");

        Console.WriteLine(
            $"Investimentos:  " +
            $"[{new string('█', blocosAlocado).PadRight(25, '-')}] " +
            $"{percAlocado:F1}% " +
            $"(R$ {alocado:F2})");
    }
}