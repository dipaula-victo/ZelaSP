# Evidências de validação — TASK-001

**Estado atual (14/11/2026): InitialCreate aplicada e schema verificado em `slayr\SQLEXPRESS` / `ZelaSP_Dev`, com autorização específica do PM/PO.** As seções anteriores à última registram as etapas históricas e suas restrições vigentes na ocasião. A evidência atual está em “Aplicação controlada de InitialCreate”, ao final.

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
