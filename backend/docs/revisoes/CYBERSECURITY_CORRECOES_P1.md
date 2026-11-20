# Correções P1 — Cybersecurity

19/11/2026 — branch feature/task-008-academic-deliverables.

## Implementação

- R1: `mobile/src/services/risk-state.ts` usa geração de requisição, comparação de escopo e fila de commits. Respostas obsoletas e erros obsoletos são descartados. A fila revalida a geração antes de iniciar o commit. O provider usa esse coordenador e invalida em mudança de configurações, reset e desmontagem. A tela invalida no blur e protege loading/erro por geração local.
- API/MOCK: o modo atual vem da configuração de inicialização, não de um seletor em runtime. Chaves de cache permanecem separadas por modo. O coordenador compara escopo contendo modo e flags; testes trocam o escopo em memória nas duas direções. Uma troca real de configuração exige reinicialização do aplicativo; ainda não foi validada no Expo.
- R2: restauração revalida/projeta risco e regrava o snapshot saneado antes de publicá-lo, preservando demais dados. Respostas são revalidadas pelo coordenador antes de persistir. Toda persistência do provider projeta novamente o risco. Campos desconhecidos são eliminados, não armazenados. Falha de leitura/regravação impede publicar cache e produz mensagem genérica. Se o armazenamento recusar escrita, não é possível garantir saneamento físico; a aplicação bloqueia a inicialização e não afirma que o disco foi corrigido.
- R3: a regex antiga protegia só o primeiro segmento. O simulador agora resolve primeiro os spans de senha; sem aspas termina em vírgula, ponto e vírgula, quebra de linha, próximo marcador ou fim. Espaços e demais pontuações ficam dentro do valor protegido. Aspas correspondentes são preservadas. Só um valor integralmente igual a rótulo é considerado protegido. Outros detectores continuam ativos. Esta política é conservadora e pode ocultar texto não sensível em casos ambíguos; não reconhece toda forma possível de PII.

Não houve mudança em C#, schema, ApiExceptionHandler, entregas acadêmicas ou resultados históricos. O patch do review anterior continua sendo um retrato anterior às correções; não representa o estado atualizado e não deve ser aplicado sobre esta árvore.

## Validação observada

Comando: `node mobile/tests/run-cyber-isolated.mjs`.

- Primeira execução: 102 aprovados, zero falhas/ignorados/cancelados, exit 0, 69,2077 ms.
- Após acrescentar o caso de persistência do mock: 103 aprovados, zero falhas/ignorados/cancelados, 59,6306 ms. São os 79 anteriores mais 24 novos.
- Testes novos: respostas invertidas, troca API/MOCK nos dois sentidos, invalidação, commit enfileirado obsoleto, fallback/indisponibilidade, resposta inválida, cache inválido/com PII, projeção de extras, falha de leitura/escrita, recuperação após commit falho, senhas com espaços/aspas/delimitadores/rótulos/idempotência e persistência do mock sem fragmentos.
- `git diff --check`: sem erros; avisos de conversão LF/CRLF em alterações preexistentes.

Os testes importam o módulo de coordenação/restauração realmente usado pelo provider, mas não montam React. Armazenamento dos novos testes é simulado; o teste core existente usa SQLite temporário. Não houve SQL Server, API, Expo ou inferência real. O runner substitui fetch, não é sandbox geral de rede. Aviso Node sobre módulo sem tipo explícito permanece; não foi alterado package.json para suprimi-lo.

## Taint Analysis: mecanismos reais e proposta pendente

Revisados `OcorrenciasService.CriarAsync`, `ProtecaoPii`, `RiscoCidadaoStore.ContarAsync`, `RiscoCidadaoService.ObterAsync` e `OpenAiExplicacaoCidadaClient.GerarAsync`.

Sources reais: descrição HTTP, eventos persistidos, texto remoto e cache local. Transforms: sanitização antes do store, auditoria técnica sem valores, agregação de vocabulário conhecido, DTO numérico fechado e validação da explicação. Sinks: banco, HTTP de resposta, adapter LLM, armazenamento e UI. O backend não implementa os headers de simulação Mobile e não produz VAZAMENTO_BLOQUEADO nesse fluxo.

Proposta mínima, para aprovação posterior: teste isolado com store espião para comprovar que a descrição original não chega à persistência/resposta; HttpMessageHandler simulado para capturar somente o agregado, verificar ausência dos sentinelas de PII e garantir fallback para saída contaminada. Não chamar provedor real, não criar registros históricos e não adicionar endpoint de falha. Isso comprova as barreiras reais existentes, não um novo evento de bloqueio de log.

Se o professor exigir especificamente marcação de variável e recusa de um sink inseguro com auditoria própria, será necessário aprovar separadamente um guard e seu contrato de metadados. Não foi implementado nesta etapa: seria mecanismo novo de backend. Logs e eventos de unsafeLog do mock continuam explicitamente simulados.

## Pendências e prontidão

- R1/R2/R3 corrigidos no código e cobertos por testes locais dos mecanismos; validar ainda a ligação completa com React/Expo e SQLite/IndexedDB reais.
- R4 permanece como decisão/prova acadêmica pendente conforme proposta acima.
- R5 parcialmente resolvido: cobertura nova de coordenação/cache, mas React, typecheck e Expo continuam pendentes por ausência das dependências locais. Nenhum pacote foi instalado.
- R6/P2 (limite de bytes da resposta Mobile) permanece aberto, fora desta correção P1.
- Cancelamento é lógico: resultados são ignorados; requisições já iniciadas não são fisicamente abortadas. Há timeout HTTP existente; não houve teste real de sua duração. Nenhuma repetição automática foi adicionada.
- Próximo passo: revisão do PM, typecheck/guards com ambiente autorizado e smoke test Expo/API. Expo Web exige CORS/proxy aprovado; nativo precisa de URL alcançável. Validar loading, blur, offline, reabertura e saneamento físico de cache.
- LLM real permanece sem chamada e depende de autorização/configuração segura. Não declarar integração completa nem usar fallback como prova de inferência.

Arquivos desta correção: `mobile/src/services/risk-state.ts`, `mobile/src/services/privacy.ts`, `mobile/src/state/AppContext.tsx`, `mobile/app/(tabs)/risco.tsx`, `mobile/tests/risk-state.test.ts`, `mobile/tests/run-cyber-isolated.mjs` e este documento.
