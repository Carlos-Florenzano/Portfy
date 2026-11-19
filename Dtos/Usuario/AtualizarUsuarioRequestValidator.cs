using FluentValidation;

namespace Portfy.Dtos.Usuario;

/// Mesmas regras de nome e salário do cadastro: a atualização não pode aceitar
/// um estado que o cadastro recusaria.
public sealed class AtualizarUsuarioRequestValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    public AtualizarUsuarioRequestValidator()
    {
        RuleFor(requisicao => requisicao.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(nome))
            .WithMessage("O nome não pode ser vazio.")
            .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(200).WithMessage("O nome deve ter no máximo 200 caracteres.");

        RuleFor(requisicao => requisicao.SalarioMensal)
            .GreaterThanOrEqualTo(0).WithMessage("O salário mensal não pode ser negativo.");
    }
}
