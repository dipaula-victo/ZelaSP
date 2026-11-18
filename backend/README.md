# ZelaSP — Backend / TASK-001 a TASK-006 e preparação offline TASK-007

## TASK-007 — Demonstração preparada, ainda não executada

Artefatos em [demo/README.md](demo/README.md): plano de 24 registros fictícios, manifesto sem IDs/resultados observados, preparador exclusivamente local, SQL de referências separado das consultas de leitura, procedimentos HTTP manuais, roteiro T01–T10/índice E01–E09 e reinício/renderização de imagens. O relato de PII tem 173 caracteres com marcador válido e expectativa protegida de 186; foi revisado estaticamente para cinco categorias, **sem executar o detector ou comprovar quantidade de eventos**. Amostras PNG/JPEG são cópias das fixtures próprias, com comprimentos e hashes locais registrados.

Nenhum script do pacote, teste, build/restore, SQL, API, OpenAI ou ESP32 foi executado nesta fase. O estado físico atual de ZelaSP_Dev é desconhecido; não usar contagens/IDs históricos como baseline. SQLs têm destino/autorizações/parâmetros/guardas explícitos; nenhuma autorização anterior é reutilizada. O preparador não conecta nem envia requests, não sobrescreve lote existente e conserva permissões de execução como pendentes. Não há provisionamento no startup/migrações.

Execução local, conexão SQL de leitura, criação de referências, mutações HTTP e reinícios exigem aprovações posteriores separadas. Sem limpeza/reparação/retry automático, alterações de schema ou correções Mobile. [Incompatibilidades Mobile](demo/COMPATIBILIDADE_MOBILE.md) impedem afirmar integração ponta a ponta pronta; usar cliente HTTP independente quando autorizado. T/E oficiais, DER/dicionário/relatório final e capturas reais permanecem pendentes de conferência documental/execução. A última suíte histórica é 317 aprovados/um SQL ignorado, não repetida nesta preparação. Ver [evidências planejadas](demo/EVIDENCIAS.md) e VALIDACAO.md.

## Dispositivos e leituras — TASK-006

`GET /api/dispositivos` lista dispositivos em `nome ASC, id ASC`, com última leitura por `lidaEm DESC, id DESC`. Retorna `[]` sem dispositivos e `ultimaLeitura: null` sem leituras. Projeção assíncrona com AsNoTracking, subconsulta escalar e left join em uma consulta, sem N+1 ou materialização de entidades. Regra pura no Domain: **nivelCritico = nivelPercentual >= 90.00m**; 0/89.99 são normais e 90/90.01/100 são críticos. Sem IA/Risco Cidadão ou geração de ocorrências.

`POST /api/leituras` aceita somente `dispositivoId` inteiro positivo existente e `nivelPercentual` numérico de 0 a 100, com até duas casas efetivas. Exemplo: `{"dispositivoId":7,"nivelPercentual":95.25}` (ID ilustrativo; usar um dispositivo realmente provisionado). Servidor gera ID, data UTC de recebimento via TimeProvider com precisão de milissegundos e classificação derivada. Retorna 201 com esses dados, sem Location: não existe GET individual de leitura. Cliente não informa timestamp, id, nivelCritico, bairro ou outros campos. Nenhum dispositivo é criado automaticamente.

Corpo limitado a **1024 bytes**, inclusive sem Content-Length, com leitura antes do model binding limitada a 1025 bytes para detectar excesso. JSON UTF-8 inválido, null, strings numéricas, campos desconhecidos/controlados ou duplicados são rejeitados. Precisão é conferida no token numérico original antes de converter para decimal, evitando arredondamento silencioso de números longos; notação científica é aceita se o valor for representável, estiver no intervalo e tiver precisão efetiva permitida. Zeros finais não aumentam a precisão: 90.0000 é válido, 90.001 é inválido. Erros 400/404/413/415/500 usam Problem Details e não ecoam payloads. Nome/local dos dispositivos devem conter identificação técnica, sem dados pessoais.

Cada POST válido insere uma leitura; não modifica dados anteriores ou cria ocorrências, históricos, eventos de segurança ou mensagens de risco. Não há autenticação do ESP32: dispositivoId é somente referência, não credencial. Não há chave de idempotência; retries após timeout podem duplicar leituras, mesmo críticas. Não realizar retry indiscriminado ou afirmar confirmação de um envio apenas pela coincidência com a última leitura de outro request.

