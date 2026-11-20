# Validação local da interface — Cybersecurity

19/11/2026. Branch: feature/task-008-academic-deliverables.

## Ambiente

Windows, Node 24.21.0, npm 11.19.0. Expo 58.0.25, React/React DOM 19.3.0, React Native 0.88.1, React Native Web 0.21.4, Expo Router 58.0.31 e TypeScript 7.0.2. Node atende aos engines registrados. Lockfile v3; dependências/versões da raiz correspondem ao package.json. Os URLs resolved apontam para registry.npmjs.org.

node_modules estava ausente. Instalação autorizada com lockfile, sem scripts de terceiros: `npm.cmd ci --ignore-scripts --no-audit --no-fund --registry=https://registry.npmjs.org`. A tentativa restrita não progrediu e foi interrompida; cache retornou acesso negado. Repetição com aprovação do ambiente: exit 0, 545 pacotes, 19 s. package.json/package-lock.json não foram alterados. Nenhuma atualização de versão executada.

O comando npm.ps1 foi bloqueado pela política do PowerShell; npm.cmd funcionou sem alterar a política.

## Comandos e resultados

Executados em mobile/, exceto servidores estáticos iniciados na raiz:

| Comando | Resultado observado |
|---|---|
| `npm.cmd run typecheck` | exit 0; nenhum diagnóstico |
| `npm.cmd test` no ambiente restrito | tsx não iniciou os casos: os.userInfo/uv_os_get_passwd ENOMEM |
| `npm.cmd test` com aprovação do ambiente | 61 aprovados, uma falha de carregamento, zero ignorados; exit 1 |
| `node tests/run-cyber-isolated.mjs` | 103 aprovados, zero falhas/ignorados/cancelados; exit 0; 62,3881 ms |
| `node --test tests/render-guards.test.ts` | 11 aprovados, zero falhas/ignorados/cancelados; exit 0; 929,1054 ms |
| `node node_modules/expo/bin/cli install --check` | exit 0, Dependencies are up to date; aviso de confiabilidade limitada offline |
| `node node_modules/expo/bin/cli export --platform web --output-dir dist-cyber-validation-20261119` | exit 0, 860 módulos; MOCK |
| `node node_modules/expo/bin/cli export --platform web --output-dir dist-cyber-api-fixtures-20261119` | exit 0, 860 módulos; contrato API com respostas interceptadas |
| `git diff --check` | sem erros; avisos LF/CRLF em alterações preexistentes |

A primeira checagem Expo incluiu --offline como argumento, rejeitado pela CLI. O comando corrigido usou EXPO_OFFLINE=1 e CI=1 no processo, sem modificar configuração global. Não equivale a expo-doctor completo ou compatibilidade nativa comprovada.

Falha efetiva de npm test: risk-api.test.ts:10 usa top-level await; tsx/esbuild seleciona saída CommonJS e rejeita esse recurso. Nenhum dos 53 casos desse arquivo rodou nesse comando. O runner nativo separado executou esses casos com sucesso. Não apresentar npm test como aprovado. Recomenda-se adequar a inicialização assíncrona do teste ao carregador ou aprovar configuração explícita de módulo; nenhuma correção aplicada nesta validação.

## Renderização real Expo Web

Bundles foram servidos por `python -m http.server`, exclusivamente em 127.0.0.1, portas 8765/8766. Browsers Microsoft Edge headless lançados por Playwright já instalado, contextos novos e isolados, viewport 430 × 932. Todos os browsers foram fechados em finally e os dois servidores próprios foram encerrados por Ctrl+C. Nenhuma API ASP.NET foi iniciada.

O navegador integrado estava indisponível. Playwright utilizou o Edge existente; nenhum navegador foi instalado. Todas as origens fora do servidor local foram bloqueadas por interceptação. No segundo cenário, cada /api/* foi respondido em memória pelo Playwright, antes de alcançar o servidor. Somente GETs simulados; nenhum acesso ao backend ou LLM.

### MOCK — oito verificações aprovadas

Pontuação 25/MÉDIO, contagens CPF 2, telefone 1, email 0, senha 1, saúde 0; alertas e explicação de demonstração. Origem explicitamente fallback sem chamada ao LLM. Indisponibilidade mostra erro genérico e preserva último resultado validado. Offline mantém resultado, desabilita atualização; recuperação remove aviso de resultado antigo. Zero exceções de página.

Artefatos: `mobile/.expo/cyber-validation-20261119-v2/results.json`, `01-mock-risk.png`, `02-unavailable-stale.png`, `03-offline.png`.

### Respostas API simuladas — nove verificações aprovadas

Pontuação 0/BAIXO, explicação LLM fictícia com janela UTC 12–19/11. Loading observado durante atraso controlado de 900 ms; resposta FALLBACK muda o rótulo corretamente. Resposta com senha fictícia é rejeitada, preservando fallback anterior. HTTP 500 simulado não expõe sentinela interna. Cache adulterado com senha fictícia não é renderizado; após reabertura, o snapshot real do IndexedDB contém risk=null e não contém a sentinela. Zero exceções de página. Os 13 requests API interceptados foram GETs.

Artefatos: `mobile/.expo/cyber-validation-api-fixtures-20261119-v2/results.json`, `01-llm-fixture.png`, `02-error-retains-fallback.png`. Capturas inspecionadas visualmente: score, contagens, origem, texto e janela legíveis; mensagens genéricas de falha e preservação do resultado visíveis. São capturas do viewport; conteúdo inferior exige rolagem, não foi presumido capturado só por usar fullPage.

Tentativas iniciais de automação tiveram falhas de seletores (nome acessível do link contém ícone; havia dois progressbars). Corrigidos somente os seletores em memória, sem alterar o aplicativo. Pastas parciais v1 preservadas. Não confundir falha de automação com erro da aplicação.

## Limitações e próxima etapa

- Renderização real validada em React Native Web/Expo Web. Não executado Android/iOS, emulador ou Expo Go.
- Os 11 guards React usam primitivas e contexto substituídos; são diferentes do browser com aplicativo e IndexedDB reais.
- Concorrência fora de ordem e troca de escopo passaram nos testes do coordenador; corrida completa com navegação React ainda merece cenário de browser dedicado.
- O mock legado não fornece janela; ela foi validada com o contrato API simulado. Origem LLM na captura é fixture, nunca inferência real.
- Não houve validação com API real, CORS real, SQL Server, credenciais ou LLM. Persistência nativa SQLite do aplicativo ainda pendente.
- Corrigir o carregamento do teste no npm test; revisar em separado o rótulo acessível de loading do risco, que ainda diz Carregando ocorrências.
- Bundles gerados em dist-cyber-* são artefatos locais não rastreados, não destinados ao commit. Nenhum arquivo foi apagado. Evidências em .expo são ignoradas pelo Git. Este documento é o único arquivo documental novo desta etapa; fontes funcionais, dependências declaradas, backend e entregas C#/SOA preservados.

Parecer: interface Web apta a uma próxima validação controlada com API, após revisão dos resultados e preparação de conectividade/CORS. Não declarar integração ponta a ponta ou validação nativa concluída. Não houve operação Git de escrita.
