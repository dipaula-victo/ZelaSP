# Evidências SOA — reaproveitamento sem novas demonstrações

Não foi executada aplicação, banco, build ou teste neste fechamento. Registros históricos não são nova comprovação do ambiente atual. A numeração abaixo é local de SOA, não substitui os identificadores oficiais de C#.

## PDF de referência

[RELATORIO_CSHARP_REFERENCIA.pdf](evidencias/RELATORIO_CSHARP_REFERENCIA.pdf) é uma cópia integral do relatório final de C#, usada apenas como referência de provas compartilhadas. Original: `backend/academico/csharp/entrega/GS_CSHARP_DjalmaMoreiraDeAndradeFilho_RM555530/docs/relatorio.pdf`. A cópia SOA evita obrigar a publicação de toda a pasta de montagem de C# ou duplicar as capturas em PNG.

- 36 páginas; 13 partes acadêmicas.
- Tamanho:2378740 bytes.
- SHA-256:`BB93F1129BBF3F926EA0B01FF28283761241C6CB98068B2CC5EC440878BEC146`.
- Conteúdo, capturas e resultados foram preservados. A ausência do ícone ODS11 foi aceita pela equipe para C#; não inventar uma nova exigência equivalente para SOA.
- Origem informada da fotografia: Folha de S.Paulo. Autoria, URL e autorização de reprodução não comprovadas. O reaproveitamento não concede licença.

## Índice

| Prova | Arquivo/seção | O que efetivamente comprova | Limite |
|---|---|---|---|
| Arquitetura e fontes | PDF seção3, páginas5–8; quatro projetos em backend/src | Separação de camadas e estrutura real | Capturas são históricas |
| Contrato automático | PDF página8; Program.cs e transformers | OpenAPI servido pela API; publicação de contratos | Não prova execução de todas as rotas |
| Ambiente/build/testes | PDF seção4, páginas9–10; seção9, páginas27–31 | Build documentado e seleção histórica42 aprovações, zero falhas/ignorados | Seleção não equivale à suíte integral; não reexecutada aqui |
| Modelo e banco | PDF seção5, páginas11–14; migração e snapshot | Nove entidades, sete FKs, modelador e estrutura física SQL Server | Nenhuma consulta atual ao banco |
| Migração aplicada | backend/VALIDACAO.md, registro de14/11/2026; InitialCreate/DDL | Aplicação histórica em slayr\SQLEXPRESS/ZelaSP_Dev; histórico EF10.0.6 | Não reaplicar nem presumir estado físico atual |
| Persistência SQL Server | PDF seção6, páginas17–19 | Ocorrência3/imagem3; POST201; PNG612216 bytes; GET200; mesmo SHA-256 | Origem/permissão da fotografia limitada; não comprova reinício SQL Server |
| COMMIT SQL Server | PDF página21 | Ocorrência3 ABERTA → EM_ANALISE, HTTP200, histórico2, executor zelasp-api | Demonstração manual histórica, sem repetição |
| ROLLBACK após duas escritas | PDF páginas22–23; AcademicRollbackTests; registro E07 acadêmico | UPDATE/INSERT atingidos, falha antes do COMMIT, HTTP500 e ABERTA/0 após rollback | SQLite/TestServer, não SQL Server |
| Validações e falhas | PDF páginas24–27; testes e VALIDACAO | Regras de upload/transição e Problem Details | Código de teste e registro de execução são provas diferentes |
| Indisponibilidade/retomada | PDF páginas30–31 | GET200 → API encerrada → conexão recusada → reinício → GET200 | Não é recuperação de imagem após reinício |

## Históricos complementares

- [VALIDACAO do backend](../../VALIDACAO.md): TASK-002/003, provas reais SQL Server; TASK-004/005/006, testes e limites. Última suíte completa registrada na TASK-006:317 aprovados/um SQL ignorado; não é resultado de hoje.
- [Registro E07](evidencias/E07_REGISTRO_SQLITE.md): cópia integral de `backend/academico/csharp/evidencias/E07_ROLLBACK_SQLITE.md`, transcrição da execução de18/11/2026, sem simular saída bruta.
- [AcademicRollbackTests](../../tests/ZelaSP.Tests/AcademicRollbackTests.cs): teste e controle; depende do ajuste local da factory.
- [Procedimento SQL Server](../../docs/SQLSERVER_INTEGRATION.md) e [imagens](../../docs/IMAGENS_SQLSERVER_INTEGRATION.md): procedimentos e históricos, não autorização para novas escritas.

Os links só serão acessíveis no GitHub se esses arquivos forem efetivamente publicados. Ver PUBLICACAO_GITHUB.md. Não publicar sinais com proofKey, autorização efêmera, bancos temporários ou todos os diretórios de execução para substituir este índice.
