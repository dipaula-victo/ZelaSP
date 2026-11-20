# Matriz de requisitos - Cybersecurity

Fonte específica: Cybersecurity - Explicação Global Solutions.pdf, Prof. Vitor Lasse, fornecido pela equipe. P. 1: parsing/PII; p. 2: barreira de saída/IA; p. 3: PDF, código e vídeo. Não aplicar C1-C8, T01-T10 ou E01-E09 de C# a esta entrega.

| Requisito | Implementação | Comprovação disponível | Situação / limite |
|---|---|---|---|
| Identificar PII | ProtecaoPii.Analisar | PiiApiTests e seleção 175 | ATENDIDO no escopo de cinco categorias; não toda PII concebível |
| Mascarar antes de persistir | OcorrenciasService.CriarAsync | HTTP/SQLite sanitizados e auditoria técnica | COMPROVADO em testes isolados; não nova gravação SQL Server |
| Marcar variável sensível | TextoClassificado.Classificar | 25 testes do tipo | COMPROVADO no tipo explícito |
| Preservar classificação | Concatenar, reclassificação da junção | Casos das duas ordens e fragmentos | COMPROVADO nas operações suportadas |
| Bloquear saída insegura | TentarPublicar e DTO somente com textoLiberado | Oito testes de publicação e TaintBarrierTests | COMPROVADO no sink HTTP da explicação; não guard universal de logs |
| Auditoria técnica | PII_DETECTADO/MASCARADO | Cinco eventos sem valores originais no teste integrado | COMPROVADO; não há evento backend de bloqueio de log produzido por esse fluxo |
| Request externo seguro | ResumoRiscoAi e construção fechada | Handler simulado verifica chaves e ausência de sentinelas | COMPROVADO com transporte simulado |
| Integração API LLM | OpenAI Responses API via HttpClient | Adapter e RiscoAiTests | IMPLEMENTADO; inferência real PENDENTE |
| Tradução cidadã por IA | Texto validado ou fallback | Texto aceito simulado e validação de rejeição | PARCIAL: fallback real não substitui inferência |
| Indicador/dashboard | Calculadora e tela Risco | GET real 200, JSON e screenshot | COMPROVADO, 0/BAIXO e FALLBACK no recorte |
| PDF documental | Relatório 14 páginas/22 seções | Revisão visual de todas as páginas | PREPARADO localmente; não equivale a envio |
| Código-fonte | Backend/Mobile no repositório único | Inventário proposto | PUBLICAÇÃO PENDENTE de autorização/revisão |
| Vídeo dashboard | Não localizado vídeo final | Roteiro em CONFORMIDADE | PENDENTE |

Evidências: [índice](evidencias/INDICE.md). Resultados são históricos, sem nova execução de teste/HTTP nesta montagem. Não há nota acadêmica atribuída.
