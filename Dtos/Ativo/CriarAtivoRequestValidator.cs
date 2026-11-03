using FluentValidation;
using Portfy.Models;

namespace Portfy.Dtos.Ativo;

/// A forma do pedido. A regra de preço positivo já existe no domínio; aqui ela é
/// antecipada para devolver 400 com mensagem clara antes de tocar no modelo.
public sealed class CriarAtivoRequestValidator : AbstractValidator<CriarAtivoRequest>
{
    public CriarAtivoRequestValidator()
    {
        RuleFor(requisicao => requisicao.Ticker)
            .NotEmpty().WithMessage("O ticker é obrigatório.")
            .MaximumLength(10).WithMessage("O ticker deve ter até 10 caracteres.")
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("O ticker aceita apenas letras e números.");

        RuleFor(requisicao => requisicao.Nome)
            .NotEmpty().WithMessage("O nome do ativo é obrigatório.")
            .MaximumLength(120).WithMessage("O nome do ativo deve ter até 120 caracteres.");

        RuleFor(requisicao => requisicao.Tipo)
            .NotEmpty().WithMessage("O tipo do ativo é obrigatório.")
            .Must(tipo => Enum.TryParse<TipoAtivo>(tipo, ignoreCase: true, out _))
            .WithMessage("O tipo deve ser Acao, Fii, RendaFixa, Cripto ou Etf.");

        RuleFor(requisicao => requisicao.PrecoAtual)
            .GreaterThan(0).WithMessage("O preço atual deve ser maior que zero.");
    }
}
