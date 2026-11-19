using System.Collections.Concurrent;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Estado compartilhado de saúde das fontes externas (Singleton). Quando uma
/// fonte falha — 429, 5xx, timeout ou credencial recusada — ela sai da rotação
/// por um período, e as requisições seguintes vão direto para o fallback em vez
/// de pagar o mesmo timeout. Uma resposta bem-sucedida a devolve à rotação.
public sealed class DisjuntorDeProvedores
{
    private readonly ConcurrentDictionary<OrigemDeMercado, DateTimeOffset> _bloqueadoAte = new();
    private readonly IRelogio _relogio;

    public DisjuntorDeProvedores(IRelogio relogio)
    {
        _relogio = relogio ?? throw new ArgumentNullException(nameof(relogio));
    }

    public bool EstaDisponivel(OrigemDeMercado origem)
    {
        if (!_bloqueadoAte.TryGetValue(origem, out DateTimeOffset bloqueadoAte))
            return true;

        if (bloqueadoAte <= _relogio.UtcNow)
        {
            _bloqueadoAte.TryRemove(origem, out _);
            return true;
        }

        return false;
    }

    public void Abrir(OrigemDeMercado origem, TimeSpan duracao) =>
        _bloqueadoAte[origem] = _relogio.UtcNow.Add(duracao);

    public void Fechar(OrigemDeMercado origem) => _bloqueadoAte.TryRemove(origem, out _);
}
