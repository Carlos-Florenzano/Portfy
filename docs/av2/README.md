# Artefatos de avaliação — AV2

Índice dos documentos exigidos pela rubrica da AV2. Todos descrevem o código que
está no repositório; nenhum declara concluído o que não foi comprovado.

| Artefato | Arquivo | Item(ns) da rubrica |
|---|---|---|
| Especificação de requisitos (3 fluxos + 5 regras + critérios de aceitação) | `requisitos.md` | A03, G01 |
| Modelo de classes (classes, atributos, métodos, relações) | `modelo-de-classes.md` | B01–B06 |
| Análise do modelo (invariantes e decisões comparadas) | `analise-do-modelo.md` | B07, B08 |
| Revisão de código (alteração, diff, impacto, parecer) | `revisao-de-codigo.md` | C01–C06 |
| Registro de depuração (defeito, hipóteses, técnica, correção) | `registro-de-depuracao.md` | E01–E06 |
| Matriz de rastreabilidade (fluxos, regras, implementação, testes) | `rastreabilidade.md` | F01–F05 |
| Auditoria final (requisitos × evidências × pendências) | `auditoria-final.md` | F06 |
| Índice legível por máquina | `evidencias.json` | todos |

## Como usar

1. Comece por `evidencias.json`: ele lista os 40 códigos (`A01`…`G02`) com
   `situacao` (`atendido` / `parcial` / `nao_atendido`) e os caminhos de evidência.
2. Para entender o contrato funcional, leia `requisitos.md` — é a fonte única dos
   três fluxos e das cinco regras; os outros documentos referenciam esses
   identificadores em vez de redefini-los.
3. Para conferir qualquer afirmação, o caminho de evidência aponta para arquivo e,
   quando útil, para o método ou teste que prova o comportamento.

## Regerar as evidências

```bash
# build e testes (Windows: exporte APPDATA, PROGRAMDATA, PROGRAMFILES etc. se o
# restore falhar com "Value cannot be null (Parameter 'path1')")
dotnet build Portfy.csproj
dotnet test tests/Portfy.Tests
```

Estado medido em 2026-11-19: **build 0 erros / 0 avisos · 247 testes aprovados**.

## Limite deste conjunto

`evidencias.json` é um **índice**, não substitui código nem teste. A auditoria que o
acompanha é o que sustenta a nota; o julgamento dos itens de modelagem é do
avaliador.
