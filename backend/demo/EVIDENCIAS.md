# Roteiro T01–T10 e índice E01–E09 — proposta interna

Estado dos ensaios TASK-007: **TODOS PENDENTES**. Não houve testes, build, SQL, HTTP, OpenAI, reinício ou renderização nesta preparação. Enunciados/rubricas/guias oficiais não foram localizados: associação destes IDs com critérios oficiais **PENDENTE DE CONFERÊNCIA DOCUMENTAL**. Não reapresentar testes T01–T13 da TASK-002 como se fossem a numeração oficial T01–T10.

## Roteiro integrado

| Teste | Requisito/ação futura | Resultado esperado | Evidência |
|---|---|---|---|
| T01 | Identidade, migração, schema, referências e baseline | Destino correto; guardas satisfeitas; marcador ausente | E01 |
| T02 | Criar A/B/C e negativos de contrato/referência | 201/Location nos válidos; 400 sem delta nos inválidos | E02/E06 |
| T03 | Listar, filtrar, paginar, detalhe/ID inexistente | 200 com itens/totais/ordem; 404 no inexistente conferido | E02 |
| T04 | Transições B/C, histórico e conflitos | B 1 histórico/C 2; 409 sem delta para salto/repetição | E02/E03 |
| T05 | PNG/JPEG, metadados, binário e negativos | 201/200, hashes/MIME/vínculo; 400/404/413 sem delta inválido | E04 |
| T06 | Encerrar/reiniciar API e cliente; recuperar/renderizar | Bytes idênticos após reinícios, sem novo upload | E05 |
| T07 | PII fictícia, respostas protegidas, auditoria segura | Cinco categorias esperadas, sem originais nos sinks | E06 |
| T08 | Risco global, janela e fallback | Contagens/pontos iguais a SQL na mesma janela; alertas fixos | E06/E07 |
| T09 | Dispositivo sem leitura; 89.99/90/90.01, negativos | null; false/true/true; última correta; nenhuma ocorrência automática | E08 |
| T10 | Integridade, regressão, índice e reprodução | Evidências reais e limitações sem resultados inventados | E01/E09 |

## Índice de evidências

Pasta futura de evidência do lote: `execucoes/<MARCADOR>/evidencias/`. Criá-la/preenchê-la somente na etapa autorizada. As extensões abaixo são propostas; formato oficial depende do guia.

| ID | Requisito/procedimento | Esperado | Observado histórico disponível | Arquivos futuros e pendência | Dependências |
|---|---|---|---|---|---|
| E01 | Schema/relacionamentos e preflight | Nove entidades, sete FKs, destino e versão corretos | TASK-001 registra aplicação e conferência; não é estado atual | preflight.txt, DER editável/exportado, dicionário; nova leitura pendente | Repo; SQL leitura |
| E02 | T02–T04 | A/B/C e históricos 0/1/2 | TASK-002 contém HTTP/SQLite e SQL de persistência | requests-responses.json, estados.sql-result.txt; novo lote pendente | API + SQL |
| E03 | COMMIT/ROLLBACK/conflito | Nenhuma escrita parcial | TASK-002: rollback SQL e concorrência SQLite documentados | anexos históricos identificados; nova SQL só se PM exigir/aprovar | Histórico; futura execução autorizada |
| E04 | T05 | PNG/JPEG, metadados e hashes iguais | TASK-003: 201/200, 120/634 bytes, hashes SQL confirmados | uploads.json, metadados.json, hashes-sql.txt; prova nova pendente | API + SQL |
| E05 | T06 | Persistência após reinício e exibição | Nenhuma evidência completa localizada | pids.json, hashes-antes-depois.json, downloads, captura-render.png/video; tudo pendente | Processos próprios + API + SQL |
| E06 | T07/T08 | Sanitização e cálculo global/fallback | TASKs 004/005: testes locais, sem prova SQL integrada nova | descricao-protegida.json, eventos.txt, risco.json, risco-sql.txt | API + SQL leitura/mutações próprias |
| E07 | LLM e taint | Payload agregado, resposta validada/fallback | Adapter implementado e handlers simulados; zero inferência real registrada | referência TAINT_ANALYSIS, resultado local futuro; chamada real pendente de decisão | Local; externo só com autorização |
| E08 | T09 | Valores/UTC/persistência e última leitura | TASK-006: SQLite e tradução SQL offline | leituras.json, dispositivos.json, leituras-sql.txt; físico pendente | API + SQL; hardware se exigido |
| E09 | T10/reprodução | Pacote consistente e rastreável | Última suíte registrada: 317 aprovados/um ignorado | manifesto final, índice requisito→teste→evidência, resultado de regressão/capturas reais | Local; Mobile depende de alinhamento |

Resultado observado do lote TASK-007 em cada linha: **PENDENTE**, não copiar o esperado para a coluna observado. Falhas devem ter seus arquivos e tentativas preservados. Relacionar evidência a commit/versão, marcador, UTC, request, resposta e consulta, sem credenciais ou dados pessoais reais.

## Regras para a prova

- GET/SQL de risco usam a mesma janela/precisão. Não assumir 30 global nem banco vazio.
- Campos de auditoria não têm FK para ocorrência: explicar correlação operacional e concorrência residual. Não inventar causalidade relacional.
- HTTP 400/409 não prova rollback após escrita. Não há endpoint de falha injetada. Uma nova prova SQL de rollback requer dados/autorizações adicionais fora do lote-base.
- E05 exige reinício da API e cliente, conforme REINICIO_IMAGENS.md; novo DbContext não substitui.
- E07 distingue teste de handler simulado, adapter HTTP implementado e inferência real não executada. Fallback não substitui exigência acadêmica de LLM se o professor exigir prova remota.
- HTTP com cliente de PC não é ESP32 físico. Mock Mobile não é backend real. Não executar scripts de navegador que criem dados reais sem autorização própria.

## Checklist do Code Review

- [ ] Conferir a branch/commit e que só backend/ recebeu artefatos documentais/preparatórios.
- [ ] Revisar a quantidade 24 e nomes com marcador; confirmar que imagens ficam em B.
- [ ] Conferir relato manual/limites e distinguir expectativa de cinco eventos de execução comprovada.
- [ ] Conferir fonte/créditos/hashes das duas amostras; nenhum dado pessoal real ou chave.
- [ ] Revisar SQL de referências: destino fixado, integração Windows, marcador, histórico, confirmação e transação própria, sem alterações em registros anteriores.
- [ ] Revisar SQLs de leitura e seus parâmetros obrigatórios; não inventar IDs ou baseline.
- [ ] Revisar manifesto, intenção antes de envio, parada em timeout e ausência de retries/limpeza.
- [ ] Aprovar explicitamente o procedimento manual HTTP e o executor concreto futuro, não apenas os templates.
- [ ] Conferir não persistência da explicação agregada e nenhuma criação de evento de bloqueio simulando fato real.
- [ ] Decidir alinhamento Mobile e necessidade de ESP32/OpenAI reais em fases separadas.
- [ ] Conferir E05 e proteção contra encerrar processos de outros projetos.
- [ ] Obter documentos oficiais, validar T/E e formatos/prazo da entrega.
- [ ] Autorizar separadamente execução local, conexão SQL de leitura, referências SQL, mutações HTTP e reinícios.

## Entregáveis finais fora desta fase

DER editável/exportado, dicionário completo, relatório acadêmico consolidado, capturas/vídeo e índice oficial continuam pendentes de definição documental/execução. Este pacote prepara roteiro e rastreabilidade, não declara esses entregáveis concluídos. Arquivos runtime e resultados não são gerados automaticamente no startup ou na migração.
