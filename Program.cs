// Observação: o Program.cs é quem vai orquestrar o uso das classes que foram separadas em módulos;
// De forma que o programe funcione plenamente como um todo.

using System;
using System.Text;
using AnaliseApresentacao;
using GestaoSalarioOrcamento;
using PortfyModuloInvestimentos;
using SimuladorFinanceiro;

namespace Portfy;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            // 1. Instanciação dos Módulos Principais
            Usuario usuario = new Usuario("Investidor", 5000.00m);
            CarteiraSimulada carteira = new CarteiraSimulada();

            // 2. Passagem do controle para a orquestração do Dashboard
            Dashboard dashboard = new Dashboard(usuario, carteira);
            dashboard.IniciarMenuPrincipal();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Erro inesperado na aplicação]: {ex.Message}");
            Console.ResetColor();
        }
    }
}