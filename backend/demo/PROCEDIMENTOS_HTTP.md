# Procedimentos HTTP — pendentes de autorização

**Nenhum request foi executado nesta fase.** Não há script que envie HTTP automaticamente. Este roteiro preserva os contratos atuais e deve ser operado request a request, com registro no manifesto. Aprovação dos artefatos não concede autorização de escrita no banco.

## G0 — Antes de iniciar

- Aprovação específica de mutações HTTP para UM marcador e até 24 registros totais planejados; referências SQL aprovadas separadamente.
- `01-preflight.sql` revisado/executado sob autorização, evidência anexada e sem divergências; saída real do SQL de referências capturada e IDs positivos/distintos conferidos com seus nomes/chaves/vínculos.
- API própria: PID/porta/binário/commit identificados. Fixar a connection string integrada do destino aprovado por configuração explícita e conferir a configuração efetiva, sem exibir segredos. Não confiar apenas na presença de um processo na porta ou em um GET bem-sucedido.
- Conferir overrides de configuração e **IA efetivamente desabilitada**. Não inserir credencial ou reaproveitar flag/autorização SQL antiga. Windows integrada não autentica o cidadão/ESP32.
- Um único escritor no lote, sem uso paralelo do aplicativo real. Registros de baseline são intocáveis. Se a propriedade do processo/API ou dos IDs for incerta, parar.
- Somente cliente HTTP independente para a demonstração backend atual; ver incompatibilidades Mobile. Qualquer execução fora de sandbox precisa do comando integral e confirmação individual quando exigidos pelo ambiente.

## G1 — Guarda obrigatória em CADA mutação

1. Ler estado da etapa no manifesto: só PENDENTE, tentativa zero. Uma etapa CONFIRMADA não é repetida; EM_ENVIO/AMBIGUA/FALHOU/PARCIAL interrompe escritas até revisão.
2. Conferir pré-condição na tabela abaixo com GET e/ou SQL de leitura autorizado. Resolver IDs do próprio lote; nunca MAX/IDENT_CURRENT ou ID fixo. Não avançar status de registro anterior ao baseline.
3. Salvar intenção EM_ENVIO, método/rota, instante, tentativa 1 e identificadores lógicos antes de enviar. Se não conseguir salvar, não enviar.
4. Fazer UMA chamada. Não habilitar retry da ferramenta, proxy ou biblioteca.
5. Exigir código esperado, formato, ID/Location quando aplicável, vínculo e estado. Salvar resposta e IDs imediatamente. Não armazenar Authorization/tokens/segredos no manifesto.
6. Conferir efeitos de leitura. Só então marcar CONFIRMADA. Falha/timeout/ausência de resposta ou falha de gravação da evidência torna a etapa AMBIGUA; interromper, preservar e pedir leitura/revisão, sem novo POST/PATCH automático.

## Ordem e pré-condições

`{API}` é `destinoEsperado.apiBaseUri` aprovado, não placeholder executável. IDs vêm de `manifesto.ids`. A/C usam descrições do plano com marcador; B usa template PII. Nunca enviar os envelopes plano/requests-pendentes.json diretamente como DTO.

| Etapa | Request | Pré-condição | Esperado |
|---|---|---|---|
| criarA | POST /api/ocorrencias | Referências confirmadas; nenhuma ocorrência A com marcador | 201/Location, APP/ABERTA, histórico vazio |
| criarB | POST /api/ocorrencias | Nenhuma B; baseline de eventos capturado; janela exclusiva | 201/Location, descrição protegida exata, cinco eventos ainda a comprovar |
| criarC | POST /api/ocorrencias | Nenhuma C com marcador | 201/Location, APP/ABERTA |
| analisarB | PATCH /api/ocorrencias/{B}/status | B própria ABERTA, histórico 0 | 200 EM_ANALISE, histórico 1 |
| analisarC | PATCH /api/ocorrencias/{C}/status | C própria ABERTA, histórico 0 | 200 EM_ANALISE, histórico 1 |
| resolverC | PATCH /api/ocorrencias/{C}/status | C própria EM_ANALISE, histórico 1 | 200 RESOLVIDA, histórico 2 |
| uploadPng | POST /api/imagens multipart | B própria, PNG ausente; hash/título/arquivo previstos | 201/Location, novo ID e metadados |
| uploadJpeg | POST /api/imagens multipart | JPEG ausente e PNG confirmada | 201/Location, outro ID e metadados |
| leitura8999 | POST /api/leituras | Dispositivo próprio, nenhuma leitura | 201, 89.99/false |
| leitura9000 | POST /api/leituras | Uma leitura própria, etapa anterior confirmada | 201, 90.00/true |
| leitura9001 | POST /api/leituras | Duas leituras próprias | 201, 90.01/true |

Após cada leitura: GET /api/dispositivos e captura da ultimaLeitura. Ao final, dispositivo com leitura aponta a 90.01; o outro mantém null. Leituras ordenadas por lidaEm DESC, id DESC. Não forçar relógio/timestamp para provocar empate: cobertura de empate SQLite é histórica; prova física controlada precisaria de autorização adicional.

## Corpos e campos

