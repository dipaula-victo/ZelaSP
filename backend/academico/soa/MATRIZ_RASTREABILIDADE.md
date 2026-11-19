# Rastreabilidade — SOA e Web Services

Fonte normativa: slides oficiais GS CitizenTech, página27. Total publicado:10,0 pontos; não há nota atribuída nesta matriz. Situação comprobatória baseada em código e evidências históricas, sem novas execuções.

| Critério | Pontuação | Arquivo/método | Evidência | Situação |
|---|---:|---|---|---|
| Estruturação do domínio | 3,0 | [Domain](../../src/ZelaSP.Domain); [DbContext](../../src/ZelaSP.Infrastructure/ZelaSpDbContext.cs); serviços Application e DI | [PDF de referência](evidencias/RELATORIO_CSHARP_REFERENCIA.pdf), seções3 e5; histórico da TASK-001 em VALIDACAO | IMPLEMENTADO E COMPROVADO historicamente: quatro camadas, nove entidades e sete FKs |
| Conexão com banco | 3,0 | [AddInfrastructure](../../src/ZelaSP.Infrastructure/DependencyInjection.cs); `OcorrenciasStore.CriarAsync/AlterarStatusComHistoricoAsync`; `ImagensStore` | PDF seções5–7: estrutura física, upload/recuperação e COMMIT SQL Server; [VALIDACAO](../../VALIDACAO.md), TASK-002/003 | IMPLEMENTADO E COMPROVADO no SQL Server; não é inspeção atual do banco |
| Migrações | 1,5 | [InitialCreate](../../src/ZelaSP.Infrastructure/Migrations/20261114191732_InitialCreate.cs), designer/snapshot; [DDL](../../Migrations/InitialCreate.sql) | VALIDACAO, aplicação de14/11/2026; PDF seção5 com estrutura física | IMPLEMENTADO E COMPROVADO historicamente; nenhuma migração aplicada nesta preparação |
| Documentação automática | 1,5 | [Program](../../src/ZelaSP.Api/Program.cs): `AddOpenApi/MapOpenApi`; transformers e atributos dos controllers | PDF seção3, página8: contratos extraídos do OpenAPI servido pela API; [catálogo atual](ENDPOINTS.md) | IMPLEMENTADO E COMPROVADO historicamente; JSON somente em Development, sem Swagger UI |
| Testes automatizados | 1,0 | [ZelaSP.Tests](../../tests/ZelaSP.Tests); `OccurrenceApiTests`, `OccurrenceConcurrencyTests`, `AcademicRollbackTests`, testes de modelo/imagens/PII/risco/IoT | PDF seções4 e7–9; VALIDACAO; [prova de rollback](evidencias/E07_REGISTRO_SQLITE.md) | IMPLEMENTADO E COMPROVADO historicamente; seleção42 ≠ suíte integral; SQL protegido não deve ser executado automaticamente |

## Itens documentais de entrega

| Item | Onde conferir | Estado |
|---|---|---|
| Descrição, equipe, arquitetura e comandos | [README de SOA](README.md), acessível pelo README da raiz | Preparado localmente |
| Endpoints e OpenAPI | [ENDPOINTS](ENDPOINTS.md), contrato e fonte Api | Conferidos estaticamente:13 operações |
| Evidências e origem | [EVIDENCIAS](EVIDENCIAS.md) | Históricos catalogados, sem resultados inventados |
| Código completo no GitHub e acesso | [PUBLICACAO_GITHUB](PUBLICACAO_GITHUB.md) | Publicação da referência final depende do usuário; não confirmada por remote local |

A equivalência da transação de status/histórico foi aprovada no projeto; não se exige reimplementar o exemplo de domínio de C#. O ROLLBACK após duas escritas foi demonstrado em SQLite/TestServer, e o COMMIT manual no SQL Server. Essa distinção deve acompanhar o reaproveitamento das provas. Não aplicar automaticamente C1–C8, T01–T10 ou E01–E09 de C# como critérios oficiais de SOA.
