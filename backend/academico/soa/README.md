# ZelaSP — SOA e Web Services / Global Solution 2026.2

Turma **3ESPH**. CitizenTech para registrar e acompanhar problemas de zeladoria urbana, como acúmulo de resíduos e lixeiras cheias. A solução organiza relatos por bairro/categoria, imagens persistidas no banco, status e histórico transacional. O backend também contém proteção de PII, indicador de risco e telemetria, sem tornar essas extensões requisitos adicionais da entrega de SOA.

Entrega: [repositório GitHub](https://github.com/dipaula-victo/ZelaSP). **Documentação preparada localmente; a publicação final e o acesso do professor ainda precisam ser confirmados pelo responsável.** A configuração de `origin` não comprova publicação.

## Equipe

| Integrante | RM |
|---|---|
| Djalma Moreira de Andrade Filho | 555530 |
| Felipe Paes de Barros Muller Carioba | 558447 |
| Lucas Rodrigues | 556323 |
| Victor Hugo de Paula | 554787 |

O nome apresentado para Lucas preserva a identificação utilizada no relatório acadêmico de referência; esta tarefa não altera cadastro técnico ou configuração de equipe.

## Escopo oficial e navegação

Referência consultada: slides `GS CitizenTech - 3ES Fev - FIAP 2026 -.pdf`, página 27, fornecidos pelo responsável. Exigem código completo no GitHub, README, documentação dos endpoints e evidências. Não impõem o ZIP ou a numeração de evidências da disciplina C#.

- [Matriz dos cinco critérios](MATRIZ_RASTREABILIDADE.md).
- [Endpoints e OpenAPI](ENDPOINTS.md).
- [Evidências históricas e limites](EVIDENCIAS.md).
- [Pendências de publicação e conferência de segurança](PUBLICACAO_GITHUB.md).
- [Contrato detalhado compartilhado](../../docs/API_CONTRACT.md).

## Arquitetura e domínio

Na raiz do repositório, `backend/ZelaSP.sln` reúne quatro projetos:

| Camada | Responsabilidade |
|---|---|
| `backend/src/ZelaSP.Domain` | Entidades, enumerações, transições de status e regras determinísticas |
| `backend/src/ZelaSP.Application` | DTOs, validações e serviços de aplicação |
| `backend/src/ZelaSP.Infrastructure` | DbContext, mapeamentos, stores EF Core/LINQ e migrações |
| `backend/src/ZelaSP.Api` | Controllers, DI, Problem Details e OpenAPI |

As nove entidades são BAIRRO, CATEGORIA_OCORRENCIA, OCORRENCIA, IMAGEM_OCORRENCIA, HISTORICO_STATUS, DISPOSITIVO, LEITURA_SENSOR, EVENTO_SEGURANCA e MENSAGEM_RISCO. O DbContext define nove PKs, sete FKs, índices e CHECKs; não há exclusões em cascata. A associação evento/mensagem é 1 → 0..1 com FK única. Bytes de imagens usam `varbinary(max)` e não caminhos locais. Consulte o [DbContext](../../src/ZelaSP.Infrastructure/ZelaSpDbContext.cs).

Controllers delegam aos serviços; stores usam consultas assíncronas, projeções e `AsNoTracking` nas leituras. `OcorrenciasStore.AlterarStatusComHistoricoAsync` executa UPDATE condicional e INSERT do histórico na mesma transação; confirma com `CommitAsync`, e o descarte sem confirmação reverte a transação. DTOs evitam exposição direta de entidades EF.

## Ambiente e banco

Pré-requisitos: SDK .NET 10, NuGet acessível para restaurar dependências e SQL Server disponível para executar a API oficial. SDK historicamente utilizado: **10.0.202**; target `net10.0`; pacotes EF Core **10.0.6**. Testes utilizam xUnit 2.9.3, TestServer e SQLite relacional. Versões estão nos projetos e em `backend/Directory.Build.props`.

A configuração histórica de desenvolvimento é `.\SQLEXPRESS` / `ZelaSP_Dev`, com autenticação Windows integrada. Ela é uma referência da máquina de demonstração, não um destino universal. `AddInfrastructure()` usa `UseSqlServer`. Configure `ConnectionStrings__ZelaSP` apenas no processo local com um destino de desenvolvimento aprovado; não versione credenciais. Não apresente SQLite como provider oficial.

A API exige connection string configurada, mas seu startup **não** aplica migrations, executa seed ou cria tabelas. O banco e seu schema devem ser preparados previamente por um responsável autorizado. Não aplicar novamente a estrutura a um banco existente apenas para reproduzir a documentação.

## Migração existente

Há uma migração inicial, `20261114191732_InitialCreate`, acompanhada de designer/snapshot em [Migrations](../../src/ZelaSP.Infrastructure/Migrations). O [DDL idempotente](../../Migrations/InitialCreate.sql) e os procedimentos [SQL Server](../../docs/SQLSERVER_INTEGRATION.md) estão disponíveis para revisão. A aplicação histórica foi registrada em [VALIDACAO.md](../../VALIDACAO.md).

Nenhuma nova migração é necessária para esta entrega documental. Não executar `migrations add`, `database update`, DDL ou carga sem conferir o destino e obter autorização. A factory de design time lê a variável de ambiente, não user-secrets.

## Reprodução — comandos não executados neste fechamento

Todos os comandos abaixo partem da **raiz do repositório clonado**, onde existe `backend/`. Não utilizar a cópia acadêmica empacotada de C# como raiz do repositório.

```powershell
dotnet restore .\backend\ZelaSP.sln
dotnet build .\backend\ZelaSP.sln --configuration Debug --no-restore
```

Após configurar um SQL Server/schema de desenvolvimento aprovado e conferir a disponibilidade da porta:

```powershell
# Ajuste o destino ao seu ambiente. Não execute contra banco não aprovado.
$env:ConnectionStrings__ZelaSP = 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
$env:RiscoAi__Enabled = 'false'
dotnet run --project .\backend\src\ZelaSP.Api\ZelaSP.Api.csproj --configuration Debug --no-build --no-restore --no-launch-profile -- --environment Development --urls http://127.0.0.1:5187
```

O exemplo de confiança do certificado corresponde ao SQL Server local histórico; não é recomendação de configuração de produção. As variáveis acima são locais ao terminal; não utilizar `setx` ou mudar configurações globais. Encerre sua própria instância com Ctrl+C nesse terminal.

API: `http://127.0.0.1:5187`. OpenAPI: `http://127.0.0.1:5187/openapi/v1.json`, somente em Development. Não existe Swagger UI/Scalar instalado nem página inicial na raiz. O JSON é gerado automaticamente, complementado pelos transformers e atributos dos controllers; o [catálogo](ENDPOINTS.md) lista as **13 operações** atuais. Não confundir os dez endpoints de uma etapa histórica do README antigo com o estado atual.

A tool local EF está fixada em `backend/.config/dotnet-tools.json`. Para uma conferência de modelo futura, em terminal na pasta `backend/` e após build/tool restore autorizados:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build
```

Esses comandos não foram executados nesta preparação. A conferência de modelo não significa aplicação da migração.

## Testes

Para a seleção acadêmica isolada, após o build:

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-build --no-restore --filter "(FullyQualifiedName~ZelaSP.Tests.AcademicRollbackTests|FullyQualifiedName~ZelaSP.Tests.OccurrenceApiTests|FullyQualifiedName~ZelaSP.Tests.OccurrenceConcurrencyTests)&Category!=SqlServerWrite"
```

São as classes da seleção histórica de **42 aprovações**, não a suíte integral. `AcademicRollbackTests.cs` e o ajuste de `OccurrenceApiFactory.cs` precisam estar na referência final publicada; ver checklist. O build anterior à execução evita reutilizar binários desatualizados.

Para a suíte local mais ampla, **no repositório completo, incluindo Mobile**:

```powershell
dotnet test .\backend\ZelaSP.sln --configuration Debug --no-build --no-restore --filter "Category!=SqlServerWrite"
```

Parte de `RiscoApiTests` lê `mobile/src/services/privacy.ts` por caminho relativo. Essa dependência foi preservada, não enfraquecida. Manter a variável de autorização de escrita SQL ausente; o filtro exclui a categoria e o teste SQL possui proteção adicional. Não executar a suíte em ambiente com permissões de escrita habilitadas sem revisão. Novas execuções devem registrar seus próprios totais: 317 aprovações/um SQL ignorado são um histórico da TASK-006, não o resultado atual presumido.

## Limitações e estado de entrega

Esta preparação fez somente inspeções e edição documental. Não executou restore, build, testes, HTTP, SQL ou demonstrações. Evidências SQL Server, SQLite e adapter LLM simulado são identificadas separadamente. Não há autenticação de usuários/dispositivos; o código acadêmico não representa implantação de produção.

O backend original continua com RM técnico554787; a cópia C# teve correção própria para555530. Isso não foi propagado nesta task. A fotografia usada nas provas tem origem informada como Folha de S.Paulo, sem autoria/URL/licença comprovadas; não declarar licença livre. O ícone ODS11 ausente e aceito na entrega C# é uma ressalva daquela disciplina, não requisito adicional imposto a SOA.

As cinco capacidades funcionais estão implementadas e possuem evidências históricas. A entrega por link só fica efetiva depois de o responsável revisar/publicar os arquivos selecionados e confirmar o acesso do professor. Nenhum commit, push, publicação, ZIP ou envio ao Teams foi realizado aqui.
