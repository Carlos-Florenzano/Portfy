using FluentValidation;

namespace Portfy.Dtos.Orcamento;

/// Mesmas regras de forma da criação; o nome da categoria não é editável.
public sealed class AtualizarOrcamentoRequestValidator : AbstractValidator<AtualizarOrcamentoRequest>
{
    public AtualizarOrcamentoRequestValidator()
    {
        RuleFor(requisicao => requisicao.LimiteDefinido)
            .GreaterThanOrEqualTo(0).WithMessage("O limite definido não pode ser negativo.");

        RuleFor(requisicao => requisicao.PercentualDoSalario)
            .InclusiveBetween(0, 100)
            .WithMessage("O percentual do salário deve estar entre 0 e 100.");
    }
}
