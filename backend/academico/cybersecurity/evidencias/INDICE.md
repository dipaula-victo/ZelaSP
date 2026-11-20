# Índice de evidências de Cybersecurity

Os identificadores CY são internos desta disciplina, não a numeração oficial de C#. [Relatório PDF](../docs/RELATORIO_CYBERSECURITY_ZELASP.pdf), com sumário e 14 páginas.

| ID | Título e contexto | Ambiente / resultado histórico | Local / PDF | Limitação |
|---|---|---|---|---|
| CY-PII | Parsing e sanitização | Código real e PiiApiTests selecionados na regressão | [Fonte](../../../src/ZelaSP.Application/ProtecaoPii.cs); PDF pp. 4-5 | Não é detector universal; execução isolada SQLite |
| CY-TAINT | Marcação e bloqueio HTTP | 33 testes novos aprovados | [Registro, código e logs](taint-explicito-20261119/README.md); PDF pp. 6-7 | Propagação limitada ao tipo e sink |
| CY-BAR | Cadeia integrada de barreiras | POST, cinco eventos, agregado e rejeição; teste aprovado | [Registro](taint-barreiras-20261119/README.md) | Handler externo simulado; sem LLM/SQL Server |
| CY-REG | Regressão e build | 175 aprovados, zero falhas/ignorados; build zero avisos/erros | [Regressão](taint-explicito-20261119/regressao-console.txt), [build](taint-explicito-20261119/build-console.txt); PDF p. 11 | Seleção, não suíte integral; não somar os 33 de novo |
| CY-API | Dashboard com API real | HTTP 200; 0/BAIXO, contagens zeradas, FALLBACK | [Captura](integracao-real-5188-20261119/dashboard.png), [JSON](integracao-real-5188-20261119/response.json), [registro](integracao-real-5188-20261119/README.md); PDF pp. 8 e 10 | Banco configurado ZelaSP_Dev; demais endpoints bloqueados |
| CY-WEB | Falhas, cache e renderização | 17 verificações de browser; IndexedDB real; 11 guards | [Registro](../../../docs/revisoes/CYBERSECURITY_VALIDACAO_EXPO_WEB.md); PDF pp. 11-12 | Parte usa resposta interceptada; documento antecede correção do runner |
| CY-MOB | Runner padrão corrigido | npm test 114; isolado 103; typecheck aprovado | [Correção de inicialização](../../../../mobile/tests/risk-api.test.ts); PDF p. 11 | Resultados aprovados no histórico da sessão e confirmados pelo PM; sem log bruto separado consolidado |
| CY-LLM | Adapter de tradução | Implementação e testes simulados | [Código](../../../src/ZelaSP.Infrastructure/OpenAiExplicacaoCidadaClient.cs), [plano](../PLANO_VALIDACAO_LLM.md); PDF p. 9 | Inferência real pendente |
| CY-VIDEO | Vídeo do dashboard | Sem vídeo final localizado | PENDENTE; PDF p. 13 | Não há link de vídeo a entregar ainda |

As capturas e JSON permanentes são cópias fiéis dos originais em .expo. Integridade registrada nas respectivas pastas. A elaboração do PDF não executou novamente testes, API, SQL Server ou LLM. Capturas do código no PDF são transcrições identificadas, não screenshots inventadas.
