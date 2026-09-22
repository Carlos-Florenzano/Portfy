using System;

namespace Portify.Modulo1
    //responsável pelo gerenciamento dos orçamentos
    // e pela organização dos gastos em diferentes categorias.
{
    public class Orcamento
    {
        public string NomeCategoria { get; private set; } = "";
        // Nome da categoria de gasto.
        // Exemplos: Alimentação, Transporte, Lazer ou Moradia.


        public decimal LimiteDefinido { get; private set; }
        // Valor máximo definido para aquela categoria de gasto.


        public decimal ValorGastoAtual { get; private set; }
        // Valor total que já foi gasto dentro da categoria.
        // Esse valor só é alterado através do método AdicionarDespesa.


        public decimal PercentualDoSalario { get; private set; }
        // Porcentagem do salário mensal destinada à categoria.


        public Orcamento(string nomeCategoria, decimal limiteDefinido, decimal percentualDoSalario)
        // Construtor responsável por criar um orçamento
        // já com sua categoria, limite e percentual definidos.
        {
            NomeCategoria = nomeCategoria;
            LimiteDefinido = limiteDefinido;
            PercentualDoSalario = percentualDoSalario;
            ValorGastoAtual = 0;
        }


        public void AdicionarDespesa(decimal valor)
        // Adiciona uma nova despesa ao orçamento.
        // O valor informado é somado ao ValorGastoAtual.
        {
            if (valor > 0)
            {
                ValorGastoAtual += valor;
            }
        }


        public bool ValidarEstouroOrcamento()
        // Verifica se o valor gasto ultrapassou o limite definido.
        // Retorna true caso o limite tenha sido ultrapassado
        // e false caso o orçamento ainda esteja dentro do limite.
        {
            return ValorGastoAtual > LimiteDefinido;
        }
    }
}
