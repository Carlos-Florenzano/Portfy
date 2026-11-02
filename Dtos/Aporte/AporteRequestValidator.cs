using FluentValidation;

namespace Portfy.Dtos.Aporte;

/// Regras de forma do aporte; a existência do usuário de origem é regra de
/// negócio e continua no AporteService.
public sealed class AporteRequestValidator : AbstractValidator<AporteRequest>
{
    public AporteRequestValidator()
    {
        RuleFor(requisicao => requisicao.Valor)
            .GreaterThan(0).WithMessage("O valor do aporte deve ser maior que zero.");

        RuleFor(requisicao => requisicao.OrigemSalarioId)
            .GreaterThan(0).WithMessage("O ID da origem do salário deve ser um número positivo.");
    }
}
