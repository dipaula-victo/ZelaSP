# Code Review — Cybersecurity / ZelaSP

Data: 19/11/2026. Branch: feature/task-008-academic-deliverables.
Revisão estática. Sem testes, Expo, HTTP, SQL ou chamadas externas nesta etapa.

## Resumo executivo
Nenhum P0 identificado. Recomenda-se corrigir os P1 antes de declarar integração concluída. Contrato de risco compatível estaticamente; payload externo agregado e fechado; cálculo independente da IA; saída validada. Nada disso comprova execução no Expo ou inferência real.

## Achados

### R1 — P1: resposta antiga pode substituir a mais recente
Local: mobile/src/state/AppContext.tsx:223–226 e 42–49; mobile/app/(tabs)/risco.tsx:29–45.
Evidência: cada refresh persiste sua resposta incondicionalmente. A fila ordena gravações pela conclusão, não pelo início da requisição. Não há geração, deduplicação ou cleanup do foco.
Cenário: A inicia antes, B termina primeiro e salva; A termina depois e sobrescreve B. riskUpdatedAt recebe o horário atual, aparentando atualização recente. Cada finally pode também ocultar loading com outra requisição pendente.
Impacto: indicador defasado e chamadas redundantes, inclusive LLM quando habilitado.
Correção: latest-request-wins, deduplicação e descarte após blur/unmount; loading coerente. Para cancelamento, revisar services/http.ts:11–14, que substitui o sinal recebido.
Testes: promises A/B concluídas em ordem inversa; offline/foco/desmontagem; loading concorrente. Cenário deduzido estaticamente, não reproduzido nesta revisão.

### R2 — P1: cache rejeitado pode continuar no armazenamento
Local: mobile/src/state/AppContext.tsx:97–112; services/contract.ts:197–200; db/storage.ts:14–26; db/storage.web.ts:14–32.
Evidência: safeCache só atualiza saved.current/setSnapshot. Persistência futura pode saneá-lo; offline ou falha pode deixar o original no disco.
Impacto: proteção da UI não prova remoção de eventual PII de cache legado/adulterado. O cliente novo rejeita respostas inválidas antes de persistir; não foi identificado armazenamento dessas respostas novas.
Correção: regravar snapshot saneado preservando demais dados e tratando falha, sem limpeza ampla.
Testes: armazenamento adulterado, abertura offline, verificação física, reabertura e erro de gravação. Teste puro parseCachedRisk não cobre isso.

### R3 — P1: senha com espaços deixa fragmentos no simulador — preexistente
Local: mobile/src/services/privacy.ts:54–56; services/mock-engine.ts:58–107; services/contract.ts:31.
Evidência: regex consome apenas primeiro segmento sem espaços. Por inspeção, senha: PrimeiraFicticia SegundaFicticia deixa o segundo segmento; mock persiste masked.text. Descrição já rotulada com sufixo pode passar pelo guard de descrição.
Impacto: fragmento potencialmente sensível em relato/cache do mock; falsa equivalência com backend. Dependência preexistente, não regressão do patch.
Limite: não demonstra bypass da explicação de risco, que rejeita texto detectado e colchetes.
Correção: alinhar em tarefa aprovada a política conservadora do simulador ao backend; não apresentar mock como prova integral.
Testes: espaços, aspas, rótulo com sufixo, múltiplos marcadores, delimitadores, idempotência e persistência.

### R4 — P1: bloqueio em sink inseguro só demonstrado no simulador
Local: mobile/src/services/mock-engine.ts:92–105; services/contract.ts:201–213; backend/docs/TAINT_ANALYSIS.md:75–85; OcorrenciasService.CriarAsync.
Evidência: unsafeLog acrescenta VAZAMENTO_BLOQUEADO no mock. Headers de simulação não implementam bloqueio equivalente no backend. Documentação reconhece isso.
Impacto: lacuna de comprovação acadêmica, não prova de exfiltração. Contar eventos não prova produtor real.
Correção: aprovar cenário isolado de sink bloqueado e auditoria segura; não criar endpoint inseguro nem inventar eventos no banco. Identificar simulação no vídeo.
Testes: spy do sink comprovando ausência do valor marcado e metadados seguros de bloqueio.

### R5 — P1: 79 testes não validam React/estado/Expo
Local: mobile/tests/run-cyber-isolated.mjs:1–18; render-guards.test.ts:70–80,163–195; risk-api.test.ts:112–124,146–175.
Evidência: runner importa só core e risk-api. Guards React substituem AppContext e desativam useFocusEffect; os três novos guards não participaram dos 79. Caso abort injeta DOMException imediatamente, não mede timeout nem cancelamento do consumidor. Type stripping não verifica tipos.
Impacto: loading, recuperação, concorrência, cache físico e Expo ainda sem comprovação; testes puros continuam úteis.
Correção/testes: typecheck e guards com dependências originais, testes de estado/armazenamento e smoke test Expo autorizado.

