# ZelaSP — Contrato REST v1 / TASK-002

Contrato para integração Mobile. Endereço local de exemplo: `http://localhost:5187`. OpenAPI disponível em Development: `GET /openapi/v1.json`. Exemplos abaixo usam exclusivamente dados fictícios; os identificadores dependem do banco de destino.

## Convenções

- JSON com nomes camelCase. Enviar `Content-Type: application/json` em POST/PATCH.
- IDs: inteiros positivos de 32 bits. Status/origem: nomes textuais; respostas usam maiúsculas. No PATCH, o conversor aceita os nomes também sem distinção de maiúsculas/minúsculas; números e strings numéricas são rejeitados. No filtro de listagem, usar exatamente `ABERTA`, `EM_ANALISE` ou `RESOLVIDA`.
- Datas: ISO 8601 em UTC, com sufixo `Z`. O servidor define os horários; SQL Server armazena `datetime2(3)`. Não enviar datas nos requests.
- Não há autenticação de usuários nesta etapa. `executor` é um identificador técnico de configuração do servidor, não uma identificação autenticada do cidadão ou atendente.
- POST/PATCH rejeitam campos extras. Cliente não pode enviar `id`, `status` inicial, `origem`, `criadaEm`, `executor` ou campos de histórico fora dos contratos abaixo.
- Não há upload de fotos, criação de bairros/categorias ou integração de sensores nesta tarefa. Ocorrências não exigem fotografia.

## Endpoints

| Método | URL | Sucesso | Erros documentados |
|---|---|---|---|
| GET | `/api/bairros` | 200, array de bairros | 500 |
| GET | `/api/categorias-ocorrencia` | 200, array de categorias | 500 |
| POST | `/api/ocorrencias` | 201, ocorrência e Location | 400, 500 |
| GET | `/api/ocorrencias` | 200, página de ocorrências | 400, 500 |
| GET | `/api/ocorrencias/{id}` | 200, ocorrência | 400, 404, 500 |
| PATCH | `/api/ocorrencias/{id}/status` | 200, ocorrência atualizada | 400, 404, 409, 500 |
| GET | `/api/ocorrencias/{id}/historico` | 200, array de histórico | 400, 404, 500 |

O framework pode retornar 405 para método não suportado e 415 para Content-Type incompatível. IDs de rota inteiros não positivos retornam 400; rotas com ID não interpretável como int32 não correspondem ao endpoint e retornam 404.

## 1. GET /api/bairros

Sem body, parâmetros de rota ou query parameters definidos. Ordenação `nome ASC, id ASC`.

```http
GET /api/bairros
```

Resposta 200:

```json
[{"id":1,"nome":"Bairro fictício","subprefeitura":"Subprefeitura fictícia"}]
```

DTO: `id` int32; `nome` string até 120; `subprefeitura` string até 120. Sem registros: `[]`.

## 2. GET /api/categorias-ocorrencia

Sem body, parâmetros de rota ou query parameters definidos. Ordenação `nome ASC, id ASC`.

```http
GET /api/categorias-ocorrencia
```

Resposta 200:

```json
[{"id":1,"nome":"Descarte irregular"}]
```

DTO: `id` int32; `nome` string até 120. Sem registros: `[]`.

## 3. POST /api/ocorrencias

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| bairroId | int32 | Sim | Maior que zero e existente |
| categoriaId | int32 | Sim | Maior que zero e existente |
| descricao | string | Sim | 1–4000 caracteres antes do trim; não aceita null, vazio ou apenas espaços |

Nenhum campo opcional. O servidor remove espaços nas extremidades da descrição, gera ID/data UTC e fixa status `ABERTA` e origem `APP`. `SENSOR` permanece um valor válido no modelo para a integração IoT futura, mas não pode ser escolhido neste endpoint.

```http
POST /api/ocorrencias
Content-Type: application/json

{"bairroId":1,"categoriaId":1,"descricao":"Descarte fictício para demonstração."}
```

Resposta 201, com `Location: http://localhost:5187/api/ocorrencias/1` (host conforme ambiente):

