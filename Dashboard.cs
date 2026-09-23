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
        Console.WriteLine("6. Simular Variação de Mercado (Oscilar Preços)");
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
                    valor,
                    DateTime.Now);

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

private void PressionarParaContinuar()
{
    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
    Console.ReadKey();
}

    // Métodos para exibição gráfica descritos na sua arquitetura
    public void GerarGraficoSalario()
    {
        
        // Implementação dos relatórios visuais
    }

    public void GerarGraficoPatrimonio()
    {
        
        // Implementação dos relatórios visuais
    }
}