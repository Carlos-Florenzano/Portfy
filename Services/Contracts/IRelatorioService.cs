using Portfy.Dtos.Relatorio;

namespace Portfy.Services.Contracts;

/// Contrato injetado do relatório: consolida, por usuário autenticado, os dados que
/// já existem no SQLite (usuário, carteira, aportes e orçamentos). Nenhum valor é
/// recalculado no SPA (arch §6.2).
public interface IRelatorioService
{
    RelatorioResponse Gerar(int usuarioId, DateOnly? inicio, DateOnly? fim);

    /// Mesma consolidação em CSV (separador ';'), pronta para download.
    string GerarCsv(int usuarioId, DateOnly? inicio, DateOnly? fim);
}
