# Integração real Expo Web → API de Risco Cidadão

Execução: 19/11/2026 à noite, America/Sao_Paulo; consulta em 20/11/2026 01:44 UTC. Branch feature/task-008-academic-deliverables.

## Ambiente e preflight

- Portas 5188/8081 sem listener antes da inicialização. Nenhuma utilização da porta 5187 nesta demonstração.
- Processo preexistente PID 42432 preservado: saída padrão de ZelaSP.Api, início 2026-11-19T19:50:23.7569049Z.
- DLL separada validada: backend/.artifacts/cors-api-validation/ZelaSP.Api.dll; SHA-256 9FAB82077CC49417DC7ABEBF025916C66835EA69E83D5C6CBCE9D1D2D8DD67DA.
- API PID 24200; início UTC 2026-11-20T01:42:48.9482337Z; executável da saída separada. Listener confirmado 127.0.0.1:5188; logs confirmam Development e content root backend/src/ZelaSP.Api.
- Expo/Metro PID 40628; início UTC 2026-11-20T01:43:08.4420347Z; listener confirmado 127.0.0.1:8081.
- Conexão explicitamente passada por argumento: .\SQLEXPRESS / ZelaSP_Dev, autenticação integrada, Encrypt=true e TrustServerCertificate=true. RiscoAi:Enabled=false explícito. Não houve inspeção direta de catálogo SQL nesta execução; a identidade física do servidor não foi reconfirmada por SELECT.
- Fontes revisados não executam migração, EnsureCreated/Deleted ou seed na inicialização. RiscoCidadaoStore usa consulta agregada AsNoTracking sem SaveChanges.

## Comandos executados

API, com working directory C:\Users\switch\Desktop\ZelaSP\backend\src\ZelaSP.Api:

```powershell
& '..\..\.artifacts\cors-api-validation\ZelaSP.Api.exe' --environment Development --urls 'http://127.0.0.1:5188' --ConnectionStrings:ZelaSP 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true' --RiscoAi:Enabled false
```

Executada fora do sandbox após aprovação do ambiente, para autenticação Windows. Nenhuma alteração de configuração persistente.

Expo, com working directory C:\Users\switch\Desktop\ZelaSP\mobile:

```powershell
$env:EXPO_PUBLIC_DATA_MODE='api'
$env:EXPO_PUBLIC_API_URL='http://127.0.0.1:5188'
$env:EXPO_PUBLIC_TEST_MODE='false'
$env:EXPO_OFFLINE='1'
$env:CI='1'
npm.cmd run web -- --host localhost --port 8081
```

Verificações locais: netstat.exe -ano -p tcp, Get-Process para os PIDs identificados, Get-FileHash da DLL e git diff --check. Não houve build/restore/testes adicionais, SQL direto ou alteração funcional.

## Navegador e isolamento

Microsoft Edge headless via Playwright instalado; novo contexto não persistente, viewport 430 × 932, service workers bloqueados. Origem da página http://127.0.0.1:8081. Não utilizado cache de demonstração anterior.

Interceptação instalada antes de abrir a página: permitiu os assets do Expo em 8081 e exatamente um GET real a http://127.0.0.1:5188/api/risco-cidadao. Esse GET foi liberado com route.continue(), não respondido por fixture, proxy ou route.fulfill. Todas as demais origens/rotas de negócio foram abortadas antes do envio.

Bloqueados de fato: GET /api/ocorrencias, GET /api/bairros e GET /api/categorias. Nenhuma chamada /api/equipe foi observada; as primeiras falhas impediram chegar a ela. Assim, listagem, catálogos, cadastro, status e imagens não foram validados. O bootstrap da página inicial exibe erro genérico esperado dessas chamadas abortadas; a tela de risco funcionou independentemente.

## HTTP real observado

- Uma única consulta de risco real, HTTP 200.
- Content-Type: application/json; charset=utf-8.
- Access-Control-Allow-Origin: http://127.0.0.1:8081.
- Access-Control-Allow-Credentials: ausente.
- Resposta validada por parseRisk(raw, true), incluindo allowlist, pontuação, alertas, janela e texto. JSON integral correspondeu à projeção permitida antes de ser salvo. Nenhuma PII reconhecida ou detalhe interno encontrado. Isso é verificação heurística, não garantia de detecção universal.

