# ZelaSP — Backend / TASK-001

Fundação da API CitizenTech do ZelaSP (Global Solution FIAP 2026.2), compartilhada pelas disciplinas C# Software Development e SOA & Web Services. Esta entrega contém o modelo de persistência e a configuração HTTP; os casos de uso serão implementados em tarefas posteriores.

## Tecnologias e ambiente

- C# / .NET 10, ASP.NET Core Web API e injeção de dependência nativa.
- Entity Framework Core SQL Server e Design 10.0.6; ferramenta local `dotnet-ef` 10.0.6.
- Microsoft.AspNetCore.OpenApi 10.0.6: documento OpenAPI nativo, sem dependência de interface Swagger UI.
- Microsoft.OpenApi 2.7.5 e System.Security.Cryptography.Pkcs 9.0.21 fixados para corrigir vulnerabilidades das dependências transitivas identificadas no restore.
- Testes: Microsoft.NET.Test.Sdk 17.14.1, xUnit 2.9.3, runner Visual Studio 3.1.4 e Microsoft.AspNetCore.Mvc.Testing 10.0.6.

Requer SDK .NET 10 e acesso ao NuGet para restaurar os pacotes. Validação realizada no Windows com SDK 10.0.202 e runtime 10.0.6. SQL Server só é necessário para persistência real e aplicação da migração. Nenhum teste desta entrega abre conexão com o banco.

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
│   ├── ZelaSP.Application/      # ponto inicial para futuros serviços
│   ├── ZelaSP.Domain/           # nove entidades e duas enumerações
│   └── ZelaSP.Infrastructure/   # DbContext, Fluent API e migrações
└── tests/ZelaSP.Tests/          # testes de domínio, modelo e host HTTP
```

Referências: Api → Application e Infrastructure; Infrastructure → Domain; Application → Domain; Tests → Api (demais referências transitivas). Domain não depende de EF Core ou ASP.NET. Application contém apenas um marcador de assembly, pois não há casos de uso nesta tarefa. Não foram criados repositórios genéricos ou serviços sem utilização.

## Modelo de dados

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

Documento OpenAPI: `http://localhost:5187/openapi/v1.json`, apenas em Development. Não há interface Swagger UI nesta entrega. A rota raiz e rotas de negócio ainda retornam 404, com Problem Details. Controllers e DI estão preparados para os próximos endpoints. Exceções não tratadas recebem HTTP 500 padronizado sem mensagem interna no corpo. O handler usa operação assíncrona e os testes HTTP usam `async/await`; o DbContext oferece as operações assíncronas do EF Core para os futuros serviços. A inicialização não aplica migrações nem verifica conexão com SQL Server.

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
dotnet test ZelaSP.sln --no-restore
```

Os testes usam o modelo SQL Server real e o host ASP.NET em memória, sem providers alternativos e sem chamadas de conexão, `EnsureCreated`, `Migrate` ou `SaveChanges`. Cobrem nove entidades, sete FKs e seus destinos, exclusões, imagem opcional, tipos físicos, enumerações, limites numéricos, snapshot/migração, SQL gerado, DI, OpenAPI e respostas 404. A inicialização com Kestrel também foi verificada separadamente.

Resultados reais, falhas intermediárias e comandos estão em [VALIDACAO.md](VALIDACAO.md). O inventário completo está em [ARQUIVOS.md](ARQUIVOS.md); o uso de IA, em [prompts.md](prompts.md).

Correções do Tech Lead validadas em 14/11/2026: restore concluído, build com zero erros/avisos, 13 testes aprovados e nenhuma alteração pendente entre modelo e a única migração inicial. Os testes adicionais verificam título obrigatório com máximo de 100, cardinalidade 1 → 0..1, FK obrigatória e única, navegações e SQL gerado. Migração, designer, snapshot e script SQL foram atualizados sem aplicar a migração. Nenhuma dependência foi adicionada ou alterada.

Após a aplicação, consultas de leitura confirmaram nove tabelas de domínio e `dbo.__EFMigrationsHistory`, com um registro `20261114191732_InitialCreate` / `10.0.6`; nove PKs de domínio, sete FKs obrigatórias com NO_ACTION, sete índices de FK (um único) e 14 CHECK constraints habilitadas e confiáveis. Tipos, tamanhos, precisão, nulabilidade, identidades, chaves e índices foram comparados com os artefatos aprovados sem divergências. O título é `nvarchar(100)` (200 bytes em `sys.columns`); os bytes da imagem usam `varbinary(max)`. Todas as nove tabelas de domínio permanecem sem registros. Restore, build com zero avisos/erros, os 13 testes e a conferência de mudanças pendentes passaram novamente após a migração.

Pendente: revisão final por PM/PO. Conexão integrada, aplicação do schema e metadados SQL foram validados; inserções de domínio e fluxos de negócio não foram executados nesta etapa. A identidade Windows utilizada tem privilégios amplos, portanto futuras escritas exigem nova autorização e conferência do destino efetivo. Não há endpoints de negócio, upload, transações de status, autenticação de usuários da API, detector PII, taint analysis, LLM, dashboard, hardware, mobile, ML ou infraestrutura Linux.

Referências técnicas consultadas: [Fluent API e CHECK constraints](https://learn.microsoft.com/ef/core/modeling/indexes/), [exclusões e cascatas](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete), [migrações EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing), [correção OpenAPI](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc) e [correção Pkcs](https://github.com/advisories/GHSA-846f-f764-76mx).
