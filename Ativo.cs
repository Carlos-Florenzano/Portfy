using System;

namespace Portfy.ModuloInvestimentos
{
    /// Categorias de ativos disponíveis para simulação no Portfy.
    public enum TipoAtivo
    {
        Acao,
        Fii,
        RendaFixa,
        Cripto,
        Etf
    }

    /// Representa um ativo financeiro individual no catálogo do sistema.
    public class Ativo
    {
        // Propriedades do ativo individual (encapsulamento ajustado para leitura pública e alteração restrita)
        public string Ticker { get; private set; } // Leitura pública (get), alteração restrita à classe (private set)
        public string Nome { get; private set; }
        public TipoAtivo Tipo { get; private set; }
        public decimal PrecoAtual { get; private set; }
        public decimal RentabilidadeSimulada { get; private set; }

        // Gerador de números aleatórios para simulação de mercado
        private static readonly Random _random = new Random();

        // CONSTRUTOR: vai definir como os dados inseridos serão convertidos de forma conveniente para os métodos ao criar uma nova classe.
        public Ativo(string ticker, string nome, TipoAtivo tipo, decimal precoInicial)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("O ticker não pode ser vazio.", nameof(ticker));

            if (precoInicial <= 0)
                throw new ArgumentException("O preço inicial deve ser maior que zero.", nameof(precoInicial));

            Ticker = ticker.ToUpper();
            Nome = nome;
            Tipo = tipo;
            PrecoAtual = precoInicial;
            RentabilidadeSimulada = 0m;  // Observação: o "m" diz ao compilador do C# que o 0 deve ser tratado estritamente como o tipo decimal.
        }


        // MÉTODOS

        /// Atualiza o preço atual do ativo para um valor específico informado.
        public void AtualizarPreco(decimal novoPreco) // É tipo um try except "primitivo", mas funcional e mais modular às regras de negócio, como se fosse um filtro de passagem.
        {
            if (novoPreco <= 0)
            {
                // throw: dispara uma exceção na memória
                // new ArgumentException() instancia um padrão de erro do .NET usado especificamente quando um argumento recebido por um método não atende aos requisitos esperados.
                // O nameof(novoPreco) converte o nome da variável para texto ("novoPreco").
                // Serve para garantir que o log de erro aponte a variável correta mesmo se ela for renomeada no futuro.   
                throw new ArgumentException($"[Erro] O preço do ativo '{Ticker}' deve ser maior que zero.", nameof(novoPreco));
            }

            PrecoAtual = novoPreco;
        }

        /// Simula uma variação aleatória de preço no mercado no intervalo de -5% a +5%.
        public void SimularVariacaoPreco()
        {
            // Sorteia um número do tipo double no intervalo de -0.05 a +0.05 (-5% a +5%)
            double percentualVariacao = (_random.NextDouble() * 0.10) - 0.05; // NextDouble() é uma built-in function que pede para a _random os dados especificamente em double.

            // Reutiliza a lógica de cálculo repassando a variação sorteada
            SimularVariacaoPreco((decimal)percentualVariacao);
        }

        /// Sobrecarga: Permite simular a variação informando uma porcentagem específica diretamente.
        public void SimularVariacaoPreco(decimal percentualVariacao)
        {
            // Faz a conversão explícita de double para decimal para manter a precisão financeira
            // "fator" tem um espaço na memória para decimais, no qual vai receber a soma de 1 (int) + possível double
            // se o número 1 for com um double, o resultado dará double pela conversão do C#
            // então o "(decimal)" força o resultado a ser convertido para decimal antes de entrar na variável "fator", que está preparada para armazenar um dado do tipo decimal
            // sem isso, um erro seria certo caso percentualVariacao fosse número quebrado
            decimal fator = (decimal)(1m + percentualVariacao);

            decimal precoAntigo = PrecoAtual;
            PrecoAtual = Math.Round(PrecoAtual * fator, 2); // Math.Round() é mais uma built-in function, que vai nesse caso arredondar o resultado para até 2 casas decimais depois da vírgula

            // Calcula o percentual de variação ocorrido na simulação
            if (precoAntigo > 0)
            {
                RentabilidadeSimulada = Math.Round(((PrecoAtual - precoAntigo) / precoAntigo) * 100, 2);
            }
        }
    }
}