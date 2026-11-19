using FluentValidation;

namespace Portfy.Dtos.Carteira;

/// Regras de forma da compra/venda. Saldo insuficiente, posição inexistente e
/// ticker desconhecido são regra de negócio e ficam no CarteiraService.
public sealed class OperacaoCarteiraRequestValidator : AbstractValidator<OperacaoCarteiraRequest>
{
    public OperacaoCarteiraRequestValidator()
    {
        RuleFor(requisicao => requisicao.Ticker)
            .NotEmpty().WithMessage("O ticker do ativo é obrigatório.")
            .MaximumLength(20).WithMessage("O ticker deve ter no máximo 20 caracteres.");

        RuleFor(requisicao => requisicao.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
    }
}
