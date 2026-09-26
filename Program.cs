// Observação: o Program.cs é quem vai orquestrar o uso das classes que foram separadas em módulos;
// De forma que o programe funcione plenamente como um todo.

using System;
using System.Globalization;
using System.Text;
using Portfy.Apresentacao;
using Portfy.Dominio.Financas;
using Portfy.Dominio.Investimentos;

namespace Portfy;

public class Program
{
    public static void Main(string[] args)
    {
        CultureInfo culturaPtBr = CultureInfo.GetCultureInfo("pt-BR"); // Define a cultura para pt-BR (Português do Brasil) para formatação de datas, números e moeda.
        CultureInfo.CurrentCulture = culturaPtBr;
        CultureInfo.CurrentUICulture = culturaPtBr;
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