### R6 — P2: corpo HTTP Mobile sem limite efetivo de bytes
Local: mobile/src/services/http.ts:32–33; services/contract.ts:174–182.
Evidência: response.text/JSON.parse precedem limite de 600 caracteres e descarte de campos extras.
Impacto: API mal configurada pode consumir memória. Comportamento compartilhado preexistente; adapter OpenAI já limita sua própria resposta a 32 KiB.
Correção/testes: limite efetivo compatível com runtime, inclusive sem Content-Length; corpo excessivo e JSON profundo; erro genérico.

## Contrato backend/Mobile
Fontes: backend/src/ZelaSP.Application/ContratosRisco.cs:5–23; RiscoCidadaoService.cs:22–43; mobile/src/services/contract.ts:110–200; risk-client.ts:7–13; mobile/src/data/types.ts.

| Campo | Conferência |
|---|---|
| pontuacao/nivel | 0–100; BAIXO 0–19, MEDIO 20–49, ALTO 50–100; parser recalcula |
| periodoDias | Exatamente 7 |
| deteccoes | Cinco categorias não negativas; inteiros seguros JS são subconjunto conservador de long |
| vazamentosBloqueados | Peso 15, sem somar categoria novamente |
| alertas | Tipos/textos fixos, sem duplicatas, correspondentes às contagens |
| janela | UTC, sete dias; obrigatória no fetch real, opcional em legado |
| explicacao | Até 600 caracteres, LLM/FALLBACK; obrigatória no fetch real, opcional em legado |

GET real correto. Projeção elimina campos extras. Datas de sete casas aceitas com resolução JS de milissegundos. Cache não tem TTL nem vínculo com URL da API (AppContext.tsx:26): limitação de atualidade/proveniência. Compatibilidade estática não é resposta HTTP efetivamente observada.

## Segurança e Taint Analysis
Sources: descrição HTTP, eventos persistidos, resposta LLM e cache não confiável.
Transforms: ProtecaoPii → descrição mascarada/eventos técnicos → persistência atômica → agregação por janela/vocabulário → calculadora determinística → DTO agregado fechado → validação de saída → parseRisk/projeção.
Sinks: banco, resposta HTTP, cliente OpenAI, armazenamento/UI Mobile.

OpenAiExplicacaoCidadaClient.cs:38–62 reconstrói constantes, enumerações e números. Não recebe descrições, entidades, IDs individuais, imagens, executores ou auditoria livre. Linhas 24–25 e 64–110 limitam timeout, tamanho e formato. RiscoCidadaoService.cs:9–18,29–43 revalida e gera fallback; score não depende da IA. risk-client retorna erro genérico.
Detecção é heurística, não cobre toda PII concebível. Texto aceito pode ser semanticamente enganoso: validação não garante verdade narrativa. UI não executa HTML. Mock mostra aviso e fallback de demonstração; MOCK não é origem aceita do backend. Fallback não comprova inferência.

## Testes e evidências
Registro histórico consultado: backend/docs/CYBERSECURITY_FLUXO_E_VALIDACAO.md:43–61: 79 aprovados, zero falhas/ignorados/cancelados, exit 0, 51,5316 ms; 26 core e 53 novos. Não reexecutado nesta revisão.
Cobertura dos novos: parser, contagens/pesos/teto, alertas, PII/NFC, projeção, janela UTC, origem, legado, mock e GET com fetch substituído; falhas HTTP/rede/JSON/abort simuladas.
Ausentes ou insuficientes: races, AppContext real, saneamento físico do cache, cancelamento/timeout observado, renderização executada, typecheck, Expo e inferência real. Guards React fora dos 79. Bloqueio de fetch no runner não é sandbox geral de rede.
Testes backend tentados anteriormente não rodaram: build bloqueado por DLLs em uso, MSB3021/MSB3027.

## Regressões e prontidão
Alterações gateway concentram getRisk. Serviços de ocorrências/imagens mantêm incompatibilidades preexistentes; sucesso estático do risco não comprova integração das demais telas.
Expo Web exige CORS/proxy aprovado; API não habilita CORS atualmente. Não desabilitar segurança. Cliente nativo precisa de URL alcançável: localhost do aparelho não é o computador.
Parecer: solicitar ajustes P1 e validação visual antes de declarar fluxo final. Chamada real LLM depende de autorização e configuração segura próprias. Vídeo deve separar API real, mock, LLM e fallback sem credenciais/PII.

## Patch e preservação
Patch contra HEAD local: 13 arquivos Cybersecurity, nove modificados e quatro novos. Exclui backend/VALIDACAO.md, backend/prompts.md, ApiExceptionHandler.cs, entregas C#/SOA e E05ProcessHost. Inclui guards React ainda não executados. Não aplicar cegamente sobre working tree que já contém alterações.
git diff --check sem erros; staging vazio; git apply --numstat analisou os 13 arquivos sem aplicar. Hashes dos 13 fontes iguais antes/depois. Criados apenas relatório e patch. Sem correções, instalação, testes/build, HTTP, SQL ou Git de escrita.
