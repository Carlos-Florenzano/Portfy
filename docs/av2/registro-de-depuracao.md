# Registro de depuração — `IOException` ao apagar o banco temporário

Defeito investigado, hipóteses, técnica de depuração, causa raiz, correção e
verificação antes/depois. A reprodução foi **reexecutada** em 2026-11-19 para este
registro; não é relato de terceiros.

## 1. O defeito (E01)

| Campo | Conteúdo |
|---|---|
| Sintoma | 5 testes falham com `System.IO.IOException` ao final da execução |
| Mensagem | `The process cannot access the file 'portfy-tests-<guid>.db' because it is being used by another process.` |
| Ponto de lançamento | `tests/Portfy.Tests/Api/ApiFactory.Dispose` → `File.Delete(caminho)` |
| Testes afetados | `Api.AutenticacaoTests` (5 de 5) |
| Introduzido por esta alteração? | **Não** — é anterior; não foi apresentado como melhoria de nenhuma feature |

## 2. Condições de reprodução, esperado e observado (E02)

**Condições.** Windows 10, .NET 10, `dotnet test tests/Portfy.Tests`, sem
`Pooling=False` na connection string do banco de teste (estado anterior à correção).

**Esperado:** os 5 testes de `AutenticacaoTests` passam e o arquivo temporário é
removido no `Dispose`.

**Observado (reproduzido nesta revisão):**

```
Com falha Portfy.Tests.Api.AutenticacaoTests.Login_DevolveTokenComIdentidadeDoUsuario
  System.IO.IOException : The process cannot access the file
  'C:\...\Temp\portfy-tests-6bb348fb84da43419126331aa4fde99e.db'
  because it is being used by another process.
...
Com falha! – Com falha: 5, Aprovado: 0, Total: 5
```

Cada falha aponta um arquivo **diferente** (GUID próprio). Isso já elimina a
hipótese de dois testes disputando o mesmo arquivo.

## 3. Hipóteses e como cada uma foi testada (E03)

| # | Hipótese | Como foi testada | Resultado |
|---|---|---|---|
| H1 | Dois testes compartilham o mesmo banco temporário | os nomes dos arquivos nas 5 mensagens são GUIDs distintos | **descartada** |
| H2 | O pool de conexões do `Microsoft.Data.Sqlite` mantém o handle aberto após o `Dispose` do contexto | executar apenas `--filter "FullyQualifiedName~AutenticacaoTests"`: se o problema é de concorrência entre suítes, isolar reduziria a falha; se é de handle, continua falhando 5/5 | **confirmada** — isolado, continua 5/5 |
| H3 | Antivírus/indexador segurando o arquivo | a falha é determinística e sempre no mesmo ponto (`ApiFactory.Dispose`), não intermitente | **descartada** |
| H4 | `File.Delete` chamado antes do fim do `base.Dispose` | leitura do código: `base.Dispose(disposing)` é a **primeira** linha | **descartada** |

A hipótese H2 é a única compatível com as três observações: arquivos distintos,
falha determinística, ponto único de lançamento.

## 4. Técnica de depuração aplicada (E04)

**Técnica usada: análise de log/stack trace + isolamento por filtro (redução do
espaço de busca).**

1. a mensagem de exceção nomeia o arquivo e a operação (`File.Delete`);
2. o stack trace aponta o frame exato (`ApiFactory.Dispose`);
3. o isolamento por `--filter` separa "concorrência entre testes" de "recurso não
   liberado" — a falha persistir isolada prova que não é concorrência;
4. a leitura do código confirma que `Dispose` da base já rodou quando o `Delete` é
   chamado.

Não foi necessário breakpoint: a exceção já traz arquivo, operação e frame. O
critério para dispensar o breakpoint foi "a evidência disponível já distingue todas
as hipóteses levantadas".

## 5. Causa raiz e correção (E05)

**Causa raiz.** O pool de conexões do `Microsoft.Data.Sqlite` mantém a conexão
física (e portanto o *file handle*) viva após o `Dispose` do `AppDbContext`. No
Windows, `File.Delete` falha enquanto existe handle aberto — diferente de
plataformas POSIX, onde o arquivo pode ser desvinculado com handle aberto.

**Correção (mínima e no *seam* correto).** `Pooling=False` na connection string do
banco **de teste**, em `tests/Portfy.Tests/Api/ApiFactory.cs`. Nenhum código de
produção foi alterado: o pool é desejável em produção e o problema era exclusivo do
ciclo de vida do arquivo temporário de teste.

**Por que a correção é de causa, não de sintoma.** Alternativas descartadas:

| Alternativa | Por que foi rejeitada |
|---|---|
| `try/catch` em volta do `File.Delete` | esconderia a falha e deixaria arquivos temporários acumulando |
| `GC.Collect()` antes do `Delete` | depende de finalização não determinística; não garante liberação |
| ignorar o `Delete` | vaza arquivo por execução |
| `Pooling=False` no banco de produção | penalizaria a aplicação real para resolver problema de teste |

## 6. Verificação antes/depois (E06)

| Momento | Comando | Resultado |
|---|---|---|
| **Antes** (sem `Pooling=False`) | `dotnet test --filter FullyQualifiedName~AutenticacaoTests` | **5 falhas / 5** |
| **Depois** (com `Pooling=False`) | mesmo comando | **5 aprovados / 0 falhas** |
| Suíte completa | `dotnet test tests/Portfy.Tests` | **247 aprovados / 0 falhas** |

**Comparação entre a técnica usada e uma alternativa.**

| Critério | Análise de stack trace + filtro (usada) | Breakpoint / passo a passo (alternativa) |
|---|---|---|
| Tempo até a causa | baixo — a exceção já nomeia arquivo, operação e frame | médio — exige parar no `Dispose` e inspecionar handles |
| Dependência de IDE | nenhuma; funciona em CI e terminal | exige depurador anexado |
| Prova de que não é concorrência | direta (isolamento por filtro) | indireta (exigiria observar dois testes ao mesmo tempo) |
| Risco de mascarar o problema | baixo | baixo |
| Quando seria melhor | — | se a exceção não trouxesse frame nem arquivo |

Conclusão da comparação: para **este** defeito, a análise de stack trace foi
suficiente e mais barata; o breakpoint seria a escolha se a mensagem fosse genérica.

## 7. Prevenção de regressão

- O motivo está comentado no próprio `ApiFactory` (linhas 17-19), para que a
  remoção do `Pooling=False` seja reconhecida como regressão e não como limpeza.
- A suíte completa roda em 6 s; qualquer retorno do `IOException` aparece na
  primeira execução.
- `ApiFactory` apaga `-shm` e `-wal` além do `.db`, então o estado do SQLite em modo
  WAL também é limpo entre execuções.
