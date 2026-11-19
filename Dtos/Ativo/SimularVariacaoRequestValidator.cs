using FluentValidation;

namespace Portfy.Dtos.Ativo;

/// Limite inferior protege a invariante "preço > 0" do domínio; o superior evita
/// simulação absurda e overflow de faixa monetária.
public sealed class SimularVariacaoRequestValidator : AbstractValidator<SimularVariacaoRequest>
{
    public SimularVariacaoRequestValidator()
    {
        RuleFor(requisicao => requisicao.PercentualCustomizado)
            .GreaterThan(-100).WithMessage("O percentual deve ser maior que -100.")
            .LessThanOrEqualTo(1000).WithMessage("O percentual deve ser no máximo 1000.")
            .When(requisicao => requisicao.PercentualCustomizado.HasValue);
    }
}
