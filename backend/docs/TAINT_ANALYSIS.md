# TASK-005 — Sources, transforms e sinks

## Limites da análise

Análise manual arquitetural acompanhada de testes de barreira. Não é ferramenta de análise estática/dinâmica universal, certificação de ausência de PII ou implementação de mecanismos futuros da TASK-006. Nenhum SQL Server ou serviço OpenAI foi acessado na validação. A consulta de documentação pública da API não equivale a executar inferência.

## Fluxo principal

```text
HTTP POST /api/ocorrencias [source: descrição não confiável]
  → validação do contrato/referências
  → ProtecaoPii [transform: detecção e mascaramento]
  → DTO interno com descrição protegida + eventos técnicos constantes
  → SaveChanges atômico [sink: OCORRENCIA e EVENTO_SEGURANCA]

HTTP GET /api/risco-cidadao [gatilho, sem texto usado em prompt]
  → instante UTC único e intervalo inclusivo de sete dias
  → EVENTO_SEGURANCA [source: classificações persistidas ainda não confiáveis]
  → allowlist exata de tipo/categoria + filtro temporal + GROUP BY
  → ContagensRisco [transform: somente números]
  → CalculadoraRiscoCidadao [transform: pontos e nível autoritativos]
  → ResumoRiscoAi [barreira de tipo, sem campos livres]
  → payload reconstruído [constantes + categorias permitidas + números]
  → HttpClient OpenAI [sink externo, somente se explicitamente habilitado]
  → resposta LLM [nova source não confiável]
  → limite de bytes + envelope completed + schema local + texto simples + ProtecaoPii
  → texto aceito OU fallback determinístico
  → JSON HTTP [sink público: cálculo local, alertas fixos e explicação separados]
```

## Catálogo de barreiras

| Source | Transform/barreira | Sink permitido | Dados excluídos |
|---|---|---|---|
| Descrição POST | Motor PII, validação de limites | Ocorrência protegida | Valores reconhecidos originais |
| Detecções | Apenas categoria e metadados técnicos constantes | EVENTO_SEGURANCA, mesma unidade atômica | Valores, trechos, posições, hashes pessoais |
| Eventos persistidos | Janela UTC, allowlist, agrupamento | Contagens internas | IDs, origem/destino/ação livres, descrições, vínculos individuais |
| Contagens | Pesos fixos, saturação e nível | ResumoRiscoAi/HTTP | Nenhuma regra delegada ao modelo |
| ResumoRiscoAi | Validação de consistência e construção explícita do JSON | Endpoint HTTPS fixo | Entidades, nomes, executor/RM, imagens, condições médicas, trechos pessoais |
| Credencial de configuração segura | Header Authorization por request | Somente endpoint OpenAI aprovado | Não vai em payload, resposta pública ou logs |
| Resposta LLM | Máximo 32 KiB; completed/sem recusa; JSON somente texto; 1–600 caracteres; texto simples e detector PII | Explicação transitória ou descarte | Erros do provedor, conteúdo inválido/PII, pontuação sugerida pela IA |

ProtecaoPii não é a barreira primária do request externo: a barreira é a ausência de campos de texto livre no contrato e na consulta. Não se enviam relatos nem mesmo após mascaramento. Categoria SAUDE e sua contagem são classificações técnicas agregadas; nenhum termo médico original é enviado.

## Vocabulário e semântica

Somente PII_DETECTADO e VAZAMENTO_BLOQUEADO com CPF/TELEFONE/EMAIL/SENHA/SAUDE. Desconhecidos, inclusive categoria desconhecida em bloqueios, são ignorados. Bloqueio soma 15 uma única vez, sem incrementar deteccoes. A TASK-005 não produz bloqueios nem converte erros da IA em eventos. No SQL Server, collation binária e tamanho físico protegem contra equivalência de caixa, acento e padding; sem mudar schema. O provider SQLite usa suas comparações e tamanhos equivalentes para o vocabulário ASCII aprovado.

## HTTP externo e falhas

Integração desabilitada por padrão; configuração segura não é lida/impressa pelos testes. Os testes usam apenas sentinela explícita de credencial fictícia. Endpoint não é recebido do usuário e redirects estão desabilitados, evitando encaminhamento de Authorization a outro host por redirect. HttpClient é gerenciado por DI; timeout de cinco segundos inclui corpo; não há retry, cookie, tools, conversation ID ou previous_response_id. Payload solicita store=false, o que não equivale a garantia de retenção zero em infraestrutura do provedor.

Configuração ausente/inválida, HTTP/rede, timeout, recusa, JSON inválido, corpo/texto excessivo e saída rejeitada geram ausência de texto do adapter e fallback da aplicação. Cancelamento do chamador é propagado. Falha do banco não é convertida em indicador vazio ou erro de IA: permanece falha pública genérica. GET não chama SaveChanges e não grava MENSAGEM_RISCO/eventos. Não há tentativa de associar agregação a um evento individual.

