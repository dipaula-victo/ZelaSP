# ZelaSP - Cybersecurity | FIAP GS 2026.2

Turma **3ESPH**. Plataforma CitizenTech para registro e acompanhamento de problemas de zeladoria urbana. Este recorte protege dados pessoais, agrega eventos técnicos e apresenta o Risco Cidadão no dashboard.

**Estado:** documentação local preparada. API real e SQL Server foram validados com FALLBACK; integração HTTP OpenAI está implementada/testada com respostas simuladas. **Inferência real e vídeo final permanecem pendentes.** Não apresentar fallback como IA remota.

## Materiais da entrega

- [PDF acadêmico - 14 páginas, 22 seções](docs/RELATORIO_CYBERSECURITY_ZELASP.pdf).
- [Fonte editável HTML do relatório](docs/RELATORIO_CYBERSECURITY_ZELASP.html).
- [Matriz de requisitos e comprovações](MATRIZ_REQUISITOS.md).
- [Índice de evidências](evidencias/INDICE.md).
- [Inventário exato proposto para publicação](INVENTARIO_PUBLICACAO.json).
- [Conferência e pendências](CONFORMIDADE.md).
- [Planejamento de inferência real](PLANO_VALIDACAO_LLM.md).

Repositório de entrega: https://github.com/dipaula-victo/ZelaSP. Branch local: feature/task-008-academic-deliverables. Link existente não comprova que estas alterações estejam publicadas; não houve commit/push nesta preparação.

## Equipe

| Integrante | RM |
|---|---|
| Djalma Moreira de Andrade Filho | 555530 |
| Felipe Paes de Barros Muller Carioba | 558447 |
| Lucas Rodrigues | 556323 |
| Victor Hugo de Paula | 554787 |

Líder acadêmico: Djalma Moreira de Andrade Filho. A configuração técnica histórica de RM no backend é separada da identificação da entrega e não foi alterada neste fechamento.

## Código e testes

| Recurso | Fonte |
|---|---|
| Parsing e mascaramento | [ProtecaoPii](../../src/ZelaSP.Application/ProtecaoPii.cs) |
| Sanitização/auditoria | [OcorrenciasService](../../src/ZelaSP.Application/OcorrenciasService.cs) e [store](../../src/ZelaSP.Infrastructure/OcorrenciasStore.cs) |
| Classificação e sink HTTP | [TextoClassificado](../../src/ZelaSP.Application/TextoClassificado.cs) e [serviço de risco](../../src/ZelaSP.Application/RiscoCidadaoService.cs) |
| Agregado e cálculo | [store de risco](../../src/ZelaSP.Infrastructure/RiscoCidadaoStore.cs), [calculadora](../../src/ZelaSP.Domain/CalculadoraRiscoCidadao.cs) |
| Adapter LLM | [OpenAiExplicacaoCidadaClient](../../src/ZelaSP.Infrastructure/OpenAiExplicacaoCidadaClient.cs) |
| Dashboard | [risco.tsx](../../../mobile/app/%28tabs%29/risco.tsx), [contrato](../../../mobile/src/services/contract.ts), [estado seguro](../../../mobile/src/services/risk-state.ts) |
| Provas automatizadas | [TaintBarrierTests](../../tests/ZelaSP.Tests/TaintBarrierTests.cs), [TextoClassificadoTests](../../tests/ZelaSP.Tests/TextoClassificadoTests.cs), [TaintPublicationTests](../../tests/ZelaSP.Tests/TaintPublicationTests.cs), [RiscoAiTests](../../tests/ZelaSP.Tests/RiscoAiTests.cs) |

Resultados históricos: Mobile npm test 114; runner isolado 103 (subconjunto); guards React 11 (incluídos); Expo Web 17 verificações; CORS 12; novos testes taint 33; regressão PII/Risco 175; build separado sem avisos/erros. **Não somar seleções sobrepostas.** Não foram reexecutados para elaborar a documentação.

## Reprodução com segurança

Requisitos: .NET SDK 10, Node compatível com engines do lockfile (ambiente validado: Node 24.21.0/npm 11.19.0), dependências npm pelo lockfile e SQL Server configurado separadamente. Na raiz do repositório:

```powershell
dotnet restore .\backend\ZelaSP.sln
dotnet build .\backend\ZelaSP.sln --no-restore
Set-Location mobile
npm.cmd ci --ignore-scripts --no-audit --no-fund
npm.cmd run typecheck
npm.cmd test
node tests/run-cyber-isolated.mjs
```

Esses comandos são instruções, não execuções desta preparação; restore/build escrevem artefatos locais. Os testes Mobile usam respostas controladas e SQLite temporário; não são inferência LLM. Scripts de schema/seed e testes de escrita SQL Server não fazem parte deste roteiro.

Para API/SQL Server, conferir destino e conexão segura conforme [README técnico](../../README.md); não presumir que o banco exista em outro computador. Para o recorte real autorizado anteriormente, ver [comandos e isolamento](../../docs/revisoes/CYBERSECURITY_INTEGRACAO_REAL_5188.md). O build atual com taint está em saída separada descrita nos registros; saída histórica anterior não o substitui.

CORS é restrito a Development, origem http://127.0.0.1:8081 e GET /api/risco-cidadao. Configurar Mobile em modo api, URL efetiva da API, TEST_MODE=false. Manter RiscoAi:Enabled=false até autorização de inferência; jamais configurar chave em EXPO_PUBLIC_*, argumentos ou arquivos versionados. A demonstração bloqueou outros endpoints no navegador, portanto não comprova integração de todas as telas.

## Limitações e entrega ao professor

Taint não é universal; cobre operações do tipo e o sink da explicação. PII é heurística. Nenhum evento VAZAMENTO_BLOQUEADO foi fabricado; unsafeLog pertence ao simulador. A explicação agregada não é persistida. Android/iOS e demais contratos Mobile não foram comprovados no recorte Web.

O documento do professor pede PDF, código-fonte (ZIP ou link GitHub) e vídeo do dashboard. Não foi criado ZIP, vídeo ou publicação automática. Falta gravar/revisar/disponibilizar o vídeo, resolver ou declarar a pendência de inferência real e publicar a seleção revisada após autorização. PDF é documento de entrega com resultados e limites atuais, não declaração de atendimento integral.

## Referências da publicação

Branch preparada: `feature/cybersecurity-academic-delivery`, baseada na main atualizada. O PDF aprovado foi preservado byte a byte; seis links de referência nele ainda apontam à branch histórica `feature/task-008-academic-deliverables`, onde os novos documentos podem não estar publicados. Para consultar as versões desta entrega, use os links relativos deste README, do [índice](evidencias/INDICE.md), da [análise arquitetural](../../docs/TAINT_ANALYSIS.md) e da [validação Expo Web](../../docs/revisoes/CYBERSECURITY_VALIDACAO_EXPO_WEB.md). O sumário interno do PDF permanece válido. Nenhuma inferência LLM ou vídeo foi acrescentado nesta preparação.
