using FluentValidation;

namespace Portfy.Dtos.Usuario;

/// A nova senha segue a política do cadastro; a troca exige a senha atual, mas
/// quem decide se ela confere é o UsuarioService (regra de negócio).
public sealed class AlterarSenhaRequestValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaRequestValidator()
    {
        RuleFor(requisicao => requisicao.SenhaAtual)
            .Must(senha => !string.IsNullOrWhiteSpace(senha))
            .WithMessage("A senha atual não pode ser vazia.");

        RuleFor(requisicao => requisicao.NovaSenha)
            .Cascade(CascadeMode.Stop)
            .Must(senha => !string.IsNullOrWhiteSpace(senha))
            .WithMessage("A nova senha não pode ser vazia.")
            .MinimumLength(8).WithMessage("A nova senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(64).WithMessage("A nova senha deve ter no máximo 64 caracteres.")
            .Must((requisicao, novaSenha) => novaSenha != requisicao.SenhaAtual)
            .WithMessage("A nova senha deve ser diferente da senha atual.");
    }
}