## Logs e erros

Adapter não possui logger nem inclui dados recebidos em mensagens públicas. Loggers da factory HTTP são removidos e categoria System.Net.Http.HttpClient é filtrada. Proteções da TASK-004 para ASP.NET Core/EF continuam. Não ativar body/header logging, sensitive-data logging, captura de credenciais ou telemetria externa adicional sem revisão. Essa escolha reduz observabilidade; diagnósticos futuros devem usar classificações técnicas seguras.

## Evidências automatizadas

- RiscoAiTests verifica método/endpoint, schema solicitado, store=false, chaves exatas do input agregado, modelo configurável, falhas, recusas, PII nas cinco categorias, limite sem Content-Length, timeout real local de cinco segundos e cancelamento. HttpMessageHandler simulado, zero inferências reais.
- RiscoApiTests insere somente dados fictícios no SQLite temporário, inclusive relato legado com sentinelas e campos livres de auditoria. O handler exige ausência dessas sentinelas, CPF/e-mail/senha/saúde fictícios e identificadores no request. O fluxo real usa store, serviço, adapter e controller.
- Interceptor de comandos exige somente SELECT no GET; conferências posteriores preservam ocorrências/eventos e zero mensagens. Contagens/alertas fixos são comparados ao Mobile atual, sem modificar/executar ferramentas do Mobile. Isso não é teste ponta a ponta do aplicativo.
- RiscoSqlServerProjectionTests captura a expressão do store real e gera SQL offline com COUNT_BIG, GROUP BY, janela, collation e DATALENGTH; interceptor bloqueia conexões e exige zero tentativas. Prova tradução, não execução SQL Server.

## Riscos residuais e limites

- Detector PII é heurístico: não reconhece toda informação pessoal/ofuscada, nome ou condição médica fora da lista. O modelo pode inventar conteúdo ou afirmações incoerentes; o filtro não é análise semântica completa. Pontos/nível/alertas autoritativos permanecem fora da IA.
- O indicador é global, sem autenticação, e pequenos volumes/horários podem permitir inferências contextuais. Não acrescentar filtros individuais ou dados de eventos na resposta sem revisão.
- Não há índice temporal no schema, rate limit ou cache de inferência; volume e custo de GET com IA habilitada precisam de acompanhamento e eventual tarefa própria.
- A política de retenção/termos do provedor deve ser revisada antes da habilitação real; nenhum dado real deve ser usado como demonstração.
- A mensagem agregada não é persistida; MENSAGEM_RISCO permanece inalterada (FK obrigatória/única por evento), sem violar sua cardinalidade.
- A tela Mobile passou a renderizar explicação e origem LLM/FALLBACK, validando contrato e cache. Reconstrói apenas campos permitidos e rejeita PII reconhecida ou saída inadequada. Testes isolados não comprovam execução em Expo/dispositivo ou inferência externa; ver [fluxo e validação atual](CYBERSECURITY_FLUXO_E_VALIDACAO.md).

## Barreiras reais e simulação Mobile

No backend, o source é `CriarOcorrenciaRequest.Descricao`. `ProtecaoPii.Analisar` transforma o texto antes da persistência e fornece categorias para auditoria. Leituras protegem descrições legadas sem regravar dados. O sink externo recebe exclusivamente `ResumoRiscoAi`, reconstruído de agregados permitidos: nem relato original nem mascarado entram no payload. A saída LLM é um novo source não confiável, validado ou substituído por fallback.

No Mobile, `fetchRisk` consulta somente GET /api/risco-cidadao. `parseRisk` exige janela/explicação para a API, verifica pontos/alertas e projeta apenas campos conhecidos. `AppContext` armazena essa projeção e valida o risco restaurado do cache; a tela repete a validação antes de renderizar. Erros públicos são constantes. Isso não constitui detecção universal de PII: a principal barreira externa continua sendo o contrato fechado de agregados.

Os logs `[TAINT]` e eventos `VAZAMENTO_BLOQUEADO` de `mobile/src/services/mock-engine.ts` pertencem ao simulador. Headers `X-Rastrear-Fluxo`, `X-Simular-Log-Inseguro` e `X-Simular-Falha` não implementam esse comportamento no backend. O backend produz PII_DETECTADO/MASCARADO, mas não um evento de tentativa bloqueada em sink inseguro. Não atribuir eventos simulados ao SQL Server. Demonstrar um sink real bloqueado e sua auditoria exige cenário aprovado; não se adicionou endpoint de falha ou log inseguro nesta etapa.

## Referência do protocolo

Adapter revisado contra a [referência oficial de criação de Responses](https://developers.openai.com/api/reference/resources/responses/methods/create) em 17/11/2026: endpoint, input/instructions, text.format com json_schema, output de mensagens/output_text e store. Implementação HTTP e testes simulados não comprovam aceitação por conta/modelo remoto; chamada real continua pendente de autorização específica.
