# Catálogo HTTP — estado conferido no código

Base local documentada: `http://127.0.0.1:5187`. JSON com enums textuais; datas UTC. Não foram realizadas requisições nesta conferência. Fonte: controllers em `backend/src/ZelaSP.Api/Controllers`, DTOs em Application e transformers OpenAPI. [Contrato detalhado](../../docs/API_CONTRACT.md).

| Método | Rota | Entrada principal | Sucesso |
|---|---|---|---|
| GET | `/api/bairros` | Sem body | 200, array; pode ser vazio |
| GET | `/api/categorias-ocorrencia` | Sem body | 200, array; pode ser vazio |
| POST | `/api/ocorrencias` | JSON: `bairroId`, `categoriaId`, `descricao` | 201, ocorrência + Location para consulta por ID |
| GET | `/api/ocorrencias` | Query `pagina`, `tamanhoPagina`, opcionais `bairroId`, `categoriaId`, `status` | 200, objeto paginado |
| GET | `/api/ocorrencias/{id}` | ID positivo | 200, ocorrência |
| PATCH | `/api/ocorrencias/{id}/status` | JSON somente `status` | 200, ocorrência atualizada |
| GET | `/api/ocorrencias/{id}/historico` | ID positivo | 200, array cronológico |
| POST | `/api/imagens` | Multipart: `ocorrenciaId`, `titulo`, `executor`, `arquivo` | 201, metadados + Location |
| GET | `/api/imagens/{id}` | ID positivo | 200, metadados sem bytes |
| GET | `/api/imagens/{id}/conteudo` | ID positivo | 200, stream PNG/JPEG com MIME armazenado |
| GET | `/api/risco-cidadao` | Sem body | 200, indicador global e explicação/fallback |
| GET | `/api/dispositivos` | Sem body | 200, array e última leitura ou null |
| POST | `/api/leituras` | JSON: `dispositivoId`, `nivelPercentual` | 201, leitura; sem Location artificial |

São **13 operações de negócio**. `GET /openapi/v1.json` é o documento de infraestrutura, fora dessa contagem.

## Regras essenciais

- Ocorrência: referências existentes, descrição obrigatória até4000 caracteres e proteção PII; o servidor define ABERTA, APP e UTC. Campos extras/controlados não são aceitos.
- Listagem: `pagina=1`, `tamanhoPagina=20`, máximo100; status ABERTA/EM_ANALISE/RESOLVIDA. Resultado contém `itens`, `pagina`, `tamanhoPagina`, `totalRegistros`, `totalPaginas`; não é array simples. Histórico tem endpoint separado.
- Status: ABERTA → EM_ANALISE → RESOLVIDA, sem saltos/reabertura/repetição. Executor do histórico é configuração do servidor, não campo do PATCH. UPDATE e INSERT são transacionais.
- Imagens: PNG/JPG/JPEG coerentes entre extensão/MIME/assinatura; máximo2097152 bytes; título após trim de1 a100; executor permitido; ocorrência existente. Cliente não controla `rmLider`, data ou ID. A assinatura não comprova segurança/integridade completa do arquivo.
- Risco: últimos sete dias UTC, indicador calculado localmente e GET sem gravações; integração LLM desabilitada por padrão, sem necessidade de credenciais para o núcleo SOA. Não chamar o provedor para apenas consultar a documentação.
- Dispositivos: ordem nome/id; última leitura por data/id decrescentes. Leitura:0–100, até duas casas efetivas, corpo máximo1024 bytes mesmo sem Content-Length. Datas e nível crítico (`>=90`) são definidos pelo servidor. Não cria ocorrências automaticamente.
- Erros:400 validação,404 recurso inexistente,409 conflito,413 excesso de tamanho,415 Content-Type inadequado quando aplicável e500 inesperado. Problem Details mantém mensagem500 genérica. Não interpretar todos os códigos como possíveis em todas as rotas.

## OpenAPI automático

`Program.cs` registra `AddOpenApi` e transformers `ContratoOpenApiTransformer`/`ImagensOpenApiTransformer`; controllers descrevem tipos e respostas. `MapOpenApi()` é condicionado a Development. O documento descreve JSON, multipart, streams, parâmetros e erros. Não há Swagger UI/Scalar instalado; não é preciso acrescentá-los para este fechamento.

O catálogo Markdown complementa o documento automático, não o substitui como evidência de execução. A captura histórica na página8 do PDF de referência é visualização auxiliar dos contratos reais; não significa execução de todos os endpoints. Exemplos de requests/responses e regras completas permanecem no contrato compartilhado.
