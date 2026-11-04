using FluentValidation;

namespace Portfy.Dtos.Orcamento;

/// Mesmas regras de forma da criação. O nome é opcional (contrato anterior), mas
/// quando enviado precisa ser um texto não vazio e dentro do limite da coluna.
public sealed class AtualizarOrcamentoRequestValidator : AbstractValidator<AtualizarOrcamentoRequest>
{
    public AtualizarOrcamentoRequestValidator()
    {
        RuleFor(requisicao => requisicao.NomeCategoria)
            .NotEmpty().WithMessage("O nome da categoria não pode ser vazio.")
            .MaximumLength(60).WithMessage("O nome da categoria deve ter até 60 caracteres.")
            .When(requisicao => requisicao.NomeCategoria is not null);

        RuleFor(requisicao => requisicao.LimiteDefinido)
            .GreaterThanOrEqualTo(0).WithMessage("O limite definido não pode ser negativo.");

        RuleFor(requisicao => requisicao.PercentualDoSalario)
            .InclusiveBetween(0, 100)
            .WithMessage("O percentual do salário deve estar entre 0 e 100.");
    }
}
