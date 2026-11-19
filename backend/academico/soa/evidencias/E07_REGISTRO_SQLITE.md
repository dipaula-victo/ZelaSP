# E07 — falha após duas escritas via HTTP em SQLite temporário

Execução local observada em 18/11/2026, branch feature/task-008-academic-deliverables. Escopo: comprovação isolada do fluxo transacional real da aplicação. Não é prova SQL Server nem execução da carga de demonstração.

## Mecanismo

Teste: backend/tests/ZelaSP.Tests/AcademicRollbackTests.cs, T08_HttpFailureAfterBothWritesRollsBackStatusAndHistory. A factory existente recebeu um parâmetro opcional IInterceptor, utilizado somente nos testes. Nenhuma alteração no serviço/store de produção, contrato, entidade, migração ou configuração técnica foi necessária.

SaveChangesInterceptor.SavedChangesAsync é chamado depois do INSERT efetivamente executado por SaveChangesAsync. O fluxo de produção já realizou o UPDATE condicional e ainda não chamou CommitAsync. A sonda exige Database.IsSqlite e CurrentTransaction não nula; consulta o banco com AsNoTracking dentro da mesma transação para conferir status e linha de histórico. Não confunde entidade Added/ChangeTracker com escrita executada. Depois lança uma exceção sentinela, exclusiva do teste.

A exceção impede retorno ao CommitAsync; o descarte da transação explícita não confirmada reverte ambas as escritas. O tratamento HTTP existente retorna 500 Problem Details sem divulgar a sentinela. Não há endpoint, header ou parâmetro de falha. A sonda é registrada na DI da factory, não na aplicação de produção.

## Evidência observada

| Momento | Estado consultado | Históricos | Resultado |
|---|---|---:|---|
| Antes, DbContext próprio | Ocorrência fictícia ID 1, ABERTA | 0 | Baseline verificado |
| Após UPDATE e INSERT, na transação ativa | EM_ANALISE | 1, ABERTA → EM_ANALISE | SavedChangesAsync atingido uma vez; SaveChanges retornou 1 linha |
| Resposta da chamada | PATCH /api/ocorrencias/1/status com JSON {"status":"EM_ANALISE"} | — | HTTP 500, application/problem+json, sem sentinela no corpo |
| Depois, novo DbContext | ABERTA | 0 | Uma ocorrência continua existente; UPDATE e INSERT não permaneceram |
| Consulta HTTP posterior | ABERTA | — | GET /api/ocorrencias/1 retornou 200 |

ID 1 foi retornado pelo POST de preparação e não fixado no teste. Existe apenas neste banco SQLite em memória, destruído ao descartar a factory. Referências e relato são fictícios da fixture; nenhum registro SQL Server foi acessado. As consultas da sonda utilizaram a mesma transação para enxergar as escritas ainda não confirmadas. A conferência posterior usa outro DbContext sobre o mesmo banco temporário.

Controle independente: CommitRemainsSuccessfulWhenTestFailureIsNotArmed retornou HTTP 200 e outro DbContext encontrou EM_ANALISE com exatamente um histórico ABERTA → EM_ANALISE. O comportamento normal de COMMIT foi preservado.

## Comandos realmente executados

Antes da execução, a variável ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION foi conferida ausente. Nenhum filtro incluiu SqlServerWriteTests.

```powershell
dotnet test backend/tests/ZelaSP.Tests/ZelaSP.Tests.csproj --no-restore --filter FullyQualifiedName~AcademicRollbackTests --logger "console;verbosity=detailed"
dotnet test backend/tests/ZelaSP.Tests/ZelaSP.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~OccurrenceApiTests|FullyQualifiedName~OccurrenceConcurrencyTests|FullyQualifiedName~AcademicRollbackTests" --logger "console;verbosity=minimal"
```

Primeiro comando: compilação implícita dos projetos concluída, exit code 0, dois testes aprovados. Segundo: exit code 0, 42 aprovados, zero falhas e zero ignorados, incluindo os dois novos. Não somar os resultados como 44 testes distintos. A suíte completa não foi executada; não se declara regressão integral de todos os módulos. Não houve restore ou build separado.

Saída detalhada do primeiro teste registrou: antes ABERTA/0; sonda EM_ANALISE/1 com transação ativa depois do INSERT e antes de CommitAsync; depois ABERTA/0 e GET 200. Este documento transcreve a execução observada; não é captura bruta de terminal nem consulta SQL Server independente.

## Limites e situação acadêmica

A semântica solicitada de duas escritas e falha pré-COMMIT foi comprovada por HTTP/EF em SQLite relacional, sem mock de persistência. SQLite não comprova comportamento físico, locking, configuração ou execução no SQL Server oficial. O teste histórico antigo continua válido para seu escopo anterior e não foi substituído ou reinterpretado como falha pós-INSERT.

Manter T08/E07 como **PARCIAL no pacote acadêmico final**, com a nova parcela local comprovada, até revisão do PM sobre suficiência, contexto de equipe/executor e necessidade de prova equivalente no banco oficial. Nenhuma execução SQL Server está autorizada por este registro. Não houve endpoint público, mudança de produção, provisionamento, limpeza, novas credenciais ou reinício de demonstração.
