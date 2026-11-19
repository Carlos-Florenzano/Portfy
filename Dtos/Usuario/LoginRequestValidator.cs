using FluentValidation;

namespace Portfy.Dtos.Usuario;

/// Login só exige presença: recusar aqui por tamanho ou formato revelaria a
/// política de cadastro a quem tenta autenticar.
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(requisicao => requisicao.Login)
            .Must(login => !string.IsNullOrWhiteSpace(login))
            .WithMessage("O login não pode ser vazio.");

        RuleFor(requisicao => requisicao.Senha)
            .Must(senha => !string.IsNullOrWhiteSpace(senha))
            .WithMessage("A senha não pode ser vazia.");
    }
}
