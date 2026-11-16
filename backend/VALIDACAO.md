# Evidências de validação — TASK-001 e TASK-002

**Estado atual (15/11/2026): cenário concorrente SQLite aprovado, dez repetições adicionais aprovadas e suíte local com 53 aprovados/um SQL Server ignorado, sem escrita SQL Server nesta etapa.** Ver a última seção. A prova SQL Server sequencial de persistência/rollback foi aprovada na etapa anterior, sob autorização própria, e seus quatro registros fictícios foram mantidos. A TASK-001 e etapas anteriores da TASK-002 permanecem abaixo como histórico; restrições e resultados referem-se às respectivas autorizações.

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