Contrato e exemplo HTTP completo para ESP32 em [API_CONTRACT.md](docs/API_CONTRACT.md). Configure no firmware o host/IP e porta efetivos da API acessíveis na rede; localhost no ESP32 é o próprio dispositivo. Para demonstração em rede controlada, a API precisa escutar numa interface acessível (argumento `--urls` na execução existente); ajustar firewall é responsabilidade do ambiente, sem alterações automáticas nesta task. Não há firmware, seed real, autenticação, endpoint para cadastrar dispositivos ou teste físico ESP32 nesta entrega. O Mobile atual não possui consumidor/tela de telemetria; seus arquivos não foram alterados.

Validação local em 18/11/2026: **317 aprovados, zero falhas e um SQL Server ignorado** (253 anteriores + 64 novos), build sem avisos/erros e EF sem mudanças pendentes. SQLite temporário com afinidade NUMERIC no DDL da fixture para o percentual, mantendo CHECK 0–100; produção/schema/migrações intactos. Tradução SQL Server da consulta foi conferida offline, com conexões bloqueadas. Nenhum acesso SQL Server, chamada externa ou dispositivo físico. Resultados completos em VALIDACAO.md; entrega aguarda Code Review.

## Risco Cidadão e IA — TASK-005

`GET /api/risco-cidadao` retorna indicador global de exposição de dados, não perigo no bairro. O backend captura um único instante UTC; considera os últimos sete dias, limites inclusivos, excluindo eventos futuros. Cada PII_DETECTADO de categoria reconhecida soma CPF 6, TELEFONE 3, EMAIL 3, SENHA 10 ou SAUDE 8; VAZAMENTO_BLOQUEADO soma somente 15, sem incrementar deteccoes. Tipo de evento ou categoria desconhecida é ignorado, inclusive categoria desconhecida em bloqueios. Vocabulário é exato, com caixa e comprimento conferidos. Não há produtor de VAZAMENTO_BLOQUEADO nesta task. Resultado limitado a 100: BAIXO 0–19, MEDIO 20–49, ALTO 50–100. Contagens repetidas são preservadas; saturação antes da multiplicação evita overflow do cálculo.

Calculadora pura no Domain, serviço/DTOs no Application, agregação EF e adapter HTTP em Infrastructure e controller na API. A consulta usa AsNoTracking, filtros temporais, agrupamento no banco e apenas tipo/categoria/contagem. Não carrega descrições ou campos livres da auditoria. No SQL Server usa collation binária e DATALENGTH para evitar que caixa, acentos ou espaços finais mudem o vocabulário reconhecido. Não altera collation do banco ou schema. Não existe índice temporal no modelo atual; avaliar desempenho com volume maior em tarefa própria.

O contrato preserva os campos e textos fixos de alertas do Mobile. Adiciona `janela` e `explicacao` (`texto`, `origem`: LLM/FALLBACK). O parser atual aceita esses campos extras; a tela Mobile ainda não exibe a explicação nova. Não houve alteração no Mobile. GET não grava eventos, ocorrências ou MENSAGEM_RISCO; a explicação agregada é transitória, sem associá-la artificialmente a um evento individual.

Adapter HTTP nativo para OpenAI Responses API, endpoint fixo `https://api.openai.com/v1/responses`, modelo inicial `gpt-4.1-mini`, configurável. Sem SDK/pacote novo. HttpClient por DI, sem redirects/cookies/retries, timeout total de cinco segundos inclusive leitura, resposta limitada a 32 KiB mesmo sem Content-Length. Solicita JSON estruturado com somente texto, `store=false`, `stream=false` e limite de saída; não é declaração de retenção zero pelo provedor. Exige resposta completed, sem recusa/erro, um texto JSON válido e sem campos extras; explicação de 1–600 caracteres em texto simples, sem controles/markup/links/rótulos ou PII reconhecida pelo motor. Rejeição usa fallback, sem refletir o corpo externo ou seus erros. A IA nunca determina pontos, nível, janela ou alertas.

Configuração versionada: `RiscoAi:Enabled=false`, `Provider=OpenAI`, `Model=gpt-4.1-mini`. A propriedade sensível `RiscoAi:ApiKey` não aparece no JSON versionado. Em configuração segura externa, os nomes equivalentes são `RiscoAi__Enabled`, `RiscoAi__Provider`, `RiscoAi__Model`, `RiscoAi__ApiKey`; user-secrets já são suportados pelo projeto Api. Nenhum comando para inserir chave foi executado. Habilitação/uso real exige nova autorização específica; não reutilizar autorizações SQL ou de tarefas anteriores. Configuração ausente/inválida, falha HTTP/rede, timeout, recusa, resposta malformada/excessiva ou PII retornam indicador 200 com fallback determinístico. Falha de banco continua 500 genérico. Cancelamento do cliente é propagado, sem resposta de fallback fabricada.

Cliente externo recebe somente `ResumoRiscoAi`, com números e enum de nível, e reconstrói o payload por allowlist. Não recebe entidades, descrições, eventos completos, nomes/RMs/IDs, imagens ou metadados livres. Logs do cliente HTTP são removidos e sua categoria também é filtrada; adapter não registra chave, prompt, corpo ou resposta. Veja [Taint Analysis](docs/TAINT_ANALYSIS.md) e [contrato REST](docs/API_CONTRACT.md). O motor heurístico não garante detecção universal nem impede toda afirmação inventada pela IA; o indicador autoritativo é sempre o calculado localmente. GET com IA habilitada pode consumir quota/custo por consulta; não há cache/retry automático.

Validação local em 17/11/2026: build zero avisos/erros, **253 aprovados, zero falhas e um SQL Server ignorado** (203 anteriores + 50 novos). EF sem diferenças no modelo. Cliente externo exclusivamente simulado; integração HTTP implementada, mas chamada real à OpenAI **não executada**. Nenhum acesso SQL Server. Restore não foi necessário, sem alterações em dependências. Resultados completos em VALIDACAO.md.

API CitizenTech do ZelaSP (Global Solution FIAP 2026.2), compartilhada pelas disciplinas C# Software Development e SOA & Web Services. A TASK-001 estabeleceu modelo/persistência SQL Server; a TASK-002 implementou ocorrências; a TASK-003 adicionou imagens; a TASK-004 implementou proteção PII; a TASK-005 acrescenta indicador global e adapter LLM com chamada real ainda não executada. A TASK-006 acrescenta dispositivos e leituras; autenticação e funcionalidades posteriores permanecem em tarefas próprias.

## Proteção de relatos — TASK-004

`Application/ProtecaoPii` é uma função pura, sem dependência externa ou estado por request. Reconhece CPF com dígitos verificadores, telefone brasileiro com DDD, e-mail convencional, valor de senha e os seis termos de saúde aprovados. Usa regex sem backtracking com timeout de 250 ms; uma falha interrompe o fluxo, sem gravar texto original. A proteção usa os rótulos `[CPF OCULTADO]`, `[TELEFONE OCULTADO]`, `[E-MAIL OCULTADO]`, `[SENHA OCULTADA]` e `[SAÚDE OCULTADA]`.

O POST valida o contrato e referências, protege a descrição após trim e verifica novamente o limite de 4000 caracteres. Expansão dos rótulos acima do limite retorna 400 sem gravar. Status ABERTA, origem APP, UTC, DTOs e Location são preservados. Ocorrência sanitizada e um EVENTO_SEGURANCA por trecho selecionado são gravados em um único SaveChanges relacional, com transação automática. Os eventos contêm apenas classificações constantes, categoria e UTC; nenhum valor, trecho, posição ou hash do dado pessoal. GET de detalhe/lista e resposta do PATCH protegem também relatos legados, sem regravar dados ou criar eventos na leitura.

Sobreposições: SENHA > EMAIL > CPF > TELEFONE > SAUDE; empate por maior trecho e depois posição inicial. Trechos sobrepostos não geram eventos duplicados; repetições independentes geram eventos separados. Rótulos conhecidos são preservados e não geram novas detecções. Não há endpoint adicional ou alteração de schema.

Limitações: telefone exige DDD reconhecido e formato convencional (fixo iniciando 2–5 ou celular de nove dígitos iniciando 9, com +55 opcional); pode confundir identificadores numéricos com telefones e não comprova linha ativa. CPF válido matematicamente não comprova titularidade; CPF inválido que corresponda a telefone ainda pode ser protegido como TELEFONE. E-mail cobre formato ASCII convencional, sem validar entrega, RFC completa, endereços internacionais ou ofuscação. Senha exige `senha:`, `senha=` ou `senha é`. Sem aspas, todo o valor (inclusive espaços, pontos, !, ? e @) é protegido até vírgula, ponto e vírgula, quebra de linha (CR/LF/NEL/LS/PS), próxima expressão de senha ou fim do texto. Espaços finais antes do delimitador são preservados. Entre aspas simples/duplas, o fechamento na mesma linha delimita o valor, inclusive vírgulas e ponto e vírgula internos; texto posterior é preservado. Aspa sem fechamento aplica o limite conservador sem aspas. Expressões de senha dentro de um valor entre aspas não criam detecções separadas. Somente um valor delimitado inteiro igual a um rótulo conhecido é considerado já protegido; rótulo seguido de conteúdo adicional dentro desse limite é mascarado integralmente como SENHA. Aspas correspondentes são preservadas ao redor do rótulo na saída, mantendo o mesmo limite nas aplicações seguintes. A política pode ocultar narrativa não sensível até o delimitador: em ambiguidade, prioriza proteção. Valores que realmente contêm vírgula/ponto e vírgula ou múltiplas linhas exigem delimitação adequada e não são reconhecidos integralmente como um único valor sem aspas. Saúde cobre apenas diabetes, hipertensão, câncer, HIV, depressão e epilepsia, como palavras completas, ignorando caixa e aceitando formas sem acento/decomposição das vogais acentuadas. Não há análise semântica, OCR ou inspeção de imagens/metadados de upload.

Erros de model binding retornam `errors.request` com mensagem fixa, sem ecoar valores ou nomes arbitrários enviados pelo cliente. Logs de categorias Microsoft.AspNetCore e Microsoft.EntityFrameworkCore são suprimidos no startup e diagnósticos de exceções tratadas são suprimidos, evitando payloads, queries e mensagens de provider; isso reduz observabilidade e deverá ser substituído por diagnóstico estruturado seguro em tarefa própria. Não habilitar logs de corpos, sensitive-data logging ou overrides desses filtros sem revisão. A proteção é uma camada inicial heurística, sem garantia de detecção universal. Dados legados permanecem no banco; apenas suas respostas são protegidas. O schema existente não possui FK evento→ocorrência, portanto a atomicidade não cria vínculo relacional novo.

Validação local em 17/11/2026: restore concluído, build sem avisos/erros, **203 testes aprovados, um SQL Server ignorado**, EF sem mudanças pendentes. Os casos anteriores continuam cobertos, com adaptações documentadas para limites conservadores e preservação das aspas; nenhuma conexão SQL Server foi realizada nesta task. Comandos e resultados em [VALIDACAO.md](VALIDACAO.md).

## Tecnologias e ambiente

- C# / .NET 10, ASP.NET Core Web API e injeção de dependência nativa.
- Entity Framework Core SQL Server e Design 10.0.6; ferramenta local `dotnet-ef` 10.0.6.
- Microsoft.AspNetCore.OpenApi 10.0.6: documento OpenAPI nativo, sem dependência de interface Swagger UI.
- Microsoft.OpenApi 2.7.5 e System.Security.Cryptography.Pkcs 9.0.21 fixados para corrigir vulnerabilidades das dependências transitivas identificadas no restore.
- Testes: Microsoft.NET.Test.Sdk 17.14.1, xUnit 2.9.3, runner Visual Studio 3.1.4 e Microsoft.AspNetCore.Mvc.Testing 10.0.6.
- Somente nos testes: Microsoft.EntityFrameworkCore.Sqlite 10.0.6 e SQLitePCLRaw.bundle_e_sqlite3 3.0.5. O bundle substitui a dependência nativa transitiva vulnerável identificada na restauração, sem suprimir avisos. Nenhum pacote novo no runtime de produção.

Requer SDK .NET 10 e acesso ao NuGet para restaurar os pacotes. Validação no Windows com SDK 10.0.202 e runtime 10.0.6. Endpoints com dados requerem SQL Server; a suíte padrão usa SQLite temporário e não acessa SQL Server. O teste de escrita SQL Server é opt-in e permanece ignorado sem autorização específica.

## Organização e referências

```text
backend/
├── .config/dotnet-tools.json
├── .gitignore
├── Directory.Build.props
├── ZelaSP.sln
├── README.md
├── prompts.md
├── VALIDACAO.md
├── ARQUIVOS.md
├── Migrations/InitialCreate.sql
├── src/
│   ├── ZelaSP.Api/              # configuração HTTP, DI, erros e OpenAPI
│   ├── ZelaSP.Application/      # DTOs, validações, casos de uso e contrato de persistência
│   ├── ZelaSP.Domain/           # nove entidades e duas enumerações
│   └── ZelaSP.Infrastructure/   # DbContext, Fluent API e migrações
├── docs/                       # contrato Mobile e procedimento SQL Server
└── tests/ZelaSP.Tests/          # modelo, HTTP/SQLite e SQL Server opt-in
```

Referências: Api → Application e Infrastructure; Infrastructure → Application e Domain; Application → Domain; Tests → Api (demais referências transitivas). Domain não depende de EF Core ou ASP.NET. `OcorrenciasService` valida e orquestra os casos de uso; `IOcorrenciasStore` contém somente operações concretas necessárias, implementadas com EF Core em Infrastructure. Controllers tratam HTTP, sem regras de transição. Não há repositório genérico ou dependência de EF Core em Application.

## API REST — TASK-002

| Método | Endpoint | Resultado |
|---|---|---|
| GET | `/api/bairros` | Referências por nome/id |
| GET | `/api/categorias-ocorrencia` | Referências por nome/id |
| POST | `/api/ocorrencias` | 201, DTO e Location |
| GET | `/api/ocorrencias` | Página e filtros bairroId/categoriaId/status |
| GET | `/api/ocorrencias/{id}` | Detalhe ou 404 |
| PATCH | `/api/ocorrencias/{id}/status` | Alteração transacional e histórico |
| GET | `/api/ocorrencias/{id}/historico` | Histórico cronológico ou 404 |

Contrato completo: [docs/API_CONTRACT.md](docs/API_CONTRACT.md). POST recebe somente bairroId, categoriaId e descricao (1–4000, não apenas espaços), validando referências. Servidor define ABERTA, APP, data UTC e ID. PATCH recebe somente status textual; números e campos extras são rejeitados.

Regras aprovadas pelo PM/Arquiteto: **ABERTA → EM_ANALISE → RESOLVIDA**, sem repetição, salto ou reabertura (409). `Auditoria:Executor` fornece identificador técnico do servidor, padrão `zelasp-api`, obrigatório com até 120 caracteres, substituível por `Auditoria__Executor`. Não há autenticação de usuários; executor não é recebido do cliente.

Leituras assíncronas usam AsNoTracking e DTOs. Ocorrências: criadaEm DESC/id DESC; pagina mínima 1; tamanhoPagina 1–100, padrão 20; offset que exceda int32 retorna 400. Histórico: alteradoEm ASC/id ASC. Datas UTC com Z. Não há snapshot de paginação entre requisições.

Status e histórico usam uma transação explícita EF Core. UPDATE compara status anterior para detectar concorrência (409); inserção do histórico e commit ocorrem na mesma transação. Exceção anterior ao commit causa rollback. Cadastro inicial não cria histórico de alteração. Nenhuma migração ou mudança de schema foi necessária.

## Imagens — TASK-003

| Método | Endpoint | Resposta |
|---|---|---|
| POST | `/api/imagens` | multipart/form-data; 201, metadados sem bytes e Location |
| GET | `/api/imagens/{id}` | 200, metadados sem Dados; 404 se inexistente |
| GET | `/api/imagens/{id}/conteudo` | 200, stream dos bytes originais, image/png ou image/jpeg |

Enviar somente ocorrenciaId, titulo, executor e arquivo. O serviço valida ocorrência existente, título de 1–100 após trim, executor oficial exato, arquivo não vazio até 2 MiB (2.097.152 bytes) e correspondência de extensão/MIME/assinatura PNG ou JPEG. Dados inválidos retornam 400; ocorrência/imagem inexistente, 404; tamanho excedido, 413; falha interna, 500 genérico. Não há alteração de status/histórico durante upload. Os sete endpoints da TASK-002 foram preservados.

O parser lê multipart de forma assíncrona e limitada, sem model binding de formulário ou arquivos temporários de upload. Corpo total limitado a 2 MiB + 64 KiB; campos de texto a 4096 bytes; limites de headers/boundary também explícitos. Arquivo sem Content-Length segue a mesma contagem efetiva. Nome é apenas metadado normalizado (basename Windows/POSIX, NFC, até 255 e caracteres seguros), nunca caminho. Bytes são inseridos na entidade existente e recuperados do banco; FileStreamResult usa MemoryStream somente para transporte, sem persistência local. Consultas usam AsNoTracking; metadados projetam tamanho sem expor bytes no JSON. Não foram adicionadas dependências.

Configuração acadêmica única em `src/ZelaSP.Api/appsettings.json`, seção `Academico`: RM do líder `554787` e os quatro identificadores oficiais. `EquipeAcademica` valida a configuração no startup; ImagensService e OpenAPI consomem a mesma fonte. O cliente não escolhe RmLider e deve informar exatamente um Executor válido. Isso é identificação acadêmica, **não autenticação**. A configuração `Auditoria:Executor` da TASK-002 continua independente e inalterada; dados acadêmicos não são inseridos em logs próprios.

Assinatura valida somente o formato esperado, sem decodificação completa, garantia de integridade, antivírus ou remoção de metadados. A memória é limitada por requisição/arquivo; não foi implementada quota global de uploads simultâneos. Detecção PII e demais proteções do módulo Cybersecurity não foram antecipadas. Contrato, exemplos multipart e identificação: [docs/API_CONTRACT.md](docs/API_CONTRACT.md).

O modelo, entidades, relacionamentos, InitialCreate, designer e snapshot foram preservados, sem migração ou mudança de schema. A comprovação específica SQL Server de imagens foi concluída em execução única autorizada, com bytes e hashes confirmados; novas escritas exigem nova autorização: [docs/IMAGENS_SQLSERVER_INTEGRATION.md](docs/IMAGENS_SQLSERVER_INTEGRATION.md).

## Modelo de dados existente

| Principal | Dependente | Cardinalidade / FK obrigatória |
|---|---|---|
| BAIRRO | OCORRENCIA | 1 → 0..N / bairro_id |
| CATEGORIA_OCORRENCIA | OCORRENCIA | 1 → 0..N / categoria_id |
| OCORRENCIA | IMAGEM_OCORRENCIA | 1 → 0..N / ocorrencia_id |
| OCORRENCIA | HISTORICO_STATUS | 1 → 0..N / ocorrencia_id |
| BAIRRO | DISPOSITIVO | 1 → 0..N / bairro_id |
| DISPOSITIVO | LEITURA_SENSOR | 1 → 0..N / dispositivo_id |
| EVENTO_SEGURANCA | MENSAGEM_RISCO | 1 → 0..1 / evento_seguranca_id UNIQUE |

As nove tabelas possuem PK `int IDENTITY(1,1)` positiva. Todas as FKs usam `NoAction`: a exclusão de um principal referenciado é bloqueada pelo SQL Server. Imagens são dependentes; uma ocorrência não exige fotografia. As FKs recebem índices gerados pelo EF Core.

Um evento de segurança pode existir sem mensagem (`EventoSeguranca.MensagemRisco` anulável). Toda mensagem exige um evento existente, com `EventoSegurancaId` NOT NULL e índice único não filtrado: no máximo uma mensagem por evento. As navegações são referências em ambos os lados, com Fluent API `WithOne` e sem cascata.

Status são gravados como `varchar(20)` (`ABERTA`, `EM_ANALISE`, `RESOLVIDA`); origem de ocorrência como `varchar(20)` (`APP`, `SENSOR`). CHECK constraints limitam os valores, inclusive no histórico. Datas usam `datetime2(3)`; os futuros produtores devem fornecer UTC (o SQL Server não preserva `DateTime.Kind`). Percentual usa `decimal(5,2)` e restrição SQL inclusiva de 0 a 100, com validação equivalente na entidade. `dados` usa `byte[]` / `varbinary(max)` para os bytes reais da imagem, sem Base64 ou caminho de arquivo substituindo o conteúdo.

Todos os campos textuais e os bytes da imagem são NOT NULL; strings usam `nvarchar` com limites explícitos, exceto enumerações:

| Campos | Limite de caracteres |
|---|---:|
| nome, subprefeitura, executor | 120 |
| descricao | 4000 |
| titulo | 100 |
| nome_arquivo | 255 |
| mime | 100 |
| rm_lider | 20 |
| local | 250 |
| tipo_evento, tipo_dado | 80 |
| origem e destino de evento, acao | 120 |
| mensagem | 2000 |
| origem_mensagem, status_traducao | 40 |

O título da imagem segue o limite máximo de 100 caracteres exigido pelo enunciado FIAP. A validação de entrada de 1 a 100 caracteres será implementada no futuro endpoint de upload; nesta etapa o mapeamento garante NOT NULL e o tamanho máximo. Os demais limites técnicos estão explicitados para revisão do PM/PO. Os vocabulários de `origem_mensagem`, `status_traducao` e dos metadados de segurança não foram inventados; permanecem strings limitadas. EVENTO_SEGURANCA destina-se exclusivamente a classificações e identificadores técnicos, nunca a valores originais de dados pessoais. Não existe campo de payload pessoal, endpoint de gravação ou detector de PII nesta tarefa; a futura implementação deverá sanitizar os metadados antes de persistir. Não há lógica de sensores ou integração com IA.

## SQL Server e configuração local

Configuração autorizada para Development, sem usuário/senha no repositório:

```text
Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true
```

Ela está em `src/ZelaSP.Api/appsettings.Development.json`. Usa a identidade Windows do processo. `TrustServerCertificate=true` é exclusivo para a instância local de desenvolvimento com certificado próprio; em outros ambientes, forneça uma conexão com certificado válido e `TrustServerCertificate=false`.

Configurações de ambiente são carregadas pelo ASP.NET Core. Produção exige `ConnectionStrings:ZelaSP` externo; a API recusa inicialização sem conexão configurada. Variáveis de ambiente e user-secrets podem substituir a configuração local. Exemplo PowerShell a partir de `backend/`, sem credenciais:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__ZelaSP = 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
```

Para outras configurações sensíveis, use o secret store local, nunca um arquivo versionado:

```powershell
dotnet user-secrets set 'ConnectionStrings:ZelaSP' '<connection string local>' --project src/ZelaSP.Api
```

Não copie credenciais reais para exemplos, logs ou documentação. `.gitignore` local exclui `.env`, `appsettings.Local.json`, binários, `obj` e resultados de testes. O arquivo `appsettings.Local.json` não é carregado automaticamente; prefira user-secrets ou variáveis de ambiente.

## Restaurar, compilar e executar

Execute a partir de `backend/`:

```powershell
dotnet restore ZelaSP.sln
dotnet tool restore
dotnet build ZelaSP.sln --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/ZelaSP.Api --no-build --no-restore --urls http://localhost:5187
```

OpenAPI: `http://localhost:5187/openapi/v1.json`, apenas em Development, com dez operações (sete de ocorrências e três de imagens), exemplos, parâmetros e erros, incluindo multipart/binário. Não há interface Swagger UI ou endpoint na raiz. Erros usam Problem Details: 400/404/409 para casos previstos, 413 para upload excedido e 500 genérico, sem stack trace ou mensagem interna. A inicialização não aplica migrações nem grava dados/seeds.

## Migrações — artefatos e aplicação controlada

A primeira migração já está em `src/ZelaSP.Infrastructure/Migrations/`, acompanhada do snapshot. `Migrations/InitialCreate.sql` é o script idempotente de avanço, gerado offline, para inspeção. A factory de design time usa `ConnectionStrings__ZelaSP`, quando definida, ou a conexão integrada local acima. Ela não lê user-secrets; para executar a ferramenta com outro destino, configure a variável de ambiente.

Comandos de geração e conferência (não alteram banco):

```powershell
# Já executado nesta tarefa; não gerar outra InitialCreate sobre a existente.
dotnet ef migrations add InitialCreate --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --output-dir Migrations

dotnet ef migrations script --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --idempotent --output Migrations/InitialCreate.sql
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure
```

**InitialCreate aplicada com sucesso em 14/11/2026**, mediante autorização específica, exclusivamente em `slayr\SQLEXPRESS` / `ZelaSP_Dev`. Antes da aplicação foram confirmados o destino, o estado ONLINE e a ausência de tabelas, objetos de usuário e histórico. A aplicação usou o EF Core e a conexão explícita abaixo, sem executar manualmente o script SQL. Não reaplicar nem alterar o banco sem nova autorização.

```powershell
# Registro do comando autorizado e executado a partir de backend/:
$approved = 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
$env:ConnectionStrings__ZelaSP = $approved
dotnet ef database update InitialCreate --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build --connection $approved
```

A migração EF contém `Down` gerado pelo framework para reversão, mas ele não foi executado; não executar rollback nesta tarefa. O script SQL entregue contém somente a criação inicial e o controle de histórico de migrações.

## Testes, limites e revisão

```powershell
dotnet test ZelaSP.sln --no-restore --filter 'Category!=SqlServerWrite'
```

Os 13 testes originais da TASK-001 foram preservados integralmente e inspecionam o modelo SQL Server sem abrir conexão. A TASK-002 adiciona 39 casos de API/persistência relacional em SQLite em memória, isolados por teste e sem mocks de persistência: T01–T13, filtros, limites, concorrência, AsNoTracking, UTC, erros e OpenAPI. Somente a fixture adapta varbinary(max) para BLOB no DDL temporário; isso não substitui a validação específica SQL Server.

Resultado atual da TASK-003: **111 aprovados, zero falhas e um teste SQL Server ignorado por ausência de autorização** (112 descobertos). Inclui os 53 testes locais anteriores preservados e 58 novos casos de imagens, incluindo projeção SQL offline, SQLite relacional/HTTP sem mocks de persistência, limites, erros e OpenAPI. Fixtures PNG/JPEG são pixels magenta fictícios de 1×1, codificados em Base64 somente no código de testes; a persistência da API usa bytes reais, não Base64. Bibliotecas permanecem as mesmas.

O teste real sequencial de ocorrências/rollback da TASK-002 foi executado uma única vez sob autorização própria e passou, conforme VALIDACAO.md. Ele é ignorado nas execuções padrão atuais e não deve ser reexecutado automaticamente. A concorrência foi validada em SQLite. A TASK-003 teve uma execução única autorizada de imagens, exit code 0, com cinco registros fictícios mantidos e nenhum histórico novo. Procedimentos de novas escritas exigem autorização específica, sem reutilizar flags/autorização anteriores.

Resultados reais, comandos e inventário de cada tarefa estão em [VALIDACAO.md](VALIDACAO.md). [ARQUIVOS.md](ARQUIVOS.md) contém o inventário histórico da TASK-001; o uso de IA está em [prompts.md](prompts.md).

Correções do Tech Lead validadas em 14/11/2026: restore concluído, build com zero erros/avisos, 13 testes aprovados e nenhuma alteração pendente entre modelo e a única migração inicial. Os testes adicionais verificam título obrigatório com máximo de 100, cardinalidade 1 → 0..1, FK obrigatória e única, navegações e SQL gerado. Migração, designer, snapshot e script SQL foram atualizados sem aplicar a migração. Nenhuma dependência foi adicionada ou alterada.

Na conferência histórica após aplicar InitialCreate, consultas de leitura confirmaram nove tabelas de domínio e `dbo.__EFMigrationsHistory`, com um registro `20261114191732_InitialCreate` / `10.0.6`; nove PKs de domínio, sete FKs NO_ACTION, sete índices de FK (um único) e 14 CHECKs habilitadas/confiáveis. Tipos/tamanhos e modelo estavam coerentes. O título usa nvarchar(100); bytes de imagem, varbinary(max). As tabelas estavam vazias naquela ocasião; posteriormente, a prova autorizada da TASK-002 manteve quatro registros fictícios. Esses resultados históricos não são inspeção atual do SQL Server, cuja evidência posterior de imagens está registrada em VALIDACAO.md.

Pendente na TASK-003: revisão final do encerramento e autorização do PM para commit. A prova real de persistência de imagens foi concluída. A identidade Windows tem privilégios amplos; novas escritas exigem autorização específica e conferência do destino/dados atuais. Mobile deve seguir o contrato multipart e buscar conteúdo separadamente dos metadados. Não há autenticação, detector PII, taint analysis, LLM, dashboard, hardware, telas Mobile, ML ou infraestrutura Linux nesta entrega. Não iniciar TASK-004 automaticamente.

Referências técnicas consultadas: [Fluent API e CHECK constraints](https://learn.microsoft.com/ef/core/modeling/indexes/), [exclusões e cascatas](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete), [migrações EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing), [correção OpenAPI](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc) e [correção Pkcs](https://github.com/advisories/GHSA-846f-f764-76mx).