POST ocorrência contém **somente** bairroId, categoriaId e descricao. Não enviar origem, status, datas, IDs ou executor. Exemplo de forma, com IDs dinâmicos a substituir:

```json
{"bairroId":"<ID inteiro centro>","categoriaId":"<ID inteiro descarte>","descricao":"Demonstração fictícia <MARCADOR>; cenário A sem dados pessoais."}
```

As aspas dos placeholders de ID devem ser removidas na materialização: no JSON real, IDs são números, não strings. O preparador local deixa IDs nulos de propósito; nem null nem placeholders são corpo válido. C usa norte/buraco; B centro/lixeira. Ver os textos em carga.template.json.

PATCH contém somente `{"status":"EM_ANALISE"}` ou `{"status":"RESOLVIDA"}`. Não enviar executor. GET /api/ocorrencias/{id}/historico é separado do detalhe.

Upload: exatamente ocorrenciaId (ID B em texto no multipart), titulo (`<MARCADOR> PNG` ou `<MARCADOR> JPEG`), executor `VictorHugoDePaula_RM554787` e arquivo. PNG=image/png, JPEG=image/jpeg. Não enviar RmLider/data/bytes em JSON. Não definir boundary manualmente numa biblioteca que já o gera. Exigir RmLider 554787 do servidor, nove metadados sem Dados, bytes 120/634, UTC e Location para /api/imagens/{id}. GET metadados/conteúdo devem retornar 200/MIME correto. Datas POST/GET podem diferir em até 1 ms pela precisão datetime2(3); bytes não admitem tolerância.

Leitura: apenas `{"dispositivoId":<ID numérico comLeitura>,"nivelPercentual":89.99}`, repetindo deliberadamente a forma com 90.00 e 90.01 em etapas distintas. HTTP 201, sem Location; data é recebimento do servidor em UTC, sem timestamp do ESP32. Até duas casas efetivas; 1 KiB inclusive sem Content-Length. Cada POST válido cria uma leitura, não ocorrência/histórico/evento.

## Consultas e negativos — sem gravação esperada

- GET /api/bairros e /api/categorias-ocorrencia: localizar referências próprias, não presumir listas globais de 2/4.
- GET /api/ocorrencias?bairroId=<centro>&categoriaId=<lixeira>&status=EM_ANALISE&pagina=1&tamanhoPagina=1: resposta paginada com itens, totais coerentes e ordenação criadaEm DESC/id DESC. Conferir por IDs, não pela posição global.
- GET detalhe/histórico/imagens por IDs próprios: 200. ID inexistente deve ser escolhido e confirmado por SELECT autorizado, não presumido como 999: esperar 404.
- Campo extra/controlado, referência inválida ou descrição/título inválido: 400 e nenhum delta. Transição repetida/salto: 409 sem histórico adicional. Usar só ocorrência própria e jamais um status válido como suposta falha de rollback.
- MIME/extensão/assinatura inválidos, executor ausente/inválido: 400; ocorrência inexistente em upload: 404; arquivo acima de 2 MiB: 413. Preparar payload negativo só após revisão, sem alterar amostras fonte.
- Leituras -0.01/100.01/90.001, strings/null, campos extras: 400; dispositivo inexistente: 404; Content-Type errado: 415; corpo >1024 com/sem Content-Length: 413. Conferir ausência de delta; não deixar uma ferramenta reenviar automaticamente um valor válido.
- Falha 400/409 antes de gravar **não é prova de rollback**. Não há endpoint de injeção de falha; headers de simulação Mobile não a implementam. Reusar apenas evidência histórica explicitamente identificada ou obter nova autorização de teste controlado separado.

## Eventos e risco

Após criar B, interromper mutações até conferir descrição sanitizada e cinco eventos novos (uma categoria cada). Usar IDs antes/depois e horário de B persistido no SQL, não comparar só o timestamp de maior precisão do POST. Se houver outros escritores ou diferença de contagem, parar; não criar/editar eventos diretamente. Manifesto guarda IDs revisados e ressalva de que não existe FK causal evento→ocorrência.

GET /api/risco-cidadao deve retornar cálculo global, alertas fixos e explicacao.origem=FALLBACK com IA desabilitada. Capturar janela da resposta e vincular seus parâmetros na consulta 04 com a escala efetiva do EF. Cinco eventos do lote somam 30 brutos; a pontuação global pode conter baseline ou saturar em 100. Não alterar datas ou limpar eventos para obter um número desejado. Nenhum INSERT em MENSAGEM_RISCO.

Conferência final usa 03/04 e deltas de baseline, além de hashes e preservação dos registros anteriores. Qualquer divergência exige revisão, sem reparação automática. Finalizado o lote, somente leituras e procedimento aprovado de reinício, nunca repetição dos uploads.

### Ressalva obrigatória da correlação PII

EVENTO_SEGURANCA não possui FK para OCORRENCIA. O baseline deve anteceder a criação de B; confrontar IDs antes/depois em janela exclusiva sem outros escritores. A correlação é operacional. Igualdade exata de timestamps depende de validação real e não prova causalidade sozinha. Divergência exige interrupção e revisão manual: nunca atribuir eventos arbitrariamente nem alterar timestamps. O procedimento SQL documental está em sql/PROCEDIMENTO_EXECUCAO.md.
