namespace Portfy.Models;

// Estado nomeado da carteira (arch §9.1). Substitui a ideia de "booleano ativa/inativa":
// os três valores são significativos e o ciclo de vida é protegido no próprio agregado.
//
//   Ativa ──Suspender()──▶ Suspensa ──Reativar()──▶ Ativa
//     │                        │
//     └────Encerrar()──────────┴──▶ Encerrada (estado final, sem saída)
//
// Transições proibidas (ex.: Encerrada ──Reativar()──▶ Ativa) são recusadas no domínio
// antes de qualquer mutação, para não deixar objeto nem registro persistido inconsistente.
public enum StatusCarteira
{
    Ativa = 0,
    Suspensa = 1,
    Encerrada = 2,
}
