using FluentValidation;

namespace Portfy.Dtos.Usuario;

/// O endpoint de salário exige valor maior que zero (o cadastro aceita zero).
public sealed class AtualizarSalarioRequestValidator : AbstractValidator<AtualizarSalarioRequest>
{
    public AtualizarSalarioRequestValidator()
    {
        RuleFor(requisicao => requisicao.NovoSalario)
            .GreaterThan(0).WithMessage("O salário mensal deve ser um valor maior que zero.");
    }
}
