using FluentValidation;

namespace Portfy.Dtos.Orcamento;

/// A forma do pedido: categoria nomeada, limite e percentual não negativos.
/// A unicidade da categoria por usuário é regra de aplicação (409 no controller).
public sealed class CriarOrcamentoRequestValidator : AbstractValidator<CriarOrcamentoRequest>
{
    public CriarOrcamentoRequestValidator()
    {
        RuleFor(requisicao => requisicao.NomeCategoria)
            .NotEmpty().WithMessage("O nome da categoria é obrigatório.")
            .MaximumLength(60).WithMessage("O nome da categoria deve ter até 60 caracteres.");

        RuleFor(requisicao => requisicao.LimiteDefinido)
            .GreaterThanOrEqualTo(0).WithMessage("O limite definido não pode ser negativo.");

        RuleFor(requisicao => requisicao.PercentualDoSalario)
            .InclusiveBetween(0, 100)
            .WithMessage("O percentual do salário deve estar entre 0 e 100.");
    }
}