| Campo | JSON observado | Dashboard observado |
|---|---|---|
| Pontuação | 0 | 0/100 |
| Classificação | BAIXO | BAIXO |
| CPF/telefone/email/senha/saúde | 0/0/0/0/0 | Cinco cartões zerados, conferidos visualmente e no texto |
| Vazamentos bloqueados | 0 | 0 vazamento(s) bloqueado(s), presente no texto capturado |
| Alertas | [] | Nenhuma detecção de dados pessoais nos últimos 7 dias |
| Início UTC | 2026-11-13T01:44:02.679995Z | Mesmo timestamp |
| Fim UTC | 2026-11-20T01:44:02.679995Z | Mesmo timestamp |
| Origem | FALLBACK | Origem: fallback determinístico da API |

Explicação idêntica na resposta e na interface: “Nenhum evento reconhecido de exposição de dados foi registrado nos últimos sete dias. O indicador não representa perigo no bairro nem garante ausência de riscos.”

Onze verificações automatizadas aprovadas; zero exceções de página. Contagens e apresentação visual também conferidas na captura e no texto extraído. Zero pontos significa ausência de eventos reconhecidos nessa janela, não banco inteiro vazio nem ausência universal de riscos.

## Evidências preservadas

Cópias permanentes consolidadas, sem alterar os originais: [índice e integridade](../../academico/cybersecurity/evidencias/integracao-real-5188-20261119/README.md), [captura](../../academico/cybersecurity/evidencias/integracao-real-5188-20261119/dashboard.png) e [JSON validado](../../academico/cybersecurity/evidencias/integracao-real-5188-20261119/response.json). [Plano futuro do LLM](../../academico/cybersecurity/PLANO_VALIDACAO_LLM.md), baseado somente em inspeção local, sem chamada externa.

Diretório local: mobile/.expo/cyber-real-5188-2026-11-20T01-43-58-563Z/

- http.json: status e cabeçalhos permitidos.
- response.json: resposta validada e verificada antes de gravar.
- dashboard.png: captura real do viewport da tela; score, contagens, explicação e janela legíveis. Não captura toda a área rolável.
- dashboard.txt: texto renderizado, incluindo conteúdo inferior e páginas mantidas no DOM pela navegação.
- results.json: verificações, chamadas abortadas e horário UTC.

São artefatos locais ignorados em .expo, não automaticamente publicados. A captura é integração real com API e banco configurado, com fallback determinístico. Não é fixture, inferência LLM ou prova das demais telas.

## Encerramento e erros

API recebeu Ctrl+C, registrou Application is shutting down e PID 24200 deixou de existir. Expo recebeu duas tentativas Ctrl+C; liberou listener, mas o processo Node continuou ativo. Após revalidar caminho e início do PID próprio 40628, foi executado Stop-Process -Id 40628 -Force, sem ação por nome ou sobre filhos. Sua saída foi confirmada em consulta posterior e a sessão do terminal terminou. Nenhum encerramento do PID 42432.

Verificações finais: PIDs 24200/40628 ausentes; nenhum listener nas portas 5188/8081; PID 42432 permanece com o caminho original. O encerramento com interrupção produziu exit code 1 nas sessões; isso não foi tratado como falha do GET observado.

Aviso Expo: atualização do React Native DevTools falhou ao acessar cache dotslash fora do workspace, e a ferramenta usou versão de fallback. Metro compilou 927 módulos. Não se tentou contornar a restrição ou instalar DevTools manualmente. Avisos Node de módulo sem tipo explícito e NO_COLOR/FORCE_COLOR permaneceram. Nenhum erro HTTP de risco ocorreu.

## Limites e próxima etapa

Comprovado o recorte de leitura: banco configurado → serviço ASP.NET Core real → CORS → cliente Mobile → dashboard Expo Web. Sem migrações, SQL direto, gravação de registros, dados novos, credenciais em arquivos, chamada real ao LLM ou Git de escrita. Não alterados ApiExceptionHandler, funcionalidades, entregas C#/SOA ou configurações permanentes.

Não comprovados: score não zero com dados atuais, demais telas/contratos, integração Android/iOS e inferência LLM. Barreiras e comportamento de PII/falhas continuam com evidências automatizadas anteriores, não foram provocados novos eventos no SQL Server.

Próxima validação LLM exige autorização própria, configuração segura de credencial/modelo, análise de custo e limite de chamadas. Usar somente agregados já previstos e distinguir resposta realmente aceita como LLM de fallback. Nenhuma inferência externa é declarada concluída.