```json
{
  "id": 1,
  "bairroId": 1,
  "categoriaId": 1,
  "descricao": "Descarte fictício para demonstração.",
  "status": "ABERTA",
  "criadaEm": "2026-11-15T12:00:00Z",
  "origem": "APP"
}
```

O DTO `OcorrenciaResponse` é reutilizado pelo detalhe e pelo PATCH. Campos: `id`, `bairroId`, `categoriaId` int32; `descricao` string; `status` string enum; `criadaEm` date-time UTC; `origem` string enum APP/SENSOR. Não inclui bytes de imagem, navegações ou campos internos do EF Core. O cadastro inicial não cria histórico de alteração, pois ainda não houve transição.

Bairro ou categoria não cadastrados retornam 400 com `errors.bairroId` / `errors.categoriaId`. Ambos podem aparecer na mesma resposta. Campos ausentes, nulos, malformados, desconhecidos ou com tamanho inválido também retornam 400. Não presumir que os IDs dos exemplos existam: consultar as referências antes do cadastro.

## 4. GET /api/ocorrencias

| Query parameter | Tipo | Obrigatório | Padrão / valores aceitos |
|---|---|---|---|
| pagina | int32 | Não | 1; mínimo 1 |
| tamanhoPagina | int32 | Não | 20; entre 1 e 100 |
| bairroId | int32 | Não | Identificador positivo |
| categoriaId | int32 | Não | Identificador positivo |
| status | string enum | Não | ABERTA, EM_ANALISE, RESOLVIDA |

Filtros são combinados por AND. Bairro/categoria positivos sem registros correspondentes produzem página vazia, não 404. Não há filtro por origem, fotografia ou campos inexistentes. Ordenação `criadaEm DESC, id DESC`, com desempate determinístico. O deslocamento `(pagina - 1) * tamanhoPagina` deve caber em int32; caso exceda, retorna 400. Totais e itens são consultas separadas e podem refletir alterações concorrentes entre elas; não há snapshot de paginação entre requisições.

```http
GET /api/ocorrencias?pagina=1&tamanhoPagina=20&bairroId=1&categoriaId=1&status=ABERTA
```

Resposta 200:

```json
{
  "itens": [{"id":1,"bairroId":1,"categoriaId":1,"descricao":"Relato fictício.","status":"ABERTA","criadaEm":"2026-11-15T12:00:00Z","origem":"APP"}],
  "pagina": 1,
  "tamanhoPagina": 20,
  "totalRegistros": 1,
  "totalPaginas": 1
}
```

`itens`: array de `OcorrenciaResponse`; os demais campos são int32. Sem ocorrências: `itens: []`, `totalRegistros: 0`, `totalPaginas: 0`. A página solicitada é preservada. Página além do total também retorna 200 com `itens: []` e totais correspondentes ao filtro.

## 5. GET /api/ocorrencias/{id}

Parâmetro de rota obrigatório: `id` int32 positivo. Sem body ou query parameters definidos.

```http
GET /api/ocorrencias/1
```

Resposta 200: o mesmo objeto `OcorrenciaResponse` apresentado no POST. Identificador inexistente: 404 Problem Details. Todas as consultas de domínio desta API usam `AsNoTracking`.

## 6. PATCH /api/ocorrencias/{id}/status

Parâmetro de rota obrigatório: `id` int32 positivo. Sem query parameters definidos.

| Campo | Tipo | Obrigatório | Valores |
|---|---|---|---|
| status | string enum | Sim | ABERTA, EM_ANALISE, RESOLVIDA; a transição também deve ser permitida |

Nenhum campo opcional. `executor`, horário e status anterior são definidos pelo servidor. O valor de `executor` vem de `Auditoria:Executor` (1–120 caracteres, padrão local `zelasp-api`).

Fluxo aprovado pelo PM/Arquiteto:

```text
ABERTA → EM_ANALISE → RESOLVIDA
```

Não é permitido pular etapas, reabrir, retroceder ou repetir o status. Esses casos retornam 409 sem criar histórico adicional. Valor fora da enumeração, número ou campo ausente retorna 400; ocorrência inexistente retorna 404. Atualização concorrente detectada também retorna 409; o Mobile deve consultar novamente o detalhe antes de decidir nova ação.

```http
PATCH /api/ocorrencias/1/status
Content-Type: application/json

{"status":"EM_ANALISE"}
```

Resposta 200:

```json
{"id":1,"bairroId":1,"categoriaId":1,"descricao":"Relato fictício.","status":"EM_ANALISE","criadaEm":"2026-11-15T12:00:00Z","origem":"APP"}
```

O status e o histórico são gravados na mesma transação explícita do EF Core. UPDATE condicional compara ID e status anterior; nenhuma linha afetada gera conflito. A inserção do histórico e o commit só ocorrem após essa atualização. Qualquer exceção anterior ao commit causa rollback ao descartar a transação. Falha inesperada retorna 500 genérico. A resposta de sucesso descreve o status confirmado por esta operação; outra requisição pode avançá-lo posteriormente.

## 7. GET /api/ocorrencias/{id}/historico

Parâmetro de rota obrigatório: `id` int32 positivo. Sem body ou query parameters definidos. Ordenação `alteradoEm ASC, id ASC`.

```http
GET /api/ocorrencias/1/historico
```

Resposta 200:

```json
[
  {"id":1,"statusAnterior":"ABERTA","statusNovo":"EM_ANALISE","alteradoEm":"2026-11-15T12:05:00Z","executor":"zelasp-api"}
]
```

DTO: `id` int32; `statusAnterior`/`statusNovo` string enum; `alteradoEm` date-time UTC; `executor` string até 120. Ocorrência existente sem alterações retorna `[]`. Ocorrência inexistente retorna 404. Não há acesso a navegações, imagens ou metadados de persistência.

## Erros

Erros usam `application/problem+json`. Validação inclui um dicionário `errors`; mensagens são legíveis, mas não constituem códigos estáveis para lógica do cliente. Model binding e validação de aplicação podem produzir títulos diferentes. Não há stack trace ou mensagem interna de exceção nas respostas.

Exemplo 400:

```json
{"title":"Um ou mais campos são inválidos.","status":400,"errors":{"bairroId":["Bairro não cadastrado."]}}
```

Exemplo 404:

```json
{"title":"Ocorrência não encontrada.","status":404}
```

Exemplo 409:

```json
{"title":"Transição de status não permitida.","status":409}
```

Exemplo 500:

```json
{"title":"Erro interno ao processar a solicitação.","status":500}
```

O framework pode acrescentar `type`, `instance` e `traceId`. O cliente deve usar o código HTTP e o dicionário de validação, tolerando esses campos adicionais. Erros de JSON não expõem nomes internos dos tipos de aplicação.

## Integração Mobile e próximas etapas

1. Buscar referências; guardar os IDs reais. Referências vazias exigem provisionamento autorizado, fora desta API; não existem seeds automáticos ou endpoints de criação de referências.
2. Cadastrar somente bairroId, categoriaId e descricao; usar ID/Location retornados.
3. Paginar, aplicar filtros e usar o detalhe como estado atual.
4. Oferecer apenas a próxima transição válida e tratar 409 consultando novamente.
5. Mostrar datas conforme fuso da interface; o transporte permanece UTC.

Validação no SQL Server com escrita está preparada, mas não foi executada na TASK-002. Ver [SQLSERVER_INTEGRATION.md](SQLSERVER_INTEGRATION.md). O provider SQLite temporário prova comportamento relacional e rollback nos testes locais, sem substituir a comprovação específica no SQL Server.

Cybersecurity deverá integrar detecção/sanitização de PII e demais mecanismos em tarefas próprias. Os DTOs e o serviço de aplicação são os pontos de entrada para essa integração; não foram adicionados detectores, LLMs, fluxos de IA ou escrita em EVENTO_SEGURANCA/MENSAGEM_RISCO. Auditoria técnica não substitui autenticação futura de usuários.
