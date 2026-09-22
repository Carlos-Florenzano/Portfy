using System;
using GestaoSalarioOrcamento;
using PortfyModuloInvestimentos;
using PortifyModulo1;


namespace AnaliseApresentacao;

public class Dashboard
{
    private readonly Usuario _usuario;
    private readonly CarteiraSimulada _carteira;

    // Construtor com Injeção de Dependências (Resolve os erros das imagens 1 e 2)
    public Dashboard(Usuario usuario, CarteiraSimulada carteira)
    {
        _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        _carteira = carteira ?? throw new ArgumentNullException(nameof(carteira));
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
        Console.WriteLine("4. Exibir Gráfico de Salário e Patrimônio");
        Console.WriteLine("0. Sair");
        Console.WriteLine("==================================================");
        Console.Write("Escolha uma opção: ");

        string opcao = Console.ReadLine() ?? "";

        switch (opcao)
        {
            case "1":
                // Menu/Ação de Orçamento
                Console.WriteLine("\n[Em breve: Gestão de Salário/Orçamento]");
                PressionarParaContinuar();
                break;
            case "2":
                // Menu/Ação de Aportes
                Console.WriteLine("\n[Em breve: Aporte Simulado]");
                PressionarParaContinuar();
                break;
            case "3":
                // Menu/Ação de Negociação
                Console.WriteLine("\n[Em breve: Compra e Venda de Ativos]");
                PressionarParaContinuar();
                break;
            case "4":
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