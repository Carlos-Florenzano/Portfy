using FluentValidation;

namespace Portfy.Dtos.Aporte;

/// Regras de forma do aporte; a posse da carteira e a existência do usuário são
/// regras de negócio e continuam no AporteService.
public sealed class AporteRequestValidator : AbstractValidator<AporteRequest>
{
    public AporteRequestValidator()
    {
        RuleFor(requisicao => requisicao.Valor)
            .GreaterThan(0).WithMessage("O valor do aporte deve ser maior que zero.");

        RuleFor(requisicao => requisicao.CarteiraId)
            .GreaterThan(0).When(requisicao => requisicao.CarteiraId.HasValue)
            .WithMessage("O ID da carteira deve ser um número positivo.");
    }
}
