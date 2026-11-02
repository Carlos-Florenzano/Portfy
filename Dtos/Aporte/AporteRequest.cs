namespace Portfy.Dtos.Aporte;

/// Corpo do POST api/aporte. `CarteiraId` é opcional: quando omitido, o serviço
/// usa a carteira padrão do usuário autenticado.
public sealed class AporteRequest
{
    public decimal Valor { get; set; }
    public int? CarteiraId { get; set; }
}
