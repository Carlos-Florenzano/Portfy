using System.Text.Json.Serialization;

namespace Portfy.Models;

public class CarteiraSimulada
{
    // Objeto de sincronização para garantir Thread Safety em ambientes multithread
    private readonly object _lock = new();

    // Chave primária gerada pelo banco (arch §9.4). Fora do JSON para preservar
    // exatamente o contrato HTTP atual de GET api/carteira.
    [JsonIgnore]
    public int Id { get; private set; }

    // Dono da carteira. É o que separa "minha carteira" de "a carteira" (arch §9.1).
    [JsonIgnore]
    public int UsuarioId { get; private set; }

    // Nome da simulação. O usuário pode ter mais de uma carteira; a padrão é "Principal".
    public string Nome { get; private set; } = string.Empty;

    // Propriedades com private set para integridade dos dados
    public decimal SaldoDisponivel { get; private set; }

    // Listas internas privadas para proteger o estado do objeto
    private readonly List<Posicao> _posicoes = new();
    private readonly List<AporteSimulado> _aportes = new();

    // Propriedades públicas apenas para leitura (Exposição segura de coleções)
    public IReadOnlyCollection<Posicao> Posicoes
    {
        get
        {
            lock (_lock)
            {
                return _posicoes.AsReadOnly();
            }
        }
    }

    public IReadOnlyCollection<AporteSimulado> Aportes
    {
        get
        {
            lock (_lock)
            {
                return _aportes.AsReadOnly();
            }
        }
    }

    // Construtor exigido pelo EF Core (arch §13.1) para materializar uma carteira persistida.
    protected CarteiraSimulada()
    {
    }

    public CarteiraSimulada(int usuarioId, string nome, decimal saldoInicial)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome da carteira não pode ser vazio.", nameof(nome));

        if (saldoInicial < 0)
            throw new ArgumentException("O saldo inicial não pode ser negativo.", nameof(saldoInicial));

        UsuarioId = usuarioId;
        Nome = nome;
        SaldoDisponivel = saldoInicial;
    }

    // Método para receber aportes com proteção de concorrência
    public void AdicionarAporte(AporteSimulado aporte)
    {
        if (aporte == null)
            throw new ArgumentNullException(nameof(aporte));

        if (Id > 0 && aporte.CarteiraId != Id)
            throw new ArgumentException(
                "O aporte pertence a outra carteira.",
                nameof(aporte));

        // O _lock trava um aporte por vez, de forma a livrar o problema de dois pedidos um em cada thread, que poderia
        //sobrescrever e apagar os dados de um deles, é uma ferramenta muito específica, mas necessária, de baixo nível do .NET.
        // É o famoso thread safety, que garante esse tipo de segurança, que é crucial ainda mais em dados financeiros.
        lock (_lock)
        {
            _aportes.Add(aporte);
            SaldoDisponivel += aporte.ValorAportado;
        }
    }

    // Método de compra com thread safety (para evitar que, ao pedir mais de uma vez, que caso cada processo seja feito em uma thread, que evite de corromper a lista)
    public void ComprarAtivo(Ativo ativo, int quantidade)
    {
        if (ativo == null)
            throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade para compra deve ser maior que zero.", nameof(quantidade));

        lock (_lock)
        {
            decimal valorTotalCompra = ativo.PrecoAtual * quantidade;

            if (valorTotalCompra > SaldoDisponivel)
                throw new InvalidOperationException($"Saldo insuficiente para realizar a compra. Saldo atual: R$ {SaldoDisponivel:F2}.");

            Posicao? posicao = _posicoes.FirstOrDefault(p => p.Ativo == ativo);

            if (posicao == null)
            {
                posicao = new Posicao(ativo, quantidade, ativo.PrecoAtual);
                posicao.VincularCarteira(Id);
                _posicoes.Add(posicao);
            }
            else
            {
                posicao.AtualizarPrecoMedioEQuantidade(quantidade, ativo.PrecoAtual);
            }

            SaldoDisponivel -= valorTotalCompra;
        }
    }

    // Método de venda com thread safety (para resolver o problema similar ao do método de compra com thread safety)
    public void VenderAtivo(Ativo ativo, int quantidade)
    {
        if (ativo == null)
            throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade para venda deve ser maior que zero.", nameof(quantidade));

        lock (_lock)
        {
            Posicao? posicao = _posicoes.FirstOrDefault(p => p.Ativo == ativo);

            if (posicao == null || posicao.Quantidade < quantidade)
                throw new InvalidOperationException("Quantidade de ativos insuficiente em carteira para realizar a venda.");

            decimal valorVenda = ativo.PrecoAtual * quantidade;

            posicao.ReduzirQuantidade(quantidade);
            SaldoDisponivel += valorVenda;

            if (posicao.Quantidade == 0)
            {
                _posicoes.Remove(posicao);
            }
        }
    }

    public decimal CalcularPatrimonioTotal()
    {
        lock (_lock)
        {
            // Patrimônio Total = Saldo em Dinheiro + Valor Atual das Posições
            decimal valorTotalEmAtivos = _posicoes.Sum(p => p.Quantidade * p.Ativo.PrecoAtual);
            return SaldoDisponivel + valorTotalEmAtivos;
        }
    }
}

// Classe Posicao imutável e protegida
public class Posicao
{
    // Chave primária gerada pelo banco (arch §9.4). Fora do JSON para preservar
    // exatamente o contrato HTTP atual de GET api/carteira.
    [JsonIgnore]
    public int Id { get; private set; }

    // Chaves estrangeiras explícitas (arch §9.1). As colunas do banco mantêm os
    // nomes históricos (CarteiraSimuladaId/AtivoId) para não renomear dados existentes.
    [JsonIgnore]
    public int CarteiraId { get; private set; }

    [JsonIgnore]
    public int AtivoId { get; private set; }

    // Preenchido pelo EF Core; a posição só existe associada a um ativo persistido.
    public Ativo Ativo { get; private set; } = null!;
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }

    // Construtor exigido pelo EF Core (arch §13.1); o domínio usa o construtor validado.
    protected Posicao()
    {
    }

    public Posicao(Ativo ativo, int quantidade, decimal precoMedio)
    {
        if (ativo == null)
            throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade da posição deve ser maior que zero.", nameof(quantidade));

        if (precoMedio <= 0)
            throw new ArgumentException("O preço médio deve ser um valor positivo.", nameof(precoMedio));

        Ativo = ativo;
        AtivoId = ativo.Id;
        Quantidade = quantidade;
        PrecoMedio = precoMedio;
    }

    // Chamado pela carteira ao criar a posição; o EF confirma as duas FKs no SaveChanges.
    internal void VincularCarteira(int carteiraId) => CarteiraId = carteiraId;

    public void AtualizarPrecoMedioEQuantidade(int quantidadeAdicional, decimal precoAtual)
    {
        decimal custoTotalAtual = Quantidade * PrecoMedio;
        decimal custoNovasAquisicoes = quantidadeAdicional * precoAtual;

        Quantidade += quantidadeAdicional;
        PrecoMedio = (custoTotalAtual + custoNovasAquisicoes) / Quantidade;
    }

    public void ReduzirQuantidade(int quantidadeRemovida)
    {
        if (quantidadeRemovida > Quantidade)
            throw new InvalidOperationException("Não é possível remover mais unidades do que as disponíveis na posição.");

        Quantidade -= quantidadeRemovida;
    }
}
