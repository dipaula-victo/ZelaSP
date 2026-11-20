# Integração real — Risco Cidadão com fallback

Evidência de Cybersecurity. Não utiliza a numeração oficial de C# nem altera suas entregas.

## Arquivos permanentes

- [Dashboard real](dashboard.png): PNG, 430 × 932 pixels, captura do viewport.
- [Resposta real validada](response.json): JSON recebido do backend, copiado integralmente.
- [Integridade das cópias](INTEGRIDADE.json): tamanho, SHA-256 e origem dos dois arquivos; não inventaria os documentos Markdown.
- [Relatório técnico da execução](../../../../docs/revisoes/CYBERSECURITY_INTEGRACAO_REAL_5188.md): comandos, isolamento, processos, HTTP e encerramento.
- [Requisitos para validar o LLM](../../PLANO_VALIDACAO_LLM.md): inspeção estática, sem chamada externa.

## Origem e resultado observado

Execução em 19/11/2026, 22:44, America/Sao_Paulo. Horário registrado: 2026-11-20T01:44:03.654Z. Originais preservados em `mobile/.expo/cyber-real-5188-2026-11-20T01-43-58-563Z/`.

Ambiente: API ASP.NET Core Development em `http://127.0.0.1:5188`; Expo Web em `http://127.0.0.1:8081`; banco configurado `.\SQLEXPRESS/ZelaSP_Dev`, autenticação Windows. Build separado validado. `RiscoAi:Enabled=false` passado explicitamente na inicialização.

Endpoint: **GET /api/risco-cidadao**. Uma requisição real, HTTP 200, Content-Type application/json; charset=utf-8. Cabeçalho Access-Control-Allow-Origin igual a http://127.0.0.1:8081; Access-Control-Allow-Credentials ausente. Status e cabeçalhos foram registrados na execução original em http.json; a cópia permanente de response.json é o corpo, não um registro de cabeçalhos.

| Campo | Resultado real |
|---|---|
| Pontuação/classificação | 0 / BAIXO |
| CPF, telefone, email, senha, saúde | Todos 0 |
| Vazamentos bloqueados | 0 |
| Alertas | Lista vazia |
| Período | 7 dias |
| Janela UTC | 2026-11-13T01:44:02.679995Z até 2026-11-20T01:44:02.679995Z |
| Explicação | Nenhum evento reconhecido de exposição de dados foi registrado nos últimos sete dias. O indicador não representa perigo no bairro nem garante ausência de riscos. |
| Origem | FALLBACK |

JSON e dashboard corresponderam na execução, com onze verificações automatizadas e conferência visual. Pontuação zero não comprova banco vazio nem ausência universal de risco.

## Segurança e limites

Antes da consolidação, response.json foi novamente lido e validado com parseRisk(raw, true). A projeção permitida corresponde exatamente ao corpo original; nenhuma categoria de PII reconhecida foi encontrada no texto. A inspeção também não encontrou nomes, IDs individuais, auditoria livre, credenciais ou detalhes internos. Essa verificação é heurística, não garantia universal de ausência de informação pessoal.

Nenhuma requisição foi feita na consolidação. Na demonstração histórica, somente o GET de risco alcançou o backend; consultas GET a ocorrências, bairros e categorias foram abortadas pelo Playwright antes do envio. Essas funcionalidades e os demais contratos Mobile não estão comprovados por esta evidência. A captura mostra somente o viewport, sem toda a área rolável.

**API real com fallback determinístico:** não é resposta simulada e não é inferência real de LLM. Não houve chamada ao LLM ou escrita no banco nessa demonstração. Nenhuma nova execução foi feita para copiar os arquivos. Nenhum resultado histórico foi alterado.

Os dois arquivos foram copiados byte a byte, sem sobrescrita; tamanho e SHA-256 conferidos contra os originais. A política CORS restringe origem/rota, mas não autentica o cliente. A identidade física do servidor SQL não foi reconfirmada por SELECT nessa execução.
