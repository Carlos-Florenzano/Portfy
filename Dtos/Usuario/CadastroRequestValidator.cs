using FluentValidation;

namespace Portfy.Dtos.Usuario;

/// Regras de forma do cadastro (arch §14.1): nada de regra de negócio aqui —
/// duplicidade de login continua no UsuarioService.
public sealed class CadastroRequestValidator : AbstractValidator<CadastroRequest>
{
    public CadastroRequestValidator()
    {
        RuleFor(requisicao => requisicao.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(nome))
            .WithMessage("O nome não pode ser vazio.")
            .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(200).WithMessage("O nome deve ter no máximo 200 caracteres.");

        RuleFor(requisicao => requisicao.SalarioMensal)
            .GreaterThanOrEqualTo(0).WithMessage("O salário mensal não pode ser negativo.");

        RuleFor(requisicao => requisicao.Login)
            .Cascade(CascadeMode.Stop)
            .Must(login => !string.IsNullOrWhiteSpace(login))
            .WithMessage("O login não pode ser vazio.")
            .MinimumLength(3).WithMessage("O login deve ter no mínimo 3 caracteres.")
            .MaximumLength(200).WithMessage("O login deve ter no máximo 200 caracteres.");

        // 64 caracteres é o teto seguro: o BCrypt considera no máximo 72 bytes,
        // e um caractere UTF-8 pode ocupar mais de um byte.
        RuleFor(requisicao => requisicao.Senha)
            .Cascade(CascadeMode.Stop)
            .Must(senha => !string.IsNullOrWhiteSpace(senha))
            .WithMessage("A senha não pode ser vazia.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(64).WithMessage("A senha deve ter no máximo 64 caracteres.");
    }
}
