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
        public void ExibirRelatorio()
    {
        Console.Clear();

        Console.WriteLine("==================================================");
        Console.WriteLine("           RELATÓRIO FINANCEIRO PORTFY");
        Console.WriteLine("==================================================");

        Console.WriteLine($"\nCliente: {_usuario.Nome}");
        Console.WriteLine($"Salário Mensal: R$ {_usuario.SalarioMensal:F2}");

        Console.WriteLine("\n--- RESUMO DA CARTEIRA ---");

        Console.WriteLine(
            $"Saldo Disponível: R$ {_carteira.SaldoDisponivel:F2}");

        Console.WriteLine(
            $"Patrimônio Total: R$ {_carteira.CalcularPatrimonioTotal():F2}");

        Console.WriteLine("\n--- APORTES REALIZADOS ---");

        if (_carteira.Aportes.Count == 0)
        {
            Console.WriteLine("Nenhum aporte realizado.");
        }
        else
        {
            decimal totalAportado = 0;

            foreach (var aporte in _carteira.Aportes)
            {
                totalAportado += aporte.ValorAportado;

                Console.WriteLine(
                    $"ID: {aporte.Id} | " +
                    $"Valor: R$ {aporte.ValorAportado:F2} | " +
                    $"Data: {aporte.DataAporte:dd/MM/yyyy HH:mm}"); //dd = dia, MM = mes, yyyy= ano, HH = hora, mm = minuto
            }

            Console.WriteLine(
                $"\nTotal aportado: R$ {totalAportado:F2}");
        }

        Console.WriteLine("\n--- INVESTIMENTOS ---");

        if (_carteira.Posicoes.Count == 0)
        {
            Console.WriteLine("Nenhum investimento em carteira.");
        }
        else
        {
            foreach (var posicao in _carteira.Posicoes)
            {
                decimal valorAtual =
                    posicao.Quantidade * posicao.Ativo.PrecoAtual;

                Console.WriteLine(
                    $"[{posicao.Ativo.Ticker}] " +
                    $"{posicao.Ativo.Nome} | " +
                    $"Tipo: {posicao.Ativo.Tipo} | " +
                    $"Quantidade: {posicao.Quantidade} | " +
                    $"Valor Atual: R$ {valorAtual:F2}");
            }
        }

        Console.WriteLine("\n--- ORÇAMENTOS ---");

        if (_orcamentos.Count == 0)
        {
            Console.WriteLine("Nenhum orçamento cadastrado.");
        }
        else
        {
            decimal totalDespesas = 0;

            foreach (var orcamento in _orcamentos)
            {
                totalDespesas += orcamento.ValorGastoAtual;

                Console.WriteLine(
                    $"{orcamento.NomeCategoria} | " +
                    $"Gasto: R$ {orcamento.ValorGastoAtual:F2} | " +
                    $"Limite: R$ {orcamento.LimiteDefinido:F2}");
            }

            Console.WriteLine(
                $"\nTotal de despesas: R$ {totalDespesas:F2}");

            Console.WriteLine(
                $"Renda disponível: " +
                $"R$ {_usuario.CalcularRendaDisponivel(totalDespesas):F2}");
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine("Fim do relatório.");
    }
}