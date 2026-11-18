# Evidências de validação — TASK-001 a TASK-006

**Estado atual (18/11/2026): TASK-006 implementada para Code Review; 317 testes locais aprovados, um SQL Server ignorado, build sem erros/avisos e EF sem diferenças pendentes.** Ver a última seção. Nenhum acesso SQL Server nesta task. As seções anteriores registram etapas históricas, inclusive as provas reais autorizadas das TASKs 002/003; essas autorizações não foram reutilizadas.

Execução em 14/11/2026, Windows, SDK 10.0.202, runtime 10.0.6. Nenhuma conexão ou escrita SQL executada. Todos os arquivos do projeto criados somente em `backend/`. Os caches globais de ferramentas/pacotes do SDK foram utilizados com permissão do ambiente; não são alterações no conteúdo do monorepo.

## Inspeção antes da implementação

Comandos executados na raiz: `Get-Location`, `git status --short`, `Get-ChildItem -Force`, `Get-Content README.md`, `rg --files -g AGENTS.md -g '*csproj' -g '*sln*' -g 'global.json' -g 'NuGet.Config'`, busca de `AGENTS.md` incluindo ocultos e excluindo `.git`, `Get-ChildItem backend -ErrorAction SilentlyContinue`, `dotnet --info`, `python --version`.

Resultado: somente README raiz e pastas `ml/` e `qa/` além de `.git`; nenhum backend, solução ou instrução AGENTS encontrada; Git sem alterações locais. README raiz lido e preservado. `Get-Service -Name '*SQL*'` identificou SQLEXPRESS em execução; `Get-Command SqlLocalDB` encontrou a ferramenta, que não foi usada. O PO proibiu posteriormente aplicar migrações e definiu `ZelaSP_Dev` como destino futuro.

## Preparação e problemas encontrados

- `dotnet nuget locals global-packages --list`: acesso negado ao NuGet.Config do perfil; nenhum resultado de cache declarado.
- `dotnet new sln --name ZelaSP --format sln --output backend`: primeira tentativa bloqueada pelo acesso ao cache `.templateengine`. Os comandos subsequentes de adicionar projetos/restaurar falharam nessa tentativa por falta da solução.
- Repetição autorizada de `dotnet new sln ...` e `dotnet sln backend/ZelaSP.sln add backend/src/ZelaSP.Api/ZelaSP.Api.csproj backend/src/ZelaSP.Application/ZelaSP.Application.csproj backend/src/ZelaSP.Domain/ZelaSP.Domain.csproj backend/src/ZelaSP.Infrastructure/ZelaSP.Infrastructure.csproj backend/tests/ZelaSP.Tests/ZelaSP.Tests.csproj`: concluídos.
- `dotnet restore backend/ZelaSP.sln`: primeira restauração encontrou NU1902/NU1903 em Microsoft.OpenApi 2.0.0 e System.Security.Cryptography.Pkcs 9.0.4. Corrigidos por referências explícitas 2.7.5 e 9.0.21. Não foram suprimidos os avisos ou a auditoria.
- Uma solicitação combinada de alteração/restore/build foi rejeitada pelo controle de execução; ela não executou. As edições foram feitas no workspace e o restore solicitado separadamente, com autorização e sucesso.

## Comandos de validação e resultados reais

| Comando | Resultado |
|---|---|
| `dotnet restore backend/ZelaSP.sln` (após correções) | Sucesso nos cinco projetos, sem avisos de vulnerabilidade reportados |
| `dotnet build backend/ZelaSP.sln --no-restore` | Sucesso, zero avisos, zero erros |
| `dotnet tool restore` (em backend) | dotnet-ef 10.0.6 restaurado |
| `dotnet ef migrations add InitialCreate --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --output-dir Migrations --no-build` | Migração, designer e snapshot gerados offline |
| `dotnet test backend/ZelaSP.sln --no-restore --logger 'console;verbosity=normal'` (primeira rodada) | 9 aprovados, 2 falhas nos testes: configuração Production injetada tarde demais no host e acesso ao conversor de enum pela API de metadados incorreta |
| `dotnet test backend/ZelaSP.sln --no-restore --logger 'console;verbosity=minimal'` (após ajustes) | 11 aprovados, zero falhas, zero ignorados |
| `dotnet ef migrations script --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build --idempotent --output Migrations/InitialCreate.sql` | Script de avanço idempotente gerado, não executado |
| `dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build` | “No changes have been made to the model since the last migration.” |
| `dotnet run --no-build --no-restore --project backend/src/ZelaSP.Api --urls http://127.0.0.1:5187` (Development) | Kestrel iniciou; GET `/openapi/v1.json` respondeu 200, documento OpenAPI 3.1.1; processo iniciado para o teste foi encerrado |

O teste de Kestrel foi coordenado por Python `subprocess.Popen` e `urllib.request`, com espera limitada de até 10 segundos e encerramento em `finally`. Não persistiu dados nem manteve servidor ativo. Os comandos EF desta tabela foram executados a partir de `backend/`; os demais, da raiz.

## Cobertura dos 11 casos

- 1 teste das nove entidades, PKs, sete FKs obrigatórias com destinos exatos, NoAction e fotografia opcional.
- 1 teste de tipos SQL, limites de strings, datas, decimal, CHECK e conversões de enum.
- 3 casos de percentual válido (0, 50,25 e 100).
- 2 casos de percentual inválido (-0,01 e 100,01).
- 1 teste de migração única, modelo sem mudanças e geração SQL offline com nove tabelas de domínio, bytes reais e ausência de cascata ou DROP no avanço.
- 1 teste de inicialização Development, DI do DbContext e OpenAPI HTTP 200.
- 1 teste de rota desconhecida HTTP 404 / Problem Details.
- 1 teste de OpenAPI indisponível em Production (404).

## Pendências e limites da evidência

Conferência final após ajustes de nomes dos DbSets e textos: `dotnet build backend/ZelaSP.sln --no-restore` novamente com zero avisos/erros; `dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'` novamente com 11 aprovados e nenhuma falha; verificação EF sem mudanças pendentes. `git status --short --untracked-files=all` listou apenas 38 arquivos novos em `backend/`. `git diff --name-only` não listou modificações de arquivos preexistentes; `git diff --check` não reportou problemas nos arquivos rastreados. Busca textual por parâmetros `Password=`, `Pwd=` e `User Id=` nos arquivos de entrega não encontrou credenciais. Nenhum segredo foi incluído nas configurações; a conexão usa autenticação integrada.

Não foi executado `dotnet ef database update`, `EnsureCreated`, `Migrate`, rollback, SQL de criação ou exclusão, nem qualquer alteração a bancos/tabelas. A aplicação da migração, conectividade, permissões, integridade aplicada pelo servidor e persistência real continuam pendentes de autorização e teste exclusivo em `ZelaSP_Dev`. O teste do modelo e do SQL gerado não substitui essa validação.

Git utilizado somente para inspeção (`git status`, `git diff`); sem add, commit, push, merge, rebase, reset, checkout, switch, branch ou PR. O inventário completo de arquivos de entrega encontra-se em [ARQUIVOS.md](ARQUIVOS.md). Artefatos locais ignorados de build (`bin/`, `obj/`) não fazem parte da entrega. Revisão e aprovação final cabem ao PM/PO.

## Correções solicitadas pelo Tech Lead — 14/11/2026

Esta seção registra a validação mais recente, após corrigir o título da imagem para 100 caracteres e o relacionamento EVENTO_SEGURANCA 1 → 0..1 MENSAGEM_RISCO. A única migração inicial existente (`20261114191732_InitialCreate`) foi corrigida junto com designer e snapshot; nenhuma migração adicional foi criada.

| Comando / verificação | Resultado real |
|---|---|
| `dotnet restore backend/ZelaSP.sln` | Primeira tentativa bloqueada pelo acesso ao NuGet.Config do perfil; repetição com permissão concluída, todos os projetos atualizados |
| `dotnet build backend/ZelaSP.sln --no-restore` | Sucesso, zero avisos, zero erros |
| `dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'` | 13 aprovados, zero falhas, zero ignorados |
| `dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build` | Sem alterações pendentes entre modelo e migração |
| `dotnet ef migrations script --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build --idempotent --output Migrations/InitialCreate.sql` | Script idempotente atualizado offline; não executado no SQL Server |

Os comandos EF foram executados em `backend/`; restore/build/test, na raiz. Os dois novos testes verificam `Titulo` NOT NULL / máximo 100 / `nvarchar(100)` e a associação um para zero ou um: dependente obrigatório, principal sem dependente obrigatório, navegações de referência, índice único sem filtro e NoAction. O teste existente da migração também verifica título, FK obrigatória para evento existente e `CREATE UNIQUE INDEX` no SQL gerado.

Inspeção automatizada do arquivo `backend/Migrations/InitialCreate.sql` confirmou: `[titulo] nvarchar(100) NOT NULL`; ausência do título com 200 caracteres; `[evento_seguranca_id] int NOT NULL`; FK para EVENTO_SEGURANCA; índice único `IX_MENSAGEM_RISCO_evento_seguranca_id`; nove tabelas de domínio; ausência de `ON DELETE CASCADE` e de `DROP TABLE` no script de avanço. Confirmada uma única migração inicial. A unicidade e integridade foram verificadas no modelo e no DDL, sem inserções ou conexão ao servidor.

Nenhum pacote, projeto ou ferramenta foi adicionado ou teve versão alterada. A comparação SHA-256 dos arquivos antes/depois das correções confirma alterações somente em `backend/`, preservando os arquivos de dependências e todos os arquivos fora dessa pasta. O backend já estava não rastreado no Git ao iniciar esta revisão, por isso a comparação de conteúdo complementa `git status`.

Nenhum banco foi criado, excluído ou modificado. Não foi executado `dotnet ef database update`, rollback ou qualquer SQL. Permanecem pendentes a aplicação futura da migração em `ZelaSP_Dev`, mediante autorização, e a revisão final do Product Manager. A validação de entrada do título de 1 a 100 caracteres pertence ao futuro endpoint de upload e não foi antecipada. Nenhum comando Git de alteração foi executado.

## Aplicação controlada de InitialCreate — 14/11/2026

### Autorização e verificação anterior à escrita

O PM/PO autorizou exclusivamente aplicar a migração inicial existente no banco de desenvolvimento previamente criado. Nenhuma migração foi criada, removida, substituída ou regenerada. Foram preservados código, testes, projetos, configurações, script SQL, designer, snapshot, bin/ e obj/ (sem exclusão ou limpeza).

As verificações foram executadas por PowerShell / System.Data.SqlClient com autenticação integrada e consultas SELECT. Cada comando fora do sandbox foi apresentado integralmente e submetido à autorização individual do ambiente. No processo autorizado:

- `appsettings.Development.json`: `Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true`, sem credenciais.
- Ausentes as sobrescritas `ConnectionStrings__ZelaSP`, `SQLCONNSTR_ZelaSP`, `SQLAZURECONNSTR_ZelaSP` e `CUSTOMCONNSTR_ZelaSP`; ausente o arquivo user-secrets da API. A factory usa somente a variável `ConnectionStrings__ZelaSP` ou o fallback local aprovado; a ferramenta teve Infrastructure como startup, sem carregar a configuração de host da API.
- Destino efetivamente conectado: servidor `slayr\SQLEXPRESS`, instância `SQLEXPRESS`, banco `ZelaSP_Dev`, autenticação NTLM, estado ONLINE, `HAS_DBACCESS = 1`.
- Antes da migração: zero tabelas, zero objetos de usuário e ausência de `__EFMigrationsHistory`.
- Guarda de interrupção anterior à escrita para servidor/instância/banco divergente, banco não ONLINE, falta de acesso, autenticação não integrada ou qualquer tabela/objeto/histórico encontrado.
- Uma única migração `20261114191732_InitialCreate`; snapshot e modelo de destino do designer idênticos. Modelo atual sem diferenças pendentes, conferido pelo EF Core e pelos testes. Inspeção confirmou nove entidades, sete FKs, ausência de cascatas e comandos destrutivos no avanço, título 100, bytes reais, percentual 0–100 e mensagem de risco única e opcional por evento.

A consulta principal de preflight foi:

```sql
SELECT
    CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)) AS server_name,
    CAST(SERVERPROPERTY('InstanceName') AS nvarchar(128)) AS instance_name,
    DB_NAME() AS database_name,
    CONVERT(nvarchar(40), CONNECTIONPROPERTY('auth_scheme')) AS auth_scheme,
    (SELECT state_desc FROM sys.databases WHERE name = DB_NAME()) AS state_desc,
    HAS_DBACCESS(DB_NAME()) AS has_dbaccess,
    (SELECT COUNT_BIG(*) FROM sys.tables WHERE is_ms_shipped = 0) AS table_count,
    (SELECT COUNT_BIG(*) FROM sys.objects WHERE is_ms_shipped = 0) AS object_count,
    (SELECT COUNT_BIG(*) FROM sys.tables WHERE name = N'__EFMigrationsHistory') AS history_count;
```

### Comandos executados e resultados

Na raiz, antes e novamente após a migração:

```powershell
dotnet restore backend/ZelaSP.sln
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'
```

Resultados nas duas rodadas: restore concluído (projetos atualizados); build com zero avisos e zero erros; 13 testes aprovados, zero falhas e zero ignorados. O restore usou acesso autorizado ao NuGet.Config/caches do perfil. Os testes foram inspecionados e não executam conexão, inserção ou alteração SQL.

Em `backend/`, foram executados antes da escrita:

```powershell
dotnet ef dbcontext info --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

O contexto informou `Microsoft.EntityFrameworkCore.SqlServer`, banco `ZelaSP_Dev`, data source `.\SQLEXPRESS`. A conferência do modelo foi repetida no comando autorizado imediatamente antes de aplicar e após a migração; todas retornaram sem alterações pendentes.

Comando de aplicação efetivamente executado, em `backend/`:

```powershell
$approved = 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
$env:ConnectionStrings__ZelaSP = $approved
dotnet ef database update InitialCreate --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build --connection $approved
```

A variável foi definida apenas no processo de execução, após conferir as configurações existentes. O argumento explícito `--connection` fixou o destino da aplicação. A aplicação foi executada uma única vez. Saída real:

```text
Acquiring an exclusive lock for migration application.
Applying migration '20261114191732_InitialCreate'.
Done.
```

Não foi aplicado o script SQL manualmente. O EF Core gerenciou o bloqueio e a aplicação; não foi executado rollback ou reparo SQL.

### Conferência posterior no SQL Server

Conexão somente de leitura com `ApplicationIntent=ReadOnly` ao banco `ZelaSP_Dev`, precedida de SELECT que confirmou novamente servidor, instância e banco. A validação usou SELECTs em `sys.tables`, `sys.schemas`, `sys.columns`, `sys.types`, `sys.key_constraints`, `sys.foreign_keys`, `sys.foreign_key_columns`, `sys.indexes`, `sys.index_columns`, `sys.check_constraints` e `dbo.__EFMigrationsHistory`. Também foi executado `COUNT_BIG(*)` em cada uma das nove tabelas de domínio. Nenhum teste de inserção foi realizado.

| Item | Resultado confirmado |
|---|---|
| Destino/estado | `slayr\SQLEXPRESS` / `ZelaSP_Dev`, ONLINE |
| Tabelas de domínio | BAIRRO, CATEGORIA_OCORRENCIA, DISPOSITIVO, EVENTO_SEGURANCA, HISTORICO_STATUS, IMAGEM_OCORRENCIA, LEITURA_SENSOR, MENSAGEM_RISCO, OCORRENCIA, todas em dbo |
| Histórico | `dbo.__EFMigrationsHistory` com exatamente um registro: `20261114191732_InitialCreate`, ProductVersion `10.0.6` |
| Colunas | 50 no total, incluindo as duas do histórico; tipos físicos, comprimentos, precisão/escala aplicáveis, nulabilidade e identidades correspondentes ao DDL aprovado |
| PKs | Nove de domínio e uma do histórico, com nomes e colunas esperados |
| FKs | Sete obrigatórias, habilitadas e confiáveis, com nomes, tabelas e colunas esperados; atualização e exclusão NO_ACTION |
| Índices | Sete índices de FK além dos dez índices de PK; todos ativos, sem filtro |
| Mensagem de risco | FK NOT NULL para EVENTO_SEGURANCA e índice único `IX_MENSAGEM_RISCO_evento_seguranca_id`, sem filtro; evento pode existir sem mensagem, máximo uma mensagem por evento |
| Título da imagem | `nvarchar(100) NOT NULL`; `sys.columns.max_length = 200` representa bytes, equivalentes a 100 caracteres Unicode |
| Imagem | `varbinary(max) NOT NULL`, `max_length = -1` |
| Percentual | `decimal(5,2)` e CHECK inclusivo entre 0 e 100 |
| Enumerações | CHECKs de status da ocorrência/histórico e origem APP/SENSOR presentes |
| CHECK constraints | 14, todas habilitadas e confiáveis, incluindo IDs positivos |
| Dados de domínio | Zero registros em cada uma das nove tabelas |

Foram comparados automaticamente os metadados retornados com o script aprovado: conjunto/schema de tabelas, todas as colunas, PKs, FKs, índices e CHECKs. Nenhuma divergência. As expressões de CHECK foram comparadas considerando a normalização feita pelo SQL Server (IN convertido para OR). Como modelo, migração, designer, snapshot e DDL aprovado já estavam coerentes, a conferência também confirmou o schema aplicado em relação ao modelo EF Core, nos itens inspecionados. Não foi usado reverse engineering ou teste de escrita.

### Problemas, escopo e pendências

A primeira coleta JSON dos metadados usou `ExecuteScalar`, que recebeu apenas o primeiro bloco emitido por `FOR JSON`; o parser identificou resultado incompleto. Foi repetida somente a consulta de leitura, reunindo os blocos com `ExecuteReader` / StringBuilder; a coleta completa foi validada. A migração não falhou e não foi repetida.

Nenhum comando foi direcionado a outro banco de projeto. A única escrita autorizada foi a aplicação EF Core em `ZelaSP_Dev`, incluindo seu histórico. Nenhum dado de teste, configuração global, serviço, login ou permissão foi alterado. A identidade Windows tem privilégios amplos; futuras operações de escrita continuam exigindo conferência do destino e autorização própria. Não houve auditoria de alterações simultâneas por terceiros em outros bancos.

Somente `backend/README.md`, `backend/VALIDACAO.md` e `backend/prompts.md` foram editados. A comparação SHA-256 antes/depois preserva os demais arquivos de entrega, incluindo migração, snapshot, script SQL, código, testes e dependências, e todos os arquivos fora de backend/. Os diretórios bin/ e obj/ foram preservados e usados normalmente pelos comandos .NET, sem limpeza. Git somente de inspeção, sem add/commit/push/branches ou qualquer alteração de versionamento.

Pendente apenas a revisão final do Product Manager nesta etapa. Nenhuma TASK-002 iniciada e nenhuma autorização inferida para novas escritas.

## TASK-002 — API REST de ocorrências — 15/11/2026

### Inspeção e decisões aprovadas

Antes das edições foram inspecionados README raiz/backend, VALIDACAO, prompts, as nove entidades, enumerações, DbContext, mapeamentos/constraints, DI, configuração HTTP e os 13 testes existentes. `git status --short` estava limpo; `git log -1 --oneline` confirmou `1f081a6 Merge pull request #1 from dipaula-victo/feature/task-001-backend-foundation`. Não foram encontrados endpoints, contratos de negócio ou instruções AGENTS adicionais.

A implementação foi interrompida antes de modificar código porque o enum não definia regras de transição, e executor/origem do request não estavam especificados. O PM/Arquiteto confirmou: início ABERTA; somente ABERTA → EM_ANALISE → RESOLVIDA; 409 para repetição ou transição proibida; executor técnico configurado no servidor; origem APP controlada pelo servidor. Essas decisões foram implementadas sem alterar schema ou migração.

### Implementação e limites

Sete operações REST documentadas em [docs/API_CONTRACT.md](docs/API_CONTRACT.md), com DTOs, validações, JSON, 201/Location, paginação/filtros, UTC, AsNoTracking e Problem Details. Domain contém o fluxo aprovado; Application orquestra os casos de uso sem depender de EF; Infrastructure implementa o contrato concreto de persistência; Api recebe HTTP e configura DI/OpenAPI. A única nova referência entre camadas é Infrastructure → Application.

O PATCH usa transação explícita EF Core cobrindo UPDATE condicional por ID/status anterior e INSERT do histórico. Se o status anterior não corresponder, retorna conflito sem histórico adicional; falha na gravação do histórico descarta a transação sem commit. Validação de DTO e JSON rejeita campos controlados/extras e enumerações numéricas. A configuração de executor é validada no startup.

Dependências novas exclusivamente de teste: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.6 e `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5. O segundo fixa o bundle nativo para substituir a dependência transitiva vulnerável; não é runtime de produção. Não foram adicionados pacotes de mocking ou bibliotecas de validação/repositório.

### Comandos e resultados reais

Comandos de inspeção: `git status --short`, `git status --short --untracked-files=all`, `git log -1 --oneline`, `git diff --stat`, leituras via `Get-Content -Encoding UTF8`, buscas `rg` de arquivos/regras e comparação de hashes SHA-256 dos arquivos de entrega. Consultadas documentação oficial EF Core de transações/ExecuteUpdate e ASP.NET Core OpenAPI, além do aviso NuGet e releases oficiais do SQLitePCLRaw para resolver a vulnerabilidade transitiva.

| Comando | Resultado |
|---|---|
| `dotnet restore backend/ZelaSP.sln` (primeira tentativa) | NU1903 em SQLitePCLRaw.lib.e_sqlite3 2.1.11; não foi suprimido |
| `dotnet restore backend/ZelaSP.sln` (após fixar bundle 3.0.5) | Sucesso; quatro projetos atualizados e Tests restaurado, sem avisos de auditoria reportados |
| `dotnet build backend/ZelaSP.sln --no-restore` | Primeira tentativa também apontou nulabilidade de Headers no transformer; após inicialização explícita, build concluído com zero avisos/erros |
| `dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'` (primeira rodada nova) | 13 testes originais aprovados; 38 novos falharam na montagem da fixture, não por escrita SQL Server |
| `dotnet test backend/ZelaSP.sln --no-restore --filter 'FullyQualifiedName~T01_ListarBairros' --logger 'console;verbosity=minimal'` | Isolou erro SQLite próximo de max, após remover EventLog da configuração exclusiva dos testes |
| `dotnet test backend/ZelaSP.sln --no-restore --logger 'console;verbosity=minimal'` (fixture corrigida) | Inicialmente 51 aprovados; rodada final, após novos contratos/casos: 52 aprovados, zero falhas, um SQL Server ignorado (53 descobertos) |
| `dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build` em backend/ | Sem alterações no modelo desde a migração |

O restore foi apresentado integralmente e executado com autorização individual para ler NuGet.Config/caches do perfil. Houve uma tentativa de comando `dotnet ef` a partir da raiz com projetos prefixados por backend/, que não encontrou a ferramenta local; a repetição a partir de backend/ encontrou o manifesto e confirmou ausência de alterações pendentes. Nenhuma migração foi gerada, editada ou aplicada nesta tarefa.

Revisão final do OpenAPI identificou schemas de enums inicialmente publicados como inteiros e propriedades de entrada nullable, divergindo da validação HTTP. Foi configurada serialização numérica estrita e o transformer de schema passou a publicar enums textuais, IDs positivos e campos obrigatórios não nulos; os testes do contrato verificam essas propriedades e additionalProperties=false. Durante esse ajuste houve um erro de compilação de Minimum/Maximum, que na versão instalada de OpenAPI são strings; corrigido para a API real da biblioteca. A rodada final explícita de `dotnet build backend/ZelaSP.sln --no-restore` terminou com zero avisos/erros e, só após seu sucesso, `dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'` confirmou novamente 52 aprovados, um SQL Server ignorado e nenhuma falha.

Smoke test: `dotnet run --no-build --no-restore --project backend/src/ZelaSP.Api --urls http://127.0.0.1:5187`, com ASPNETCORE_ENVIRONMENT=Development. Processo iniciado/encerrado por Python, GET `/openapi/v1.json` retornou 200 (seis paths, sete operações) e POST `/api/ocorrencias` com `{}` retornou 400 no model binding, antes de acessar o banco. Nenhum POST/PATCH válido foi enviado à API conectada ao SQL Server.

### Evidência automatizada T01–T13

Os arquivos originais `ModelTests.cs` e `ApiTests.cs` foram preservados byte a byte. Seus 13 casos continuam aprovados. Os 39 novos casos executados usam host HTTP real de teste e SQLite relacional em memória, com uma conexão independente por caso, sem mocks de persistência:

| Requisito | Evidência |
|---|---|
| T01/T02 | Listagem de bairros/categorias, DTOs e referências vazias |
| T03 | POST válido, 201/Location, status/origem/datas do servidor e registro consultado no SQLite |
| T04/T05 | Bairro/categoria inexistentes rejeitados, sem ocorrência inserida |
| T06/T07 | Detalhe persistido e 404 em detalhe, PATCH e histórico inexistentes |
| T08 | Paginação, totais, filtros combinados e desempate por ID |
| T09/T11/T12 | Duas transições válidas, status final e dois históricos consistentes, cronologia/UTC/executor |
| T10 | Status inexistente, números, string numérica, null, ausência e executor do cliente rejeitados |
| T13 | Falha injetada depois de ExecuteUpdate e antes de inserir histórico; outro DbContext observa ABERTA e nenhum histórico |

Também foram exercitados todos os saltos/repetições/reaberturas proibidos, comparação com estado anterior desatualizado, descrição 4000/4001, campos extras, paginação inválida/overflow, enums de filtro, ausência de tracking e sete operações OpenAPI com exemplos e erros application/problem+json.

A fixture valida `IsSqlite()` antes de executar seu DDL temporário. SQLite não aceita a sintaxe SQL Server `varbinary(max)`: somente o DDL temporário gerado é adaptado para BLOB; o modelo de produção e a migração não são alterados. O provider EventLog dos testes foi substituído por console para que a limitação de permissão do sandbox não mascare a exceção original. Ambas as adaptações são exclusivamente da fixture e não validam particularidades do SQL Server. O bundle SQLite atualizado resolveu o restore sem desligar auditoria ou TreatWarningsAsErrors.

### Integração SQL Server: preparada e não executada

Um teste com trait `Category=SqlServerWrite` foi implementado e compilado. Sem a variável de autorização específica, xUnit o ignora antes de abrir conexão; a variável estava ausente durante a execução desta tarefa. Há guarda adicional no início do teste e destino SQL fixo. O teste futuro usa registros fictícios com GUID próprio, contextos distintos para comprovar persistência, UPDATE/histórico e falha de histórico com rollback; não altera referências preexistentes e mantém os registros criados sem limpeza automática.

Procedimento completo, autorização necessária e comando futuro: [docs/SQLSERVER_INTEGRATION.md](docs/SQLSERVER_INTEGRATION.md). A inspeção atual dos dados/destino e a autorização de inserts/updates devem anteceder sua execução. **Não foi aberto acesso de dados ao SQL Server nem executada escrita nele na TASK-002.** A evidência SQL da TASK-001 não foi reutilizada como prova dos novos casos de uso. O teste ignorado não é considerado aprovado.

### Arquivos desta tarefa

Criados:

- `backend/docs/API_CONTRACT.md`
- `backend/docs/SQLSERVER_INTEGRATION.md`
- `backend/src/ZelaSP.Domain/FluxoStatusOcorrencia.cs`
- `backend/src/ZelaSP.Application/Contratos.cs`
- `backend/src/ZelaSP.Application/ErrosAplicacao.cs`
- `backend/src/ZelaSP.Application/IOcorrenciasStore.cs`
- `backend/src/ZelaSP.Application/OcorrenciasService.cs`
- `backend/src/ZelaSP.Infrastructure/OcorrenciasStore.cs`
- `backend/src/ZelaSP.Api/Controllers/BairrosController.cs`
- `backend/src/ZelaSP.Api/Controllers/CategoriasOcorrenciaController.cs`
- `backend/src/ZelaSP.Api/Controllers/OcorrenciasController.cs`
- `backend/src/ZelaSP.Api/ContratoOpenApiTransformer.cs`
- `backend/tests/ZelaSP.Tests/OccurrenceApiFactory.cs`
- `backend/tests/ZelaSP.Tests/OccurrenceApiTests.cs`
- `backend/tests/ZelaSP.Tests/SqlServerWriteTests.cs`

Modificados:

- `backend/README.md`
- `backend/VALIDACAO.md`
- `backend/prompts.md`
- `backend/src/ZelaSP.Api/Program.cs`
- `backend/src/ZelaSP.Api/ApiExceptionHandler.cs`
- `backend/src/ZelaSP.Api/appsettings.json`
- `backend/src/ZelaSP.Infrastructure/DependencyInjection.cs`
- `backend/src/ZelaSP.Infrastructure/ZelaSP.Infrastructure.csproj`
- `backend/tests/ZelaSP.Tests/ZelaSP.Tests.csproj`

Sem alterações na InitialCreate, designer, snapshot, script SQL aprovado ou entidades/tipos de dados existentes. Nenhuma alteração fora de backend/, exclusão de bin/obj, comando Git de alteração, commit, branch ou PR.

A conferência SHA-256 confirmou exatamente os 24 arquivos listados nesta seção (15 novos e nove modificados), zero exclusões, nenhum arquivo de entrega alterado fora de backend/, todos os artefatos de migração preservados e os dois arquivos originais de testes preservados byte a byte. `git diff --check` não reportou problemas nos arquivos rastreados. Bin/obj foram utilizados normalmente pelo build, sem limpeza.

### Critérios e pendências

Endpoints, DTOs/validações, transação/histórico, testes locais, OpenAPI e documentação estão implementados para revisão. A prova específica de persistência e rollback no SQL Server permanece pendente de autorização/execução. Não há autenticação de usuários, PII/taint/LLM, upload, firmware ou telas Mobile. O contrato explica consumo Mobile, tratamento de conflito, referências vazias e futura integração de Cybersecurity. Não há seed automático e esta tarefa não provisiona dados de referência no banco real.

A API permite registrar e alterar ocorrências quando executada com acesso a um banco configurado; isso não foi exercitado com escrita no SQL Server nesta etapa. A identidade Windows local tem privilégios amplos. A aprovação final permanece com PM/Arquiteto; não iniciar TASK-003 automaticamente.

## TASK-002 — Validação real autorizada de persistência e rollback — 15/11/2026

### Autorização e escopo

O Product Manager autorizou expressamente executar somente o teste existente `ZelaSP.Tests.SqlServerWriteTests.PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao`, criando um bairro, uma categoria, uma ocorrência e um histórico confirmado, todos fictícios e exclusivos da execução, em `slayr\SQLEXPRESS` / `ZelaSP_Dev`. Também autorizou a tentativa de resolução com falha injetada e a permanência dos quatro registros. Não houve autorização de seed, concorrência simultânea, limpeza, novas migrações ou outras escritas.

O teste e o interceptor foram reinspecionados antes da execução. Seu código, os testes originais e todos os artefatos InitialCreate foram preservados. A variável de autorização estava ausente. O comando completo de preflight/execução foi apresentado previamente e aprovado individualmente pelo ambiente para execução fora do sandbox, com autenticação Windows integrada.

### Verificações anteriores à escrita

Comandos executados em `backend/`:

```powershell
dotnet build ZelaSP.sln --no-restore
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
dotnet test ZelaSP.sln --no-build --no-restore --list-tests --filter 'FullyQualifiedName=ZelaSP.Tests.SqlServerWriteTests.PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao'
```

Resultados: build com zero erros e avisos; nenhuma mudança pendente no modelo; listagem retornou exatamente o teste autorizado. `--list-tests` não executou testes ou escritas.

Imediatamente antes de habilitar o teste, consultas SELECT em conexão explícita para `.\SQLEXPRESS` / `ZelaSP_Dev` confirmaram:

- Servidor `slayr\SQLEXPRESS`, instância `SQLEXPRESS`, banco efetivo `ZelaSP_Dev`.
- Windows Integrated Authentication, mecanismo NTLM; banco ONLINE.
- Um registro de migração: `20261114191732_InitialCreate`, ProductVersion `10.0.6`.
- BAIRRO, CATEGORIA_OCORRENCIA, OCORRENCIA e HISTORICO_STATUS: zero registros cada.

O wrapper abortaria antes da escrita diante de qualquer divergência de identidade, estado, migração, versão, contagem ou variável de autorização preexistente. A conferência EF de mudanças pendentes foi repetida e passou dentro do mesmo comando autorizado. O teste também confirmou destino, migração e modelo antes de criar seus dados.

### Comando efetivamente executado

Após o preflight, a partir de `backend/`:

```powershell
try {
    $env:ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION = 'PM_PO_TASK002_APROVADO'
    dotnet test ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName=ZelaSP.Tests.SqlServerWriteTests.PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao' --logger 'console;verbosity=detailed'
    if ($LASTEXITCODE -ne 0) { throw 'Teste falhou; interromper sem reparo ou limpeza.' }
} finally {
    Remove-Item Env:ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION -ErrorAction SilentlyContinue
}
```

A variável existiu somente no processo do comando e foi removida no finally. Nenhuma suíte completa foi executada com escrita habilitada. **Execução única: um teste selecionado, um aprovado, zero falhas, zero ignorados; exit code 0.** Saída do teste:

```text
Execução fictícia identificada por: TASK002-FICTICIO-3cd4154b671244f3a466b2803962a9c4
Estado anterior: bairros=0, categorias=0, ocorrencias=0, historicos=0.
Registros fictícios mantidos, sem limpeza: bairroId=1, categoriaId=1, ocorrenciaId=1.
Histórico final: 1 alteração confirmada; segunda alteração revertida.
Total de testes: 1. Aprovados: 1.
```

### Leitura posterior independente e registros autorizados

Foi aberta nova conexão SqlClient somente de leitura ao banco aprovado, com nova guarda de servidor/instância/banco. Consultas parametrizadas por `@marker = N'TASK002-FICTICIO-3cd4154b671244f3a466b2803962a9c4'` leram apenas bairro/categoria com esse nome e ocorrência com essa descrição vinculada às duas referências fictícias. O histórico foi consultado somente por JOIN com essa ocorrência e suas referências. Além disso, executaram-se COUNT_BIG agregados nas quatro tabelas; nenhum conteúdo pessoal preexistente foi consultado.

| Registro confirmado | ID | Evidência |
|---|---:|---|
| BAIRRO | 1 | nome igual ao marcador; subprefeitura `Subprefeitura exclusivamente fictícia` |
| CATEGORIA_OCORRENCIA | 1 | nome igual ao mesmo marcador |
| OCORRENCIA | 1 | bairro_id=1, categoria_id=1, descrição igual ao marcador, origem APP, status EM_ANALISE |
| HISTORICO_STATUS | 1 | ocorrencia_id=1, ABERTA → EM_ANALISE, executor `integracao-task002` |

Horários armazenados pelo serviço em UTC: criada_em `2026-11-15T21:46:21.111`; alterado_em `2026-11-15T21:46:21.217`. O SQL Server armazena datetime2(3), sem informação de fuso; o código de origem usa UTC. A leitura posterior confirmou destino `slayr\SQLEXPRESS` / `ZelaSP_Dev`, NTLM e ONLINE.

| Tabela | Antes | Depois | Variação |
|---|---:|---:|---:|
| BAIRRO | 0 | 1 | +1 |
| CATEGORIA_OCORRENCIA | 0 | 1 | +1 |
| OCORRENCIA | 0 | 1 | +1 |
| HISTORICO_STATUS | 0 | 1 | +1 |

### Evidência de persistência, histórico e rollback

O teste criou dados via EF Core, encerrou os respectivos DbContexts e recuperou a ocorrência e seu histórico utilizando outro DbContext. A conexão posterior de leitura confirmou novamente os mesmos registros persistidos. Isso comprova persistência no SQL Server real; não foi usado SQLite ou mock nesta execução.

A primeira transição ABERTA → EM_ANALISE confirmou status e histórico na mesma transação. Na segunda transição, o serviço tentou EM_ANALISE → RESOLVIDA: o UPDATE condicional pertence à transação explícita e o interceptor lançou a falha esperada em SavingChangesAsync antes da inserção do histórico. A exceção foi esperada pelo teste; ao sair sem commit, a transação foi descartada. Outro DbContext verificou EM_ANALISE e um único histórico. A consulta posterior independente confirmou exatamente um histórico ABERTA → EM_ANALISE e **zero históricos com status_novo RESOLVIDA** para o marcador. Nenhuma atualização parcial permaneceu.

### Escopo, documentação e pendências

Não houve erro de execução ou necessidade de repetição, reparo ou limpeza. Os quatro registros fictícios permaneceram, conforme autorizado. Como as contagens iniciais das quatro tabelas eram zero, não existiam registros preexistentes nelas; o teste inseriu suas referências e atualizou somente a ocorrência recém-criada. Nenhum comando foi direcionado a outro banco de projeto. Não foram alterados migrações, schema, permissões, serviços ou configurações globais, nem inseridas referências adicionais. A autorização não foi usada para concorrência simultânea ou seed.

Somente `backend/VALIDACAO.md` e `backend/prompts.md` foram editados nesta etapa. Código do teste, implementação, dependências, documentos de contrato e artefatos InitialCreate foram preservados por comparação SHA-256. Bin/obj foram usados pelo build, sem exclusão ou limpeza. Git somente de inspeção; nenhum commit, branch ou PR.

Esta validação cobre os serviços/persistência e transação EF Core; não é um teste HTTP contra SQL Server nem um teste simultâneo de concorrência. A falha de histórico foi injetada pelo interceptor, não por interrupção de rede ou falha física do servidor. Essas são limitações da evidência, não resultados declarados como testados. A avaliação de concorrência simultânea e a proposta de referências de desenvolvimento continuam sem execução e fora desta autorização.

Pendente: revisão e aprovação final da TASK-002 pelo Product Manager/Arquiteto, inclusive decisão sobre quaisquer validações adicionais. Não houve autorização para commit ou TASK-003. Novas escritas exigem nova revisão; não repetir este teste com base nesta execução já consumada.

## TASK-002 — Validação final de concorrência SQLite — 15/11/2026

### Escopo e implementação

O PM autorizou implementar e executar um cenário de concorrência exclusivamente em SQLite temporário, proibindo escrita SQL Server e alterações em produção, entidades, contratos REST, InitialCreate ou snapshot. Antes de editar, foram reinspecionados OcorrenciasService, OcorrenciasStore, IOcorrenciasStore, testes de ocorrência, fixture SQLite, teste SQL Server opt-in e configuração de dependências. As alterações locais existentes da TASK-002 foram preservadas.

Criado somente `backend/tests/ZelaSP.Tests/OccurrenceConcurrencyTests.cs`. O teste `DuasOperacoesQueLeramAbertaConfirmamUmaTransicaoEUmHistorico` executa duas chamadas reais a OcorrenciasService.AtualizarStatusAsync para a mesma ocorrência, com destino EM_ANALISE. Ambas usam OcorrenciasStore e transação de produção, sem mocks, alteração do resultado do provider ou falhas artificialmente convertidas em conflito.

### Estratégia e evidência das duas leituras

- Banco relacional temporário em arquivo exclusivo, dentro da pasta de saída de testes de backend/, com GUID, WAL, cache privado, pooling desabilitado e foreign keys habilitadas.
- Uma ocorrência ABERTA e suas duas referências fictícias próprias são criadas no SQLite, sem SQL Server. O DDL temporário adapta apenas varbinary(max) para BLOB, como na fixture anterior; o modelo de produção é preservado.
- Conexões A e B e seus DbContexts são instâncias independentes, com asserções explícitas de não compartilhamento.
- Cada serviço usa um decorator exclusivamente de sincronização: ObterAsync delega ao store real e só aguarda a barreira depois da leitura completamente materializada, quando o reader já está descartado. O DTO real não é substituído.
- A barreira registra separadamente o DTO observado por A e B, exige ID correto e ABERTA em ambos e só libera quando as duas leituras terminaram. O teste não escolhe a operação vencedora nem espera um commit para iniciar a segunda chamada.
- Duas Task.Run permitem progresso independente, pois operações async do provider SQLite podem executar trabalho nativo síncrono. Após a barreira, cada chamada usa o UPDATE condicional e a transação reais do store.
- Um DbCommandInterceptor apenas observa os retornos reais de ExecuteNonQueryAsync, sem modificar comandos ou resultados. Asserções exigem exatamente dois UPDATEs, com quantidades de linhas afetadas iguais a 1 e 0.
- Uma terceira conexão/DbContext lê o estado confirmado e verifica uma única ocorrência em EM_ANALISE e um único histórico ABERTA → EM_ANALISE, pertencente à ocorrência e ao executor da operação vencedora. Também confirma uma única referência de cada tipo. Não há histórico duplicado nem alteração parcial.

Limites de espera: barreira de cinco segundos, timeout de comandos/conexões de cinco segundos, CancellationTokenSource de vinte segundos para o cenário e espera máxima de vinte e cinco segundos pelo par de operações. SQLITE_BUSY, SQLITE_LOCKED, cancelamento, timeout ou qualquer exceção diferente de ConflitoEstadoException fazem o teste falhar, nunca contam como conflito esperado.

### Comandos e resultados efetivos

Na raiz:

```powershell
dotnet restore backend/ZelaSP.sln
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName=ZelaSP.Tests.OccurrenceConcurrencyTests.DuasOperacoesQueLeramAbertaConfirmamUmaTransicaoEUmHistorico' --logger 'console;verbosity=detailed'
dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'
```

O restore foi apresentado e aprovado individualmente para acesso ao NuGet.Config/caches do perfil fora do sandbox. Nenhuma dependência foi adicionada ou alterada. Build executado antes do teste isolado e novamente após restore: zero avisos e zero erros em ambos. O teste isolado passou sem falha intermediária. A suíte completa foi executada com a variável de autorização SQL ausente: **53 aprovados, zero falhas, um SQL Server ignorado; 54 descobertos**. Os 52 casos locais anteriores, inclusive os 13 originais da TASK-001, continuaram aprovados. O teste SQL real aprovado na etapa anterior não foi reexecutado nesta etapa.

Trecho real de saída da primeira execução isolada:

```text
Conexões/DbContexts independentes: A e B. Barreira liberada somente após duas leituras reais ABERTA.
Operação A: leu ABERTA; confirmada; UPDATE afetou 1 linha.
Operação B: leu ABERTA; ConflitoEstadoException; UPDATE afetou 0 linhas.
Conexão independente de verificação: status=EM_ANALISE, históricos=1, ABERTA → EM_ANALISE, executor=concorrencia-A.
Nenhum histórico duplicado ou alteração parcial.
```

Foram executadas dez repetições adicionais, sempre com arquivo novo, por loop PowerShell que interromperia no primeiro exit code diferente de zero:

```powershell
$ErrorActionPreference = 'Stop'
for ($round = 1; $round -le 10; $round++) {
    $testOutput = & dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName=ZelaSP.Tests.OccurrenceConcurrencyTests.DuasOperacoesQueLeramAbertaConfirmamUmaTransicaoEUmHistorico' --logger 'console;verbosity=detailed' 2>&1
    if ($LASTEXITCODE -ne 0) {
        $testOutput
        throw "Falha na rodada $round; interromper sem mascarar resultado."
    }
    Write-Output "Rodada $round aprovada"
    $testOutput | Select-String 'Operação [AB]:' | ForEach-Object { $_.ToString() }
}
```

Dez de dez repetições aprovadas: A venceu nas rodadas 1, 3, 4, 5, 7, 9 e 10; B venceu nas rodadas 2, 6 e 8. Em todas ambas leram ABERTA, exatamente uma operação confirmou UPDATE de uma linha e a outra recebeu conflito após UPDATE de zero linhas. A variação de vencedor não é critério de aprovação; comprova que o teste não fixa uma ordem de sucesso. O invariante e o estado final são exigidos em todas as execuções.

Em backend/, também foi executado:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Resultado: nenhuma alteração no modelo desde a migração. Nenhum comando de migração de escrita foi executado.

### Limites do SQLite e recomendação SQL Server

O arquivo/WAL/cache privado permite conexões independentes sem recorrer às particularidades de shared cache em memória. SQLite serializa escritores: as duas chamadas se sobrepõem, mas não existem duas escritas efetivas simultâneas. A segunda transação pode aguardar o lock e depois avaliar o predicado com o status anterior desatualizado. Isso é o comportamento real do cenário e não um conflito fabricado. O provider pode repetir a espera por busy/locked até o timeout de comando; não foi implementada repetição manual da operação de negócio.

As leituras iniciais ocorrem fora da transação de escrita, exatamente como no código de produção. A barreira é posicionada após o fechamento dos readers para não criar artificialmente um cenário de upgrade de snapshot/transação que a aplicação não usa. O teste confirma a comparação otimista de estado e o histórico atômico sob o locking SQLite, sem prometer equivalência aos locks de linha/página, isolamento, deadlocks ou timeouts do SQL Server.

Para prova específica de concorrência no SGBD de produção, recomenda-se ainda um caso SQL Server com duas SqlConnections e DbContexts independentes, ocorrência fictícia própria, barreira após ambas as leituras ABERTA, duas chamadas reais e terceira conexão verificadora. Esperados uma confirmação, um conflito e um histórico; erros de deadlock/timeout devem ser relatados, não reinterpretados como sucesso. Esse caso não foi implementado/executado aqui e depende de autorização específica para escrita. O teste sequencial SQL Server da etapa anterior não comprova simultaneidade.

Referências consultadas: [erros/locking do Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors), [transações do provider](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions) e [transações SQLite](https://www.sqlite.org/lang_transaction.html).

### Preservação e revisão

Não foi identificada falha na implementação de produção nesse cenário e nenhuma alteração de produção foi necessária. Os arquivos SQLite/WAL/SHM/journal exclusivos de cada execução são removidos após descartar as conexões; a remoção de seu diretório é não recursiva. Não foi feita limpeza de bin/obj ou de arquivos preexistentes.

Arquivos desta etapa: um novo teste (`backend/tests/ZelaSP.Tests/OccurrenceConcurrencyTests.cs`) e atualizações em `backend/VALIDACAO.md` e `backend/prompts.md`. Código de produção, entidades, contratos REST, dependências, testes anteriores, teste SQL Server opt-in, InitialCreate, designer, snapshot e script SQL foram preservados por comparação de hashes. Nenhum arquivo de entrega fora de backend/ alterado e nenhuma operação Git de alteração. Nenhuma conexão de dados ou escrita SQL Server realizada nesta etapa; os registros fictícios anteriores não foram acessados ou modificados.

Pendente: revisão final pelo Product Manager e decisão sobre uma prova adicional de concorrência SQL Server. Não há autorização para commit, seed, nova escrita SQL Server ou TASK-003. A passagem local e as repetições não substituem essa decisão humana nem garantem ausência de toda condição de corrida possível.

## TASK-003 — Upload, persistência e recuperação de imagens — 16/11/2026

### Inspeção e preservação

Antes de desenvolver: inspeção de git status, git log, HEAD, instruções AGENTS, README/documentação técnica, ImagemOcorrencia, DbContext/mapeamentos, configuração, DI, tratamento de erros, OpenAPI e testes/fixture existentes. Working tree inicialmente limpo; HEAD já apontava para `feature/task-003-occurrence-images`, sobre `70809e5 Merge pull request #2`. Não foi executado comando Git de mudança de branch ou escrita. Não havia endpoint ou contrato de upload implementado. A entidade, limites e relacionamento existentes comportam os requisitos; nenhuma mudança de schema foi necessária.

### Resultado produzido

- POST /api/imagens recebe multipart/form-data com exatamente ocorrenciaId, titulo, executor e arquivo; responde 201 com DTO sem bytes e Location para GET /api/imagens/{id}.
- GET /api/imagens/{id} retorna metadados projetados, sem Dados; GET /api/imagens/{id}/conteudo retorna stream dos bytes originais recuperados do banco, com MIME PNG/JPEG e nosniff. Inexistentes retornam 404.
- Application contém DTOs/contrato concreto de persistência, EquipeAcademica, LimitesImagem e ImagensService. Infrastructure implementa consultas AsNoTracking e persistência assíncrona da entidade existente. Api contém controller, parser multipart limitado e transformer OpenAPI. Não há repositório genérico ou arquitetura paralela.
- DataUtc e RM do líder definidos pelo servidor. Fonte acadêmica única em appsettings.json, consumida por serviço/OpenAPI via DI: RmLider 554787 e os quatro identificadores fornecidos pelo PM. Executor é uma declaração do cliente, validada de forma exata, sem representar autenticação. Auditoria técnica de status permanece inalterada.
- Upload não modifica status/campos da ocorrência ou histórico, não grava em diretórios públicos, não usa nome como caminho e não registra bytes/dados acadêmicos em logs próprios. Persistência em Dados/varbinary(max), sem Base64 como armazenamento de produção.

### Validações e proteções de recursos

OcorrenciaId positivo/existente; título obrigatório de 1–100 após trim; executor obrigatório/exato; arquivo obrigatório/não vazio até 2.097.152 bytes inclusive. PNG exige assinatura de oito bytes; JPEG exige FF D8 FF. Extensão (.png/.jpg/.jpeg), MIME image/png ou image/jpeg e assinatura devem corresponder; formatos/combinações inválidos são rejeitados antes de inserir. O MIME persistido é canônico. Nome final é basename Windows/POSIX, trim/NFC, até 255, sem controles, formatação invisível, caracteres reservados ou nome-base vazio/início em ponto; apenas metadado.

O parser desabilita model binding de formulário e conta bytes reais sem confiar em Length/Content-Length. Arquivo e serviço usam cópia assíncrona limitada; a leitura para no máximo limite+1 bytes para detectar excesso. Também há limite de corpo completo (2 MiB + 64 KiB), inclusive epílogo/requests sem Content-Length; campos textuais até 4096 bytes, UTF-8 estrito; boundary até 70, 16 headers e 8192 bytes por parte. Campos extras/duplicados, incluindo rmLider do cliente, e multipart incompleto retornam 400. Excesso de arquivo/corpo retorna 413 Problem Details; internos recebem 500 genérico sem detalhes sensíveis. A API não depende de buffering em arquivo temporário de formulário.

Assinatura é identificação de formato, não validação completa de integridade, dimensões, decodificação, malware ou metadados. O teste de 2 MiB usa um payload sintético com prefixo PNG/padding para exercitar a fronteira, não comprovar integridade de um PNG grande. O limite é por requisição/arquivo, sem quota global de concorrência. Essas limitações estão explícitas no contrato; nenhum módulo de Cybersecurity ou autenticação foi antecipado.

### Comandos executados e resultados reais

| Comando | Resultado |
|---|---|
| `dotnet restore backend/ZelaSP.sln` | Sucesso; todos os projetos atualizados, nenhuma dependência nova |
| `dotnet build backend/ZelaSP.sln --no-restore` | Sucesso, zero avisos e zero erros |
| `dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName~ImagensApiTests' --logger 'console;verbosity=minimal'` | 48 aprovados |
| `dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName~Imagens' --logger 'console;verbosity=minimal'` | 57 novos casos aprovados |
| `dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'` | 110 aprovados, zero falhas, um SQL Server ignorado, 111 descobertos |
| `dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build` em backend/ | Nenhuma alteração no modelo desde a migração |

Restore apresentado e aprovado individualmente fora do sandbox somente para acessar NuGet.Config/caches do perfil. A variável de autorização de escrita SQL Server estava ausente antes da suíte e não foi habilitada. O teste SQL Server antigo foi ignorado, não reexecutado. Não houve falha intermediária de build/testes nesta tarefa; as proteções adicionais foram implementadas e verificadas incrementalmente.

Smoke test Kestrel: `dotnet run --no-build --no-restore --project backend/src/ZelaSP.Api --urls http://127.0.0.1:5187` em Development, coordenado por Python com encerramento em finally. GET /openapi/v1.json retornou 200 com nove paths/dez operações; POST /api/imagens sem boundary retornou 400 Problem Details e corpo acima de 2 MiB+64 KiB retornou 413 Problem Details. Ambos rejeitados antes de acessar dados. Nenhum upload válido foi enviado à API conectada ao SQL Server.

### Cobertura dos 57 novos casos

Os testes usam a fixture SQLite relacional temporária existente, sem modificá-la ou substituir persistência por mocks. Base64 aparece apenas nas constantes de fixtures (um pixel magenta PNG/JPEG gerado localmente em memória com System.Drawing durante a implementação); os testes não dependem de System.Drawing, downloads ou fotos reais para executar. As constantes são decodificadas para bytes, enviados por multipart e persistidos como bytes. Os arquivos originais de testes foram preservados.

| Grupo | Casos | Evidência |
|---|---:|---|
| Upload válido | 4 | PNG, JPG, JPEG, todos os quatro executores configurados, 201/Location, nove metadados, ausência de Dados/Base64 no JSON, MIME/bytes originais, DbContext independente, estado/histórico intactos |
| Entradas inválidas | 32 | Título, executor, IDs, inexistência da ocorrência, arquivo ausente/vazio/grande, GIF/SVG/PDF/ZIP, extensão/MIME/assinatura e divergências, nome, rmLider do cliente e partes extras/duplicadas; nenhuma imagem inserida e ocorrência/histórico preservados |
| Título após trim | 2 | Um e 100 caracteres aceitos com espaços externos |
| Fronteira 2 MiB | 1 | Tamanho exato aceito e bytes recuperados sem transformação |
| Nome seguro | 3 | Caminhos POSIX/Windows removidos, extensão maiúscula e Unicode NFC |
| IDs de leitura | 4 | Metadados/conteúdo inexistentes ou ID não positivo, Problem Details |
| Leituras sem tracking | 1 | Metadata/content via serviço em outro escopo sem entidades rastreadas |
| Falha interna | 1 | Interceptor falha antes da gravação da imagem; 500 genérico, sem mensagem interna ou imagem persistida |
| Cópia limitada/cancelamento | 2 | Stream sem Length/seek e sem EOF lido somente até 2 MiB+1; cancelamento respeitado |
| Multipart e corpo sem Length | 6 | Arquivo grande, epílogo grande após boundary, multipart incompleto, boundary ausente, UTF-8 inválido e campo textual excessivo; nenhuma gravação |
| OpenAPI | 1 | Multipart com quatro campos obrigatórios, equipe derivada da configuração, limite, Location/códigos, binary PNG/JPEG e metadados sem Dados |

A adaptação de varbinary(max) para BLOB continua restrita ao DDL da fixture SQLite já existente. Não é alteração no modelo ou prova do provider SQL Server. A recuperação usa nova requisição e contexto independente; o teste não compara somente o DTO devolvido pelo próprio upload.

### Inventário TASK-003

Criados:

- `backend/src/ZelaSP.Application/EquipeAcademica.cs`
- `backend/src/ZelaSP.Application/ContratosImagens.cs`
- `backend/src/ZelaSP.Application/LimitesImagem.cs`
- `backend/src/ZelaSP.Application/ImagensService.cs`
- `backend/src/ZelaSP.Infrastructure/ImagensStore.cs`
- `backend/src/ZelaSP.Api/LeitorUploadImagem.cs`
- `backend/src/ZelaSP.Api/Controllers/ImagensController.cs`
- `backend/src/ZelaSP.Api/ImagensOpenApiTransformer.cs`
- `backend/tests/ZelaSP.Tests/ImagemFixtures.cs`
- `backend/tests/ZelaSP.Tests/ImagensApiTests.cs`
- `backend/tests/ZelaSP.Tests/ImagensLimitsTests.cs`
- `backend/docs/IMAGENS_SQLSERVER_INTEGRATION.md`

Modificados:

- `backend/src/ZelaSP.Api/Program.cs`
- `backend/src/ZelaSP.Api/ApiExceptionHandler.cs`
- `backend/src/ZelaSP.Api/appsettings.json`
- `backend/src/ZelaSP.Infrastructure/DependencyInjection.cs`
- `backend/docs/API_CONTRACT.md`
- `backend/README.md`
- `backend/VALIDACAO.md`
- `backend/prompts.md`

### Escopo e pendências

Conferência de hashes preserva entidades/relacionamentos/DbContext, InitialCreate/designer/snapshot/script SQL, todos os arquivos de testes anteriores, contratos/casos de uso/controller de ocorrências, dependências e arquivos de entrega fora de backend/. Nenhuma migração criada/aplicada ou escrita SQL Server; nenhuma conexão de dados ao SQL Server nesta etapa. Os registros fictícios anteriores não foram acessados ou modificados. Nenhuma alteração Git, branch, commit, push ou PR, nem TASK-004. Bin/obj não foram excluídos ou limpos.

Contrato Mobile foi estendido apenas para imagens e esclarecimentos de escopo/histórico; o contrato REST de ocorrências foi preservado. README, VALIDACAO e prompts foram atualizados com resultados reais. O procedimento futuro SQL Server de imagens é somente documentação e exige autorização específica, conferência atual do destino/dados e escopo de registros fictícios próprios; não reutiliza autorização ou flag TASK-002.

Pendente: revisão final pelo PM/Arquiteto e eventual autorização/prova real de imagens em SQL Server. Não se afirma aprovação definitiva ou certificação independente dos requisitos acadêmicos R4/R5/R7; a implementação segue os requisitos descritos pelo PM nesta tarefa. Assinatura não comprova segurança completa; identificação acadêmica não autentica o executor. Não há LLM, login, Cybersecurity, seed, firmware ou telas Mobile.

Conferência final: executados novamente `dotnet build backend/ZelaSP.sln --no-restore` e `dotnet test backend/ZelaSP.sln --no-build --no-restore` (comando sem filtro/sem logger adicional solicitado pelo PM): zero avisos/erros no build e 110 aprovados/um SQL Server ignorado. A conferência EF permaneceu sem diferenças. SHA-256 confirmou exatamente os 20 arquivos do inventário (12 novos, oito modificados), zero exclusões, todos os arquivos anteriores de testes/dependências/Domain/DbContext/migrações preservados e nenhum arquivo de entrega alterado fora de backend/. `git diff --check` não reportou problemas.

Referência técnica consultada para parser/limites e nomes/assinaturas: [upload de arquivos no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0). A implementação não usa amostras dessa documentação como garantia de segurança completa de imagens.

## TASK-003 — Pré-validação final SQL Server somente leitura — 16/11/2026

### Consultas autorizadas e resultados

Reinspecionados ImagensStore, ImagensService, controller, configuração acadêmica, factory, fixtures e IMAGENS_SQLSERVER_INTEGRATION.md. Nenhuma edição de produção. Uma conexão SqlClient explicitamente direcionada a `.\SQLEXPRESS` / `ZelaSP_Dev`, Integrated Security=true, Encrypt=true, TrustServerCertificate=true, ApplicationIntent=ReadOnly, confirmou por SELECT:

| Item | Resultado |
|---|---|
| Servidor | slayr\SQLEXPRESS |
| Instância | SQLEXPRESS |
| Banco efetivo | ZelaSP_Dev |
| Autenticação | Windows integrada, NTLM |
| Estado/acesso | ONLINE, HAS_DBACCESS=1 |
| Migrações | Um registro: 20261114191732_InitialCreate / ProductVersion 10.0.6 |
| BAIRRO | 1 |
| CATEGORIA_OCORRENCIA | 1 |
| OCORRENCIA | 1 |
| IMAGEM_OCORRENCIA | 0 |
| HISTORICO_STATUS | 1 |

O comando completo foi apresentado e autorizado individualmente fora do sandbox. Incluiu uma guarda de servidor/instância/banco/autenticação e depois somente metadados de conexão, sys.databases filtrado pelo banco atual, o histórico EF autorizado e COUNT_BIG(*) das cinco tabelas. Não foram selecionados conteúdos pessoais, descrições, imagens, nomes ou valores de registros de domínio. Não houve qualquer instrução SQL de escrita ou acesso a outro banco de projeto. ApplicationIntent não foi tratado como barreira de permissão; a segurança desta execução veio do comando fixo, consultas somente leitura e destino conferido.

Em backend/, executado:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Resultado: sem alterações no modelo desde a migração. Isso compara modelo/snapshot, não é auditoria de toda alteração manual possível no schema SQL; o roteiro futuro inclui guarda do tipo físico Dados antes de escrever.

### Avaliação específica de tamanhoBytes

Adicionado um teste exclusivamente de diagnóstico/tradução offline: `ImagensSqlServerProjectionTests.ProjecaoRealDeMetadadosTraduzTamanhoNoSqlServerSemAbrirConexao`. Ele chama o próprio ImagensStore para capturar sua expressão real antes de executar, sem copiar a projeção manualmente; gera o SQL da fonte com ToQueryString e bloqueia qualquer abertura de conexão por interceptor. Vincula somente o ID de diagnóstico int.MaxValue, sem enumerar ou consultar linhas.

SQL relevante gerado pelo provider instalado 10.0.6:

```sql
SELECT [i].[id], [i].[ocorrencia_id], [i].[titulo], [i].[nome_arquivo], [i].[mime],
       CAST(DATALENGTH([i].[dados]) AS int), [i].[data_utc], [i].[rm_lider], [i].[executor]
FROM [IMAGEM_OCORRENCIA] AS [i]
WHERE [i].[id] = 2147483647
```

O teste confirmou CAST(DATALENGTH(... ) AS int), ausência de seleção do blob como conteúdo de metadados e zero tentativas de conexão. O ID não foi consultado no banco. A fonte é anterior ao terminal SingleOrDefault; a evidência se refere à projeção, não a uma execução real do detalhe.

Conclusão: tradução compatível com tamanhoBytes/int32 para o campo mapeado varbinary(max), com conversão explícita apropriada ao retorno DATALENGTH e ao limite de 2 MiB. Não houve necessidade de corrigir ImagensStore. Ainda não há prova de materialização de uma linha com imagem no SQL Server nesta etapa, pois a tabela tem zero registros e só foram autorizadas contagens.

Primeira execução do novo diagnóstico falhou por reutilizar um parâmetro id já extraído pelo EF sem sua bolsa de parâmetros; foi corrigido apenas o helper de diagnóstico, vinculando esse escalar a uma constante. A projeção de produção não foi alterada. A nova execução passou, com zero conexões. Essa falha intermediária não foi apresentada como erro do SQL Server ou da implementação de produção.

Comandos executados na raiz:

```powershell
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName=ZelaSP.Tests.ImagensSqlServerProjectionTests.ProjecaoRealDeMetadadosTraduzTamanhoNoSqlServerSemAbrirConexao' --logger 'console;verbosity=detailed'
dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'
```

Build sem avisos/erros; diagnóstico isolado um aprovado; suíte completa **111 aprovados, zero falhas e um SQL Server ignorado (112 descobertos)**. Os 110 casos anteriores continuam aprovados. A variável de autorização SQL antiga permaneceu ausente; nenhum teste de escrita SQL Server foi executado.

Referência técnica: [mapeamentos de funções binárias SQL Server/EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/functions). A documentação estabelece bytes.Length → DATALENGTH; a conversão para int foi conferida no SQL gerado localmente, não presumida apenas a partir da documentação.

### Fixtures e lote futuro

As constantes em ImagemFixtures.cs representam um pixel magenta de 1×1 gerado localmente por System.Drawing/Bitmap em MemoryStream na implementação. Origem já registrada na seção anterior; não são fotos reais ou downloads. Nesta inspeção os bytes foram apenas decodificados localmente para calcular comprimentos e hashes:

- PNG: 120 bytes; SHA-256 `a5c8f1a1213865e9ef89420f46e08085aa9e1c94a1723bf77218d44865f10575`.
- JPEG: 634 bytes; SHA-256 `9d8e6351c67193ebe9c11de358c8e54f127521aa511d6a1dc6003f64877e4860`.

Proposta para aprovação futura: exatamente um bairro e uma categoria com marcador TASK003-IMAGENS-GUID próprios; uma ocorrência nova via endpoint existente, ABERTA/APP; duas imagens PNG/JPEG pelos novos endpoints, vinculadas apenas a essa ocorrência. Total de cinco registros novos, nenhum UPDATE/DELETE ou alteração de registro anterior. Permanência deverá ser expressamente autorizada, sem limpeza automática. Para o baseline atual e sem terceiros, contagens finais esperadas: BAIRRO=2, CATEGORIA_OCORRENCIA=2, OCORRENCIA=2, IMAGEM_OCORRENCIA=2, HISTORICO_STATUS=1; o histórico da ocorrência nova deve ficar vazio. IDs não são presumidos.

### Roteiro preparado, não executado

IMAGENS_SQLSERVER_INTEGRATION.md foi ampliado com os comandos completos para revisão futura:

1. Preflight de identidade, autenticação, ONLINE, migração/modelo, cinco contagens e tipo físico Dados; abortar diante de divergência.
2. Lote SQL único, com guardas/placeholder que impede cópia acidental, para inserir só as duas referências próprias e retornar os IDs. Não é seed ou migração.
3. Startup de API revisada em porta exclusiva com conexão fixada por argumento de linha de comando, sem depender apenas de env vars. Confirmar ausência de processo/API divergente na porta e configuração acadêmica efetiva pelo OpenAPI antes de POST.
4. Preparar fixtures em memória da fonte única e conferir hashes. POST /api/ocorrencias apenas para a ocorrência própria. Ler snapshot da ocorrência e seu histórico antes dos uploads.
5. Dois POST /api/imagens com quatro campos, sem RM do líder/data do cliente. Exigir HTTP 201/Location com origem/path corretos, nove metadados, IDs/FKs, RM 554787, executor configurado, UTC e tamanho conhecido.
6. GET /api/imagens/{id}: HTTP 200 e metadados correspondentes; GET /conteudo: HTTP 200/MIME, comprimento e comparação byte a byte e por hash aos originais. Não imprimir binário em logs.
7. GETs posteriores da ocorrência/histórico idênticos aos snapshots. Conexão SQL independente, somente lendo IDs/marcador do lote e schema, para conferir varbinary(max), DATALENGTH 120/634, vínculo e metadados. Contagens agregadas finais/deltas esperados.

Os blocos PowerShell preparados foram analisados pelo parser PowerShell (três blocos com sintaxe válida), sem Invoke-Expression, execução de POST ou execução do SQL preparado. A primeira chamada auxiliar de verificação de sintaxe tinha uma interpolação de mensagem inválida (`$blockIndex:`); corrigida a mensagem para `${blockIndex}:`, a análise passou. Nenhum desses comandos de inspeção executou os blocos preparados. O SQL futuro foi revisado como texto, não executado/validado em runtime.

### Ressalvas e arquivos desta etapa

- Comando futuro contém INSERTs/POSTs explicitamente **não executados** e exige autorização nova; não habilita ou reutiliza teste/flag TASK-002.
- A identidade SQL tem privilégios amplos; guardas/escopo não equivalem a restrição de permissões. Contagens não auditam toda intervenção externa durante a execução.
- O lote não é uma transação única entre SQL de referências e múltiplos requests HTTP: falha pode deixar um subconjunto confirmado, a preservar sem limpeza. Timeout de POST não autoriza retry automático, pois pode ter ocorrido commit.
- A data do POST vem da entidade em memória; SQL armazena datetime2(3). O roteiro exige UTC e registra diferença máxima de 1 ms POST/GET pela precisão, e concordância GET/SQL na precisão armazenada. É risco/consideração de inspeção, não comportamento observado com imagem real agora. Não houve alteração de produção para isso; bytes serão comparados exatamente, sem tolerância.
- Assinatura não é integridade/antivírus e identificação acadêmica não é autenticação. A pré-validação não é prova de upload/persistência real de imagens.

Arquivos desta pré-validação: novo `backend/tests/ZelaSP.Tests/ImagensSqlServerProjectionTests.cs`; atualizados `backend/docs/IMAGENS_SQLSERVER_INTEGRATION.md`, `backend/VALIDACAO.md` e `backend/prompts.md`. Produção, contratos REST, fixtures anteriores, entidades, dependências e artefatos InitialCreate/snapshot não foram editados. Nenhum comando Git de alteração, banco criado ou modificado, migração aplicada ou TASK-004. Aguarda-se revisão do roteiro e autorização expressa para a execução futura.

## Encerramento TASK-003 — evidência real e conferência local — 16/11/2026

Execução única do bloco PowerShell integral revisado na conversa: **exit code 0**, sob autorização `PM_PO_TASK003_SQLSERVER_IMAGE_VALIDATION_APROVADO`, consumida exclusivamente pelo marcador `TASK003-IMAGENS-7c3fa9b2e1d44a8b9f2056c781a3d0e4`. Não repetir o lote. A autorização da TASK-002 não foi reutilizada.

Guardas confirmaram antes da escrita: servidor `slayr\SQLEXPRESS`, instância `SQLEXPRESS`, banco `ZelaSP_Dev`, Windows integrada, ONLINE, InitialCreate aplicada, modelo sem diferenças e marcador ainda não utilizado. Neste encerramento não houve novo acesso ao SQL Server.

| Tabela | Antes | Final | IDs criados |
|---|---:|---:|---|
| BAIRRO | 1 | 2 | 2 |
| CATEGORIA_OCORRENCIA | 1 | 2 | 2 |
| OCORRENCIA | 1 | 2 | 2 |
| IMAGEM_OCORRENCIA | 0 | 2 | PNG 1; JPEG 2 |
| HISTORICO_STATUS | 1 | 1 | Nenhum |

Os cinco registros fictícios permaneceram por autorização expressa. Referências e descrição da ocorrência usam o marcador; títulos são o marcador seguido de ` PNG` ou ` JPEG`. Arquivos: `task003-pixel-ficticio.png` e `task003-pixel-ficticio.jpeg`, pixels magenta fictícios 1×1 gerados localmente, sem fotos pessoais.

| Imagem | MIME | Bytes | SHA-256 original, recuperado e confirmado por SQL independente |
|---|---|---:|---|
| PNG / 1 | image/png | 120 | a5c8f1a1213865e9ef89420f46e08085aa9e1c94a1723bf77218d44865f10575 |
| JPEG / 2 | image/jpeg | 634 | 9d8e6351c67193ebe9c11de358c8e54f127521aa511d6a1dc6003f64877e4860 |

POST da ocorrência e uploads retornaram **201**. Location dos uploads: `/api/imagens/1` e `/api/imagens/2`. GETs de metadados, conteúdo, ocorrência e histórico retornaram **200**. Bytes recuperados integralmente, comparados byte a byte e por hash. SQL independente confirmou DATALENGTH, HASHBYTES, associação à ocorrência 2 e varbinary(max), comprovando também a materialização real de tamanhoBytes.

Metadados: RmLider `554787`, executor `VictorHugoDePaula_RM554787`, UTC do servidor e nomes/títulos/MIMEs/tamanhos corretos, sem Dados no JSON. Diferenças POST/GET de data: PNG 0,5856 ms e JPEG 0,2666 ms, compatíveis com datetime2(3). A ocorrência permaneceu **ABERTA/APP**, com snapshot preservado e histórico vazio. Nenhum histórico foi criado pelo lote; o histórico global anterior permaneceu intacto. Nenhum registro preexistente ou outro banco foi alterado pelo lote. Sem limpeza, reparação, alteração de schema ou migração.

### Conferência local final

Comandos executados na raiz, após confirmar ausente `ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION`:

```powershell
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore
```

Build: exit 0, zero avisos/erros. Testes: exit 0, **111 aprovados, zero falhas, um SQL Server ignorado, 112 totais**. Os 53 testes locais anteriores, incluindo os 13 da TASK-001, continuam passando. Restore não foi repetido nesta etapa.

A primeira verificação EF usou startup Api e falhou (exit 1) pela ausência de referência Design nesse projeto. Sem adicionar dependências, foi repetido o comando documentado em backend/:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Resultado: exit 0, `No changes have been made to the model since the last migration.` Não aplica migrações nem audita alterações manuais no schema.

Git somente leitura: `git branch --show-current`, `git status --short --untracked-files=all`, `git diff --stat`, `git diff --check`, `git diff --name-only`. Branch `feature/task-003-occurrence-images`. Inventário final: 21 arquivos (13 novos, oito modificados), todos backend/, nenhuma exclusão. O inventário histórico de 20 recebeu somente o diagnóstico adicional `tests/ZelaSP.Tests/ImagensSqlServerProjectionTests.cs`. InitialCreate, snapshot, entidades, projetos e dependências não constam das alterações.

Consolidados neste encerramento: README, VALIDACAO, prompts, API_CONTRACT e IMAGENS_SQLSERVER_INTEGRATION. Sem código novo, escrita SQL, repetição de uploads, teste SQL com escrita, alteração Git ou TASK-004. Pendente revisão final e autorização do PM para commit. Identificação acadêmica não autentica; assinatura não comprova integridade ou segurança completa do arquivo.

## TASK-004 — Motor PII e proteção de relatos — 17/11/2026

### Escopo, inspeção e implementação

Branch confirmada `feature/task-004-pii-protection`, working tree inicialmente limpo. Inspecionados serviços/stores/DTOs, pipeline HTTP, testes e mapeamento de EVENTO_SEGURANCA. Os campos permitem tipo_evento/tipo_dado até 80, origem/destino/acao até 120, UTC datetime2(3), sem CHECK de vocabulário. Não foi necessário alterar entidades, DbContext, InitialCreate, designer, snapshot ou DDL.

ProtecaoPii é um componente puro e determinístico em Application. Seu resultado contém somente texto sanitizado e categorias; não carrega valores originais, offsets ou trechos em DTOs de auditoria. CPF exige onze dígitos e dois verificadores corretos, excluindo sequências repetidas. Telefone exige DDD da lista brasileira e assinante convencional, com +55 opcional. E-mail reconhece formato ASCII convencional. Senha reconhece senha:, senha= e senha é; valores com espaços/delimitadores exigem aspas na mesma linha. Saúde reconhece seis termos aprovados com limites de palavra, caixa indiferente e variantes de acento. Limitações e formatos estão descritos no README.

Rótulos exatos: [CPF OCULTADO], [TELEFONE OCULTADO], [E-MAIL OCULTADO], [SENHA OCULTADA], [SAÚDE OCULTADA]. Precedência para sobreposições SENHA > EMAIL > CPF > TELEFONE > SAUDE; empate por maior comprimento e posição inicial. Cada trecho selecionado gera uma categoria; sobreposições subordinadas não geram duplicação; repetições separadas geram eventos individuais. Rótulos conhecidos são preservados, inclusive após senha:, sem novos eventos. Regex usam NonBacktracking, CultureInvariant e timeout de 250 ms; falha aborta o fluxo sem persistir o original.

O POST mantém validações anteriores e referências, protege após trim e rejeita texto protegido acima de 4000 caracteres com 400 antes da gravação. OcorrenciasStore.CriarAsync recebeu parâmetro interno opcional de eventos e executa um único SaveChangesAsync para ocorrência e todos os eventos; a transação automática do provider relacional garante tudo ou nada. Auditoria: PII_DETECTADO / categoria / API_OCORRENCIAS / BANCO / MASCARADO / mesmo instante UTC da criação. Nenhum valor original, hash, posição ou trecho pessoal é armazenado nos eventos. Sem detecção não há evento. O schema não contém FK evento→ocorrência; nenhum vínculo novo foi inventado. A atomicidade não implica correlação relacional individual.

Respostas de detalhe/listagem e do PATCH protegem também descrições legadas, sem regravar o banco ou auditar leituras. As regras e a transação de status/histórico não foram alteradas. DTOs públicos e códigos HTTP permanecem; erro automático de binding/validação passa a usar mensagem fixa em errors.request, evitando eco de nomes/valores do cliente. Logs de framework ASP.NET Core/EF são suprimidos e diagnósticos de exceções tratadas também; nenhum body logging ou logging sensível foi introduzido. Essa política reduz observabilidade e exige revisão para futura telemetria segura.

### Comandos e resultados realmente executados

Na raiz, antes das suítes, conferida a ausência de ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION; a variável nunca foi habilitada.

```powershell
dotnet restore backend/ZelaSP.sln
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName~ProtecaoPiiTests|FullyQualifiedName~PiiApiTests' --logger 'console;verbosity=minimal'
dotnet test backend/ZelaSP.sln --no-build --no-restore --logger 'console;verbosity=minimal'
dotnet test backend/ZelaSP.sln --no-build --no-restore
```

Restore: a primeira execução no sandbox falhou ao ler NuGet.Config do perfil. A repetição do mesmo comando fora do sandbox foi aprovada individualmente e terminou exit 0, todos os projetos atualizados. Nenhuma dependência adicionada ou alterada.

Builds incrementais: exit 0, zero avisos/erros. Primeira rodada isolada de 52 novos casos: 47 aprovados e cinco falhas nos testes. Quatro falhas eram desserialização dos enums textuais sem JsonStringEnumConverter no cliente de teste; corrigido somente o helper novo. A quinta usava uma sequência de dez dígitos como suposto exemplo inválido, mas ela atendia às regras de TELEFONE; substituída por exemplo de nove dígitos. Não se alterou a produção para fazer a expectativa incorreta passar. Após correções, suíte com 163 aprovados e um SQL ignorado. Acrescentadas provas de eventos por repetição e senha com rótulo parcial; última suíte: **165 aprovados, zero falhas, um SQL Server ignorado, 166 totais**, exit 0. O conjunto anterior de 111 casos continua aprovado.

Em backend/:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Executado com startup Infrastructure em ambas as conferências: exit 0, `No changes have been made to the model since the last migration.` Nenhuma migração aplicada e nenhuma conexão SQL Server aberta. Não houve execução do teste de escrita autorizado em tarefa anterior.

### Cobertura e prova transacional

54 novos casos automatizados: 43 unitários e 11 de integração HTTP/SQLite. Cobrem CPF válido/inválido, formatos, telefone/DDD, e-mail, senha e valores entre aspas, seis termos de saúde, caixa/acentos, texto sem PII, múltiplos tipos/repetições, prioridade de sobreposição, idempotência/rótulos, limites antes/depois da expansão, persistência protegida, respostas POST/GET/lista/PATCH, leitura legada sem regravação, auditoria segura, erros de binding sem eco e logs.

A prova de atomicidade usa a API e store reais, SQLite relacional em memória e um interceptor de comando que deixa executar o primeiro INSERT e lança uma falha antes do segundo. O teste exige dois INSERTs tentados e retorno do primeiro comando, HTTP 500 genérico e consulta por outro DbContext com zero ocorrências e zero eventos. Bairros/categorias fictícios de referência permanecem intactos. A mensagem injetada contém somente sentinelas fictícias, cuja ausência em resposta e logs capturados é verificada. Não há mock de persistência. Isso comprova rollback no provider SQLite; não é prova nova de execução SQL Server.

Os testes novos verificam UTC, ABERTA/APP, metadados técnicos fixos, ausência de histórico/mensagem de risco na criação e eventos não duplicados por leituras/PATCH. Há um evento por trecho efetivamente mascarado, não por cada candidato sobreposto.

### Inventário e regressão

Novos:
- src/ZelaSP.Application/ProtecaoPii.cs
- tests/ZelaSP.Tests/ProtecaoPiiTests.cs
- tests/ZelaSP.Tests/PiiApiTests.cs

Modificados:
- src/ZelaSP.Application/OcorrenciasService.cs
- src/ZelaSP.Application/IOcorrenciasStore.cs
- src/ZelaSP.Infrastructure/OcorrenciasStore.cs
- src/ZelaSP.Api/Program.cs
- src/ZelaSP.Api/ContratoOpenApiTransformer.cs
- tests/ZelaSP.Tests/OccurrenceConcurrencyTests.cs
- README.md
- docs/API_CONTRACT.md
- VALIDACAO.md
- prompts.md

A única mudança em teste anterior foi a assinatura/delegação de CriarAsync no decorator do teste de concorrência, para encaminhar o novo parâmetro interno opcional de eventos. Nenhum cenário ou assert anterior foi removido ou relaxado. Contratos REST não receberam novos campos. Nenhuma alteração em projetos/pacotes, entidades, fluxo de status, imagens ou migrações.

### Limites e pendências

Motor heurístico, sem detecção universal, análise semântica, autenticação, OCR, LLM, Taint Analysis completo ou painel. Identificadores numéricos podem parecer telefones; CPF inválido como CPF pode ser reconhecido como telefone válido. Negação/citação de termo médico também é mascarada. Senhas fora das expressões reconhecidas e formatos internacionais/ofuscados podem não ser detectados. Dados legados continuam armazenados como estavam; somente suas respostas são protegidas. Não foi feita limpeza, migração de dados ou prova SQL Server da TASK-004. Observabilidade dos frameworks está reduzida por proteção explícita. Nenhum arquivo fora de backend/ alterado, nenhum Git de alteração ou TASK-005. Aguarda Code Review do PM/Arquiteto antes de preparar commit.
## TASK-004 — Correção pós-Code Review: senha com múltiplas palavras — 17/11/2026

Causa: o ramo sem aspas da regex encerrava o valor no primeiro espaço. A expressão `senha: minha senha ficticia` deixava fragmentos após o primeiro token. A revisão autoriza exclusivamente corrigir esse limite; nenhuma alteração de persistência, auditoria, contrato, schema ou demais detectores foi feita nesta etapa.

A regex agora reconhece a expressão de introdução; um scanner limitado pelo tamanho do relato determina o trecho do valor. Sem aspas, inclui espaços e pontuação interna (ponto, !, ?, @), até vírgula, ponto e vírgula, CR/LF/NEL/LS/PS, próxima expressão reconhecida de senha ou fim do texto. Delimitadores e espaços finais são preservados. Em casos ambíguos, protege toda a narrativa até esse limite. Entre aspas simples/duplas, o fechamento na mesma linha continua delimitando o valor; pontuação interna é incluída e texto posterior é preservado. Marcadores dentro das aspas não são tratados como outra senha. Aspa não fechada usa o fallback conservador sem aspas.

Rótulos conhecidos isolados após a expressão permanecem protegidos e não geram nova detecção; texto seguinte é tratado como narrativa. Quando houver conteúdo adicional colado ao rótulo, o valor continua sujeito ao mascaramento. Sobreposição e precedência das cinco categorias permanecem; um CPF/e-mail dentro da senha é abrangido por um único evento SENHA. CPF, telefone, e-mail e saúde não tiveram suas regras modificadas.

Limitações: vírgula/ponto e vírgula são fronteiras explícitas para valores sem aspas; não há interpretação semântica para distinguir narrativa e senha. Senhas reais com esses caracteres precisam de aspas. Valores multilinha não são tratados como senha única; aspas escapadas não têm sintaxe especial. A proteção pode ocultar texto não sensível. Marcadores não previstos/ofuscados continuam fora do reconhecimento. Isso é uma política heurística documentada, não garantia universal.

Foram mantidos os 165 casos anteriores, com duas adaptações diretamente justificadas:
- A expectativa de `senha: SegredoFicticio! restante` agora mascara também `restante`, conforme a política conservadora.
- A amostra de múltiplas categorias recebeu ponto e vírgula após o valor de senha para continuar verificando saúde fora desse valor; mantém os mesmos asserts de sete detecções e cinco categorias, sem afrouxá-los.

Adicionados **22 casos**: 18 unitários e quatro HTTP/SQLite. Incluem senha simples, múltiplas palavras com os três marcadores, pontuação, CRLF/LF/Unicode, aspas, marcador dentro de aspas, CPF/e-mail no valor, rótulo existente, múltiplas senhas com/sem pontuação e aspa sem fechamento. Todos os casos unitários conferem também idempotência. Integração verifica igualdade exata do texto integralmente protegido no POST, GET individual, listagem e outro DbContext SQLite; também verifica eventos SENHA e ausência de histórico, sem alterar a persistência.

Comandos executados na raiz:

```powershell
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore
```

Resultados reais: build exit 0, zero avisos/erros; testes exit 0, **187 aprovados, zero falhas, um SQL Server ignorado, 188 totais**. A variável ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION estava ausente antes da suíte e não foi habilitada. Nenhum acesso SQL Server. Nenhum restore necessário ou executado nesta correção.

Em backend/:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Resultado exit 0: `No changes have been made to the model since the last migration.` Não houve falha intermediária de build/testes nesta correção.

Arquivos editados exclusivamente nesta revisão: src/ZelaSP.Application/ProtecaoPii.cs, tests/ZelaSP.Tests/ProtecaoPiiTests.cs, tests/ZelaSP.Tests/PiiApiTests.cs, README.md, VALIDACAO.md e prompts.md. As demais alterações da TASK-004 já estavam presentes e foram preservadas. Nenhum novo arquivo, pacote, migração, mudança em InitialCreate/snapshot, contrato REST ou persistência; nenhum comando Git de alteração, commit, push ou TASK-005. Aguarda nova revisão do PM.
## TASK-004 — Rótulos com sufixo em valores de senha — 17/11/2026

Branch conferida feature/task-004-pii-protection; alterações anteriores da task preservadas. Causa identificada: a verificação antecipada de rótulo aceitava o prefixo seguido de espaço e ignorava o restante do valor. Exemplo: senha: [CPF OCULTADO] SegredoFicticio123. Removida essa saída antecipada. Primeiro determina-se o limite completo do valor segundo a política aprovada; somente quando esse conteúdo inteiro corresponde a um rótulo conhecido ele é tratado como já mascarado. Prefixos seguidos de conteúdo adicional são protegidos como SENHA, sem alterações na persistência/auditoria.

A política de limites continua: sem aspas, vírgula, ponto e vírgula, quebra de linha, próximo marcador de senha ou fim; espaços, pontos, !, ? e @ são internos. Com aspas correspondentes, o valor termina na aspa de fechamento na mesma linha. A saída agora preserva as aspas ao redor do rótulo: senha: "[SENHA OCULTADA]" narrativa. Isso é necessário para que a segunda aplicação não interprete narrativa posterior às aspas originais como sufixo de uma senha sem aspas. Não há mudança de campos, tipos ou códigos REST; muda apenas a representação textual sanitizada, preservando seu delimitador.

Adicionados 16 casos: dez unitários (cinco rótulos com sufixo, cinco integralmente mascarados) e seis HTTP/SQLite. Testam formas com/sem aspas, texto separado por delimitador, repetição do sanitizador, POST, GET individual/lista, DbContext independente, ausência do segredo fictício na resposta/banco/eventos, zero eventos para valor já protegido e um evento SENHA para sufixo mascarado. Os testes anteriores foram mantidos, ajustando três amostras que tratavam texto após rótulo como fora do valor sem delimitador (uma expectativa passa a mascarar todo o trecho; duas amostras receberam ponto e vírgula) e sete expectativas de valores entre aspas para preservar essas aspas. Nenhuma assertion foi removida; os testes continuam exigindo idempotência e proteção integral.

A primeira rodada após remover o atalho teve 196 aprovados, cinco falhas e um SQL ignorado. As cinco falhas ocorreram na reaplicação de saídas de senhas entre aspas, pois o rótulo sem aspas perdia a fronteira original. A correção de produção passou a preservar as aspas, em vez de ignorar o problema ou relaxar idempotência. Adicionados também dois casos HTTP para os limites entre aspas.

Comandos realmente executados na raiz:

```powershell
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore
```

Resultado final: build exit 0, zero erros/avisos; testes exit 0, **203 aprovados, zero falhas, um SQL Server ignorado, 204 totais**. A variável ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION permaneceu ausente e o teste protegido não foi executado. Não houve restore ou acesso SQL Server.

Em backend/:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Resultado final exit 0: No changes have been made to the model since the last migration. Nenhuma alteração em entidades, schema, InitialCreate, snapshot, migrações ou dependências. Executado git diff --check após os documentos; sem problemas de whitespace. Git somente leitura.

Arquivos modificados nesta revisão: src/ZelaSP.Application/ProtecaoPii.cs, tests/ZelaSP.Tests/ProtecaoPiiTests.cs, tests/ZelaSP.Tests/PiiApiTests.cs, README.md, VALIDACAO.md, prompts.md. As demais alterações da TASK-004 precedem esta revisão e foram preservadas. Limitações heurísticas e de delimitadores continuam documentadas; texto adicional dentro do valor não é presumido público. Nenhum Mobile/outro diretório alterado, nenhuma escrita SQL, commit/push/pull/merge, dependência nova ou TASK-005. Aguarda revisão do Product Manager.
## TASK-005 — Indicador Risco Cidadão e adapter OpenAI — 17/11/2026

### Autorização, inspeção e escopo

Proposta aprovada pelo PM para feature/task-005-citizen-risk-ai. Branch conferida antes de editar; working tree inicialmente limpo. Inspecionados EVENTO_SEGURANCA, MENSAGEM_RISCO, Fluent API, InitialCreate/snapshot, arquitetura, contratos e documentação backend, tipos/parser/tela/alertas do Mobile. Não se alterou Mobile. Mensagem agregada não é persistida nem associada artificialmente a evento; FK obrigatória e única de MENSAGEM_RISCO preservada. Nenhum schema, migração, entidade existente ou dependência foi alterado.

Implementado GET /api/risco-cidadao, global e somente leitura. Janela de sete dias UTC com início/fim inclusivos e exclusão de futuros. Pesos 6/3/3/10/8 por CPF/TELEFONE/EMAIL/SENHA/SAUDE; bloqueio vale apenas 15, sem incrementar categoria. Eventos e categorias desconhecidos são ignorados, inclusive categoria desconhecida em VAZAMENTO_BLOQUEADO. Não há produtor de bloqueios. Pontuação satura em 100, com proteção de overflow, e classes BAIXO 0–19, MEDIO 20–49, ALTO 50–100. Pontos/classificação não dependem da IA.

Consulta EF agrupa no banco com AsNoTracking e projeta somente tipo/categoria/contagem; nunca carrega descrição, origem/destino/ação livres, IDs ou imagens. Vocabulário é exato: collation BIN2 e DATALENGTH no SQL Server impedem equivalências de caixa/acentos/padding; SQLite usa comparações/tamanhos correspondentes. Não há índice temporal novo ou alteração de collation do banco.

Resposta preserva pontuacao/nivel/periodoDias/deteccoes/vazamentosBloqueados/alertas, com textos fixos exatos do Mobile. Acrescenta janela e explicacao separadamente. Parser atual permite esses campos, mas a tela Mobile ainda não renderiza explicacao. Testes verificam formato e comparam alertas ao arquivo real Mobile; não foi executado aplicativo/parser TypeScript ponta a ponta.

### Adapter HTTP e barreiras

OpenAI Responses API com HttpClient por DI, endpoint fixo https://api.openai.com/v1/responses, modelo padrão gpt-4.1-mini configurável. Sem SDK/pacote novo; integração Enabled=false por padrão. API key ausente dos arquivos de configuração versionados; apenas sentinela explicitamente fictícia nos testes. Não foi solicitado, configurado, impresso ou utilizado segredo real.

O contrato do cliente recebe só ResumoRiscoAi com números e enum de nível. O payload é reconstruído por allowlist, com instructions constantes, input agregado, store=false, stream=false, max_output_tokens=300 e text.format json_schema estrito com uma propriedade texto. Nenhum relato (original ou mascarado), evento completo, metadado livre, executor/RM, nome, ID, imagem ou informação médica original é enviado. Referência oficial do protocolo consultada por leitura pública, com link em docs/TAINT_ANALYSIS.md; isso não é chamada de inferência.

Timeout de cinco segundos inclui headers/corpo; sem retries, redirects ou cookies. Limite de corpo 32 KiB, inclusive sem Content-Length, e explicação 1–600 caracteres em texto simples; envelope completed, sem erro/recusa e JSON somente texto. Saída com PII reconhecida/markup/links/controles ou formato inválido é descartada. Falha HTTP/rede, timeout, configuração ausente/inválida e resposta inválida retornam ausência de texto, e Application usa fallback determinístico. Cancelamento do cliente é propagado; falha de banco não vira risco zero. Loggers da factory HTTP removidos e categoria filtrada; nenhum prompt, header de autorização, corpo ou resposta é registrado pelo adapter.

### Comandos realmente executados

Na raiz, conferida ausência de ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION antes das suítes; nunca habilitada nesta task:

```powershell
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName~Risco'
dotnet test backend/tests/ZelaSP.Tests --no-restore --filter 'FullyQualifiedName~RiscoSqlServerProjectionTests'
dotnet test backend/ZelaSP.sln --no-build --no-restore
```

Builds incrementais todos com exit 0, zero erros/avisos. Primeiros 46 novos casos passaram. Diagnóstico offline adicional inicialmente falhou por parâmetro Functions já extraído pelo EF, ainda sem contexto de valores no helper; duas tentativas de diagnóstico identificaram esse parâmetro. Corrigido somente o helper para vincular EF.Functions como constante, sem copiar/alterar a query de produção para satisfazer o teste. Execução isolada passou com zero tentativas de conexão. Acrescentados três casos de configuração/stream/modelo.

Última suíte completa: **253 aprovados, zero falhas, um SQL Server ignorado, 254 totais**, exit 0. Os 203 anteriores foram preservados integralmente e continuam aprovados; nenhum teste anterior foi editado. São 50 novos casos: 16 de cálculo, 28 do adapter e validação, cinco HTTP/SQLite e um de tradução SQL Server offline. Timeout real local de cinco segundos e cancelamento foram exercitados com handler simulado.

Em backend/:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Exit 0: No changes have been made to the model since the last migration. Não houve aplicação/geração de migração. Restore não foi necessário/executado: projetos/dependências inalterados e caches existentes resolveram build/testes. Ferramentas usaram bin/obj sem exclusão ou limpeza.

Git somente leitura: branch --show-current, status --short, diff --stat, diff --name-only e diff --check. Conferência final de whitespace sem problemas; nenhum arquivo de Mobile, DbContext ou migrações apareceu nas alterações. Nenhuma operação Git de alteração, commit, push, pull, merge ou nova branch.

### Evidências e limites dos testes

- Pesos, soma/repetições, 0/19/20/49/50/100, saturação de long.MaxValue e rejeição de contagem negativa.
- SQLite real: limites inclusivos, futuros/antigos, vocabulário desconhecido/caixa/espaços finais, banco vazio e ausência de tracking.
- GET real via TestServer e adapter HTTP simulado: seis eventos válidos geram 45 pontos/MEDIO, todos os alertas Mobile e explicação LLM ou FALLBACK. Bloqueio de SENHA conta só 15. Um SELECT agrega eventos, sem leitura de ocorrência ou campos livres. Interceptor impede escrita; verificações independentes confirmam seis eventos preservados, uma ocorrência preservada e zero mensagens.
- Relato legado e campos de auditoria contêm sentinelas exclusivamente fictícias; o handler externo exige sua ausência e ausência de CPF/e-mail/senha/saúde fictícios, executor/RM/ID/metadados de imagem no request.
- Adapter: método/endpoint/schema/flags e campos exatos do agregado, configuração desabilitada/ausente/inválida, modelo configurável, HTTP 429, rede, JSON/estrutura inválidos, recusa/incomplete, campo pontuacao indevido, saída longa, PII nas cinco categorias, markup/URL, timeout e cancelamento. Stream sem tamanho é interrompido em 32769 bytes, sem carregar arquivo/corpo arbitrariamente grande.
- SQL Server: somente geração offline da expressão real do store com COUNT_BIG/GROUP BY/limites/collation/DATALENGTH, sem origens/destinos/ocorrências no SQL. Interceptor exige zero aberturas de conexão. Isso comprova tradução, não execução no SQL Server.

### Inventário TASK-005

Novos (11):
- src/ZelaSP.Domain/CalculadoraRiscoCidadao.cs
- src/ZelaSP.Application/ContratosRisco.cs
- src/ZelaSP.Application/RiscoCidadaoService.cs
- src/ZelaSP.Infrastructure/RiscoCidadaoStore.cs
- src/ZelaSP.Infrastructure/OpenAiExplicacaoCidadaClient.cs
- src/ZelaSP.Api/Controllers/RiscoCidadaoController.cs
- tests/ZelaSP.Tests/RiscoCidadaoTests.cs
- tests/ZelaSP.Tests/RiscoAiTests.cs
- tests/ZelaSP.Tests/RiscoApiTests.cs
- tests/ZelaSP.Tests/RiscoSqlServerProjectionTests.cs
- docs/TAINT_ANALYSIS.md

Modificados (8):
- src/ZelaSP.Infrastructure/DependencyInjection.cs
- src/ZelaSP.Api/Program.cs
- src/ZelaSP.Api/appsettings.json
- src/ZelaSP.Api/ContratoOpenApiTransformer.cs
- README.md
- docs/API_CONTRACT.md
- VALIDACAO.md
- prompts.md

Total 19 arquivos, todos backend/, sem exclusões. Nenhum arquivo de projeto/pacote, entidade existente, InitialCreate, snapshot ou script DDL alterado. Apenas uma classe de regras/contagens no Domain foi adicionada, sem integrar novas entidades ao modelo EF.

### Pendências para Code Review

Integração HTTP implementada; testes usam exclusivamente HttpMessageHandler simulado. **Nenhuma chamada real à OpenAI ou acesso SQL Server foi executado**. Habilitação remota com credencial real depende de nova autorização e revisão de configuração/termos do provedor. Não se apresenta simulação como prova de integração remota aceita. Não existe gravação em MENSAGEM_RISCO ou produtor de bloqueios. Nenhuma funcionalidade IoT ou TASK-006 iniciada.

Análise source/transform/sink, barreiras e riscos residuais em docs/TAINT_ANALYSIS.md. O detector de saída é heurístico e não valida toda semântica/PII inventada pelo modelo; a IA não altera o resultado autoritativo. Indicador é global sem autenticação, não mede perigo no bairro. Sem índice temporal, cache ou rate limit de inferências: volume/custo deverão ser avaliados antes de habilitação ampla. Mobile precisa de tarefa própria para exibir a nova explicação. Aguarda Code Review do PM/Arquiteto antes de commit.

## TASK-006 — Dispositivos e leituras de preenchimento — 18/11/2026

### Autorização e inspeção

Branch confirmada feature/task-006-iot-sensors, working tree inicialmente limpo. Proposta aprovada pelo PM: GET /api/dispositivos, POST /api/leituras, nível crítico >= 90.00m, data do servidor, sem ocorrências automáticas, autenticação ou idempotência. Inspecionados Domain, DbContext, InitialCreate, snapshot, serviços/stores, DTOs, erros, OpenAPI e documentação. Mobile possui identificação da origem SENSOR em ocorrências, mas não consumidor/tela de telemetria; nenhum arquivo Mobile foi alterado.

Schema existente reutilizado: DISPOSITIVO com bairro obrigatório/nome/local; LEITURA_SENSOR com FK obrigatória, decimal(5,2), CHECK inclusivo 0–100 e datetime2(3). Nenhuma entidade existente, DbContext, migração, snapshot, DDL ou índice foi alterado.

### Implementação

RegraNivelCritico no Domain é função pura `nivelPercentual >= 90.00m`. IoTService valida ID, percentual e precisão, confere existência e gera UTC via TimeProvider. Submilissegundos do relógio são truncados antes do SaveChanges para compatibilidade com datetime2(3); não se arredonda o percentual. Data significa recebimento/registro no backend, não medição física do ESP32.

GET usa AsNoTracking e uma projeção escalar com left join sobre subconsulta do último ID. Ordenação nome ASC/id ASC; leitura por lidaEm DESC/id DESC. Sem dispositivo: []; sem leitura: null. Uma consulta, sem N+1, sem entidades/navegações no JSON. Regra crítica aplicada ao DTO derivado, sem coluna nova ou dependência da IA.

POST JSON obrigatório com dispositivoId e nivelPercentual retorna 201 sem Location inventado. Cada POST válido insere uma leitura; não cria dispositivo nem altera outro registro. Campos extras/controlados/duplicados, tipos inválidos, strings numéricas, null, limites inválidos e precisão excessiva retornam 400; dispositivo ausente 404, corpo excessivo 413, Content-Type incompatível 415 e falha inesperada 500. Erros Problem Details e filtros de logging anteriores preservados; não há payload logging.

Filtro específico da action lê no máximo 1025 bytes para aceitar até 1024, inclusive sem Content-Length, antes do model binding. Verifica JSON e nomes duplicados sem distinção de caixa. A precisão numérica original, incluindo expoente e zeros finais, é conferida antes de decimal para evitar perda de dígitos da desserialização. O serviço também valida decimal antes da persistência. Exemplos: 90.0000 e 1e2 são válidos; 89.999, 90.001 e 0.90001e2 não são arredondados/gravados. Controles de ID/data/classificação são exclusivamente do servidor.

### Comandos e resultados reais

Na raiz:

```powershell
git branch --show-current
git status --short
dotnet build backend/ZelaSP.sln --no-restore
dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName~IoT'
dotnet test backend/tests/ZelaSP.Tests --no-restore --filter 'FullyQualifiedName~IoT'
dotnet test backend/tests/ZelaSP.Tests --no-restore --filter 'FullyQualifiedName~IoTApiTests.PayloadInvalidoRetorna400SemGravarOuEcoar'
dotnet test backend/ZelaSP.sln --no-build --no-restore
```

A variável ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION foi conferida ausente e nunca habilitada. Nenhum teste SQL Server com escrita executado; nenhum acesso ao SQL Server ou OpenAI nesta tarefa.

Resultados intermediários registrados com precisão:

1. Primeiros 56 casos novos: 45 aprovados, 11 falhas. Causa: a fixture SQLite existente gerava nivel_percentual como TEXT; CHECK 0–100 comparava como texto e rejeitava percentuais numéricos válidos. Ajustado somente o DDL temporário dessa coluna para NUMERIC, mantendo a CHECK ativa. A adaptação BLOB anterior permaneceu. Modelo de produção não alterado. Os 56 passaram após esse ajuste.
2. Acrescentadas provas de preservação das oito outras tabelas, CHECK numérica, falha de persistência e tradução SQL offline: 61 aprovados.
3. Acrescentados três casos de precisão; dois revelaram aceitação indevida de números com dezenas de dígitos após conversão decimal (89.999... e 90.000...001). Corrigida a inspeção do token original antes do model binding. Nenhum teste foi relaxado para aceitar arredondamento.
4. Houve um build intermediário com CS0029 por atribuir string a MultipleOf no schema OpenAPI. Corrigido para decimal 0.01m, conforme o tipo real da biblioteca instalada; sem pacote novo. Os 64 casos IoT passaram depois.

**Validação final:** build exit 0, zero avisos e zero erros; suíte completa exit 0, **317 aprovados, zero falhas, um SQL Server ignorado, 318 totais**. Os 253 testes anteriores continuam passando. Nenhuma assertion anterior foi removida/alterada. A única modificação em suporte anterior é a afinidade NUMERIC da coluna temporária SQLite, necessária para testar corretamente a constraint já existente.

Em backend/:

```powershell
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Exit 0: No changes have been made to the model since the last migration. Não houve geração/aplicação de migração. Restore não foi necessário/executado: nenhuma dependência/projeto alterado. Bin/obj utilizados normalmente sem limpeza.

Conferências Git somente leitura: diff --stat, diff --name-only e diff --check. Resultado final de whitespace sem problemas. Nenhum arquivo fora de backend/, em Mobile, DbContext, InitialCreate, snapshot ou script SQL consta das alterações; nenhum comando Git de alteração/commit/push/pull/merge foi executado.

### Cobertura dos 64 novos casos

- Cinco unitários da regra oficial: 0.00/89.99 false e 90.00/90.01/100.00 true.
- 58 HTTP/SQLite: GET vazio/ordenado, última data e desempate por ID, null sem leitura, uma consulta e ausência de tracking; oito POSTs numéricos válidos, igualdade da persistência por outro DbContext e resposta/GET; 33 casos de payload inválido; dispositivo inexistente; Content-Types incorretos; 1024/1025 bytes com e sem Content-Length; horário UTC/submilissegundos; schema OpenAPI/contrato ESP32 e ausência de Location; leituras críticas repetidas sem modificar dados anteriores; CHECK numérica ativa via SQL somente no SQLite temporário; falha injetada de SaveChanges com 500 genérico sem leitura persistida.
- Um diagnóstico SQL Server offline captura a expressão real do IoTStore e gera SQL com LEFT JOIN, TOP(1), ordenação por data/ID e sem APPLY. Interceptor exige zero tentativas de abrir conexão. Não é teste de execução no SQL Server.

Snapshot antes/depois de duas leituras críticas compara bairros, categorias, dispositivos, ocorrências, imagens, históricos, eventos e mensagens preexistentes, preservados integralmente. A leitura anterior também permanece com valor/data originais. Duas requisições válidas iguais geram IDs distintos, comprovando/documentando ausência de idempotência, sem criar ocorrências automáticas ou históricos artificiais.

### Inventário completo TASK-006

Novos (10):
- src/ZelaSP.Domain/RegraNivelCritico.cs
- src/ZelaSP.Application/ContratosIoT.cs
- src/ZelaSP.Application/IoTService.cs
- src/ZelaSP.Infrastructure/IoTStore.cs
- src/ZelaSP.Api/LimiteCorpoLeituraAttribute.cs
- src/ZelaSP.Api/Controllers/DispositivosController.cs
- src/ZelaSP.Api/Controllers/LeiturasController.cs
- tests/ZelaSP.Tests/IoTTests.cs
- tests/ZelaSP.Tests/IoTApiTests.cs
- tests/ZelaSP.Tests/IoTSqlServerProjectionTests.cs

Modificados (8):
- src/ZelaSP.Infrastructure/DependencyInjection.cs
- src/ZelaSP.Api/Program.cs
- src/ZelaSP.Api/ContratoOpenApiTransformer.cs
- tests/ZelaSP.Tests/OccurrenceApiFactory.cs
- README.md
- docs/API_CONTRACT.md
- VALIDACAO.md
- prompts.md

Total: 18 arquivos, todos backend/, sem exclusões. Interface IIoTStore permanece junto aos contratos em ContratosIoT.cs; nenhuma arquitetura paralela ou repositório genérico. Alterações em OpenAPI restringem Location ao recurso de ocorrência que possui GET individual; imagens mantêm seu transformer próprio. Tipos/limites obrigatórios IoT, multipleOf 0.01 e erros são documentados sem modificar contratos de runtime anteriores.

### Limites e pendências para Code Review

DispositivoId não autentica o ESP32. Sem credenciais/tokens, anti-replay, idempotência ou cadastro de dispositivos nesta task. Retries após timeout podem duplicar leituras; não há promessa de confirmação de um request pela coincidência com a última leitura. Não houve seed no banco real. Nome/local devem ser técnicos e não conter informações pessoais.

Testes usam somente dados fictícios no SQLite temporário e TestServer, além de tradução SQL offline. **Nenhum ESP32 físico, firmware ou escrita/leitura SQL Server foi executado.** Precisão/affinity SQLite não equivalem a validação física do provider SQL Server. Validação real de hardware/banco exige roteiro e autorização específicos. Não foram modificados status/PII/Risco Cidadão, Mobile, infraestrutura de rede/firewall ou outros integrantes. Sem IA na classificação, ocorrências automáticas, novos eventos, TASK-007 ou documentação acadêmica final. Aguarda Code Review e aprovação do PM antes de qualquer commit.