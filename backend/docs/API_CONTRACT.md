# ZelaSP — Contrato REST v1 / TASK-002 a TASK-004

Contrato para integração Mobile. Endereço local de exemplo: `http://localhost:5187`. OpenAPI disponível em Development: `GET /openapi/v1.json`. Exemplos abaixo usam exclusivamente dados fictícios; os identificadores dependem do banco de destino.

## Convenções

- JSON com nomes camelCase. Enviar `Content-Type: application/json` em POST/PATCH.
- IDs: inteiros positivos de 32 bits. Status/origem: nomes textuais; respostas usam maiúsculas. No PATCH, o conversor aceita os nomes também sem distinção de maiúsculas/minúsculas; números e strings numéricas são rejeitados. No filtro de listagem, usar exatamente `ABERTA`, `EM_ANALISE` ou `RESOLVIDA`.
- Datas: ISO 8601 em UTC, com sufixo `Z`. O servidor define os horários; SQL Server armazena `datetime2(3)`. Não enviar datas nos requests.
- Não há autenticação de usuários nesta etapa. No histórico de ocorrências, `executor` é um identificador técnico de configuração do servidor. Nas imagens, é uma declaração acadêmica do cliente validada contra a equipe oficial; nenhum deles autentica uma pessoa.
- Nos endpoints de ocorrências, POST/PATCH rejeitam campos extras: cliente não envia `id`, `status` inicial, `origem`, `criadaEm`, `executor` ou campos de histórico. O contrato de imagens da TASK-003, ao final, define seu próprio campo executor obrigatório.
- Ocorrências não exigem fotografia. O upload separado está na seção TASK-003; não há criação de bairros/categorias ou integração de sensores nesta entrega.

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

O teste sequencial SQL Server de persistência/rollback de ocorrências foi executado na TASK-002 sob autorização específica, conforme VALIDACAO.md. O procedimento em [SQLSERVER_INTEGRATION.md](SQLSERVER_INTEGRATION.md) continua exigindo autorização para qualquer nova execução. A concorrência foi verificada localmente com SQLite, sem ser tratada como prova de locking SQL Server.

TASK-004 integra detecção/mascaramento de PII no serviço de ocorrências e grava metadados técnicos em EVENTO_SEGURANCA, sem gravar valores pessoais originais nos eventos. Não há LLM, Taint Analysis completo, autenticação ou geração de MENSAGEM_RISCO nesta etapa.

### Proteção de descricao — TASK-004

O JSON público continua o mesmo. O servidor substitui trechos reconhecidos por `[CPF OCULTADO]`, `[TELEFONE OCULTADO]`, `[E-MAIL OCULTADO]`, `[SENHA OCULTADA]` ou `[SAÚDE OCULTADA]` antes do INSERT e da resposta 201. As respostas de detalhe, lista e PATCH também protegem descrições legadas sem modificar registros. O cliente deve exibir a descrição retornada e não presumir igualdade com o texto enviado. Não enviar dados reais em demonstrações.

O limite de 4000 caracteres vale para a entrada e para a descrição protegida no POST; expansão acima do limite retorna 400 com `errors.descricao`, sem INSERT de ocorrência/eventos. Erros automáticos de binding/validação usam `errors.request` com mensagem fixa, sem ecoar valores ou nomes arbitrários de campos. Códigos HTTP, DTOs, status inicial, origem, datas e Location permanecem preservados. Regras, precedência e limitações de reconhecimento estão no README; não há promessa de sanitização de imagens ou metadados de upload.

## TASK-003 — Imagens de ocorrências

Os sete endpoints de ocorrências acima mantêm seu contrato. Foram adicionadas exatamente três operações:

| Método | URL | Sucesso | Erros |
|---|---|---|---|
| POST | `/api/imagens` | 201, metadados e Location | 400, 404, 413, 500 |
| GET | `/api/imagens/{id}` | 200, metadados sem bytes | 400, 404, 500 |
| GET | `/api/imagens/{id}/conteudo` | 200, bytes PNG/JPEG | 400, 404, 500 |

### POST /api/imagens

Enviar `multipart/form-data`, deixando a biblioteca HTTP gerar o boundary. Não enviar JSON nem definir manualmente um Content-Type sem boundary. Os nomes de campo são os indicados abaixo, sensíveis a maiúsculas/minúsculas; cada campo pode aparecer uma única vez. Partes podem vir em qualquer ordem.

| Campo | Tipo | Obrigatório | Validação |
|---|---|---|---|
| ocorrenciaId | inteiro positivo em texto | Sim | int32, dígitos sem sinal, ocorrência existente |
| titulo | texto UTF-8 | Sim | Trim; 1–100 caracteres .NET/UTF-16 após trim; não apenas espaços |
| executor | texto UTF-8 | Sim | Um identificador exato da equipe configurada, sem trim ou comparação indiferente a caixa |
| arquivo | binário | Sim | Uma imagem não vazia, máximo 2.097.152 bytes, PNG ou JPEG |

Não há campos opcionais. Campos extras, incluindo `rmLider`, `dataUtc` ou `id`, e campos/arquivos duplicados retornam 400. RmLider é definido pelo servidor como `554787`; dataUtc é gerada no servidor em UTC. A identificação acadêmica **não é autenticação** e não existe login nesta tarefa.

Equipe oficial, mantida em uma única configuração `Academico` do servidor:

| Integrante | RM | Identificador aceito em executor |
|---|---|---|
| Victor Hugo de Paula — líder | 554787 | `VictorHugoDePaula_RM554787` |
| Felipe Carioba | 558447 | `FelipeCarioba_RM558447` |
| Djalma Andrade | 555530 | `DjalmaAndrade_RM555530` |
| Lucas Rodrigues | 556323 | `LucasRodrigues_RM556323` |

Formatos aceitos: `.png` / `image/png`; `.jpg` ou `.jpeg` / `image/jpeg`. Extensões e MIME são comparados sem distinção de caixa, e o MIME persistido é canônico. `image/jpg` não é aceito. A extensão, o MIME informado e a assinatura precisam concordar. PNG exige o prefixo binário de oito bytes `89 50 4E 47 0D 0A 1A 0A`; JPEG exige `FF D8 FF`. GIF, SVG, PDF, ZIP, assinatura ausente/incompatível e demais tipos são rejeitados.

**Assinatura identifica o formato esperado; não valida completamente a integridade, decodificação, dimensões, conteúdo ou segurança do arquivo.** A aplicação não executa ou interpreta imagens. Os bytes são armazenados no banco e recuperados sem transformação.

O nome fornecido nunca é caminho de persistência. O servidor remove componentes de caminho Windows/POSIX, normaliza o nome final em Unicode NFC e faz trim; exige nome de 1–255 caracteres, sem controles, caracteres de formatação/invisíveis ou caracteres reservados `< > : " / \ | ? *`, sem início em ponto e com nome-base não vazio. Esse nome fica somente como metadado; o cliente deve tratá-lo como texto, nunca como HTML.

Limites de recepção: arquivo até 2 MiB inclusive; corpo multipart total até 2.162.688 bytes (2 MiB + 64 KiB para campos/envelope), com contagem efetiva inclusive sem Content-Length e sobre o epílogo; cada campo textual até 4096 bytes UTF-8; boundary até 70 caracteres; até 16 headers/8192 bytes por parte. Esses limites são proteções de recursos e independem do tamanho declarado pelo cliente. Excesso de arquivo/corpo retorna 413; campo textual ou multipart inválido retorna 400. Não há buffering automático de formulário em diretório temporário ou público.

Exemplo PowerShell com curl.exe, **somente contra um ambiente autorizado para escrita**:

```powershell
curl.exe -i -X POST http://localhost:5187/api/imagens `
  -F "ocorrenciaId=1" `
  -F "titulo=Pixel fictício" `
  -F "executor=VictorHugoDePaula_RM554787" `
  -F "arquivo=@pixel-ficticio.png;type=image/png"
```

O arquivo local do exemplo é apenas a fonte do cliente; a API não persiste em disco. O ID deve identificar uma ocorrência real do ambiente autorizado. Esses comandos não foram executados contra SQL Server nesta TASK-003.

Resposta 201 com `Location: http://localhost:5187/api/imagens/1`:

```json
{
  "id": 1,
  "ocorrenciaId": 1,
  "titulo": "Pixel fictício",
  "nomeArquivo": "pixel-ficticio.png",
  "mime": "image/png",
  "tamanhoBytes": 68,
  "dataUtc": "2026-11-16T12:00:00Z",
  "rmLider": "554787",
  "executor": "VictorHugoDePaula_RM554787"
}
```

DTO `ImagemMetadadosResponse`: id, ocorrenciaId e tamanhoBytes são int32; titulo/nomeArquivo/mime/rmLider/executor são strings; dataUtc é date-time UTC. O tamanho do exemplo é ilustrativo. Não existe `dados`, Base64 ou navegação EF no JSON. Upload não modifica a ocorrência, seu status, seus demais campos ou seu histórico.

### GET /api/imagens/{id}

ID int32 positivo obrigatório; sem body ou parâmetros de query definidos.

```http
GET /api/imagens/1
```

Retorna 200 com os mesmos nove campos de metadados do exemplo anterior. Imagem inexistente retorna 404 com Problem Details. ID inteiro não positivo retorna 400; rota não interpretável como int32 retorna 404.

### GET /api/imagens/{id}/conteudo

ID int32 positivo obrigatório; sem body ou parâmetros de query definidos.

```http
GET /api/imagens/1/conteudo
```

Resposta de sucesso:

```http
HTTP/1.1 200 OK
Content-Type: image/png
X-Content-Type-Options: nosniff

<bytes binários originais armazenados>
```

Para JPEG, Content-Type é `image/jpeg`. O conteúdo vem de IMAGEM_OCORRENCIA.Dados, retornado por stream; não há JSON/Base64, redimensionamento, recodificação ou leitura de caminhos locais. O nome original não é usado como caminho ou cabeçalho de download. Imagem inexistente retorna 404 com Problem Details. Não há suporte a listagem/remoção de imagens ou upload de múltiplos arquivos nesta tarefa.

### Erros de imagens

Todas as respostas de erro acima usam `application/problem+json`, sem bytes, stack trace, dados acadêmicos do request ou detalhes internos do SQL Server. Exemplos:

```json
{"title":"Um ou mais campos são inválidos.","status":400,"errors":{"arquivo":["Extensão, MIME e assinatura binária devem corresponder a PNG ou JPEG."]}}
```

```json
{"title":"Ocorrência não encontrada.","status":404}
```

```json
{"title":"Imagem não encontrada.","status":404}
```

```json
{"title":"O arquivo ou corpo da requisição excede o limite permitido.","status":413}
```

```json
{"title":"Erro interno ao processar a solicitação.","status":500}
```

### Integração Mobile e prova SQL Server

O Mobile deve enviar somente os quatro campos, usar o ID/Location retornado, consultar os metadados e buscar o binário separadamente. Manter o limite de 2 MiB no cliente melhora a experiência, mas não substitui a validação do servidor. Executor não deve ser apresentado como usuário autenticado; RmLider não deve ser enviado. A API pode retornar primeiro um erro de formato antes da verificação de existência se houver mais de uma entrada inválida.

Os novos testes comprovam persistência/recuperação em SQLite real e temporário. A validação real SQL Server foi concluída em lote único autorizado: dois uploads 201, GETs 200, bytes e hashes confirmados por consulta independente, sem alterar estado ou histórico da ocorrência. A autorização foi consumida; novas escritas exigem nova aprovação. O procedimento e seu resultado histórico estão em [IMAGENS_SQLSERVER_INTEGRATION.md](IMAGENS_SQLSERVER_INTEGRATION.md) e não autorizam repetição do lote.
