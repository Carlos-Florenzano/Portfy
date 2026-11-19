using FluentValidation;

namespace Portfy.Dtos.Orcamento;

/// A despesa precisa ser positiva; o acúmulo no orçamento é regra de domínio.
public sealed class AdicionarDespesaRequestValidator : AbstractValidator<AdicionarDespesaRequest>
{
    public AdicionarDespesaRequestValidator()
    {
        RuleFor(requisicao => requisicao.Valor)
            .GreaterThan(0).WithMessage("O valor da despesa deve ser maior que zero.");
    }
}
