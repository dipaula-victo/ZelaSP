# Registro de uso de IA — TASK-001

Data: 14/11/2026 (America/Sao_Paulo).

Ferramenta: Codex, assistente de desenvolvimento com IA. Implementação e documentação geradas com assistência de IA, sujeitas à revisão humana do Product Manager e do Product Owner.

## Solicitação

Implementar exclusivamente em `backend/` a fundação C# / ASP.NET Core / EF Core / SQL Server do ZelaSP, com Api, Application, Domain, Infrastructure e Tests; nove entidades aprovadas e seus relacionamentos, migração inicial, DI, OpenAPI, tratamento básico de erros, testes e documentação. Preservar os demais arquivos e usar Git somente para inspeção, sem branches, commits ou PRs. Não desenvolver funcionalidades de tarefas posteriores.

## Esclarecimento do PO

O PO indicou a instância local SQLEXPRESS e o banco exclusivo `ZelaSP_Dev`. Autorizou apenas configuração sem credenciais, geração de artefatos, compilação, testes sem alteração do banco e documentação da aplicação futura. Proibiu expressamente `dotnet ef database update` e criação, exclusão ou modificação de bancos nesta etapa. Essa restrição foi respeitada.

## Resultado produzido

Solução .NET 10 com cinco projetos e dependências direcionais, nove entidades, duas enumerações, mapeamento Fluent API SQL Server, nove tabelas e sete FKs sem cascata, bytes de imagem em `varbinary(max)`, percentual com validação C# e CHECK SQL, migration/snapshot e script idempotente, factory de design time offline, configuração local integrada, OpenAPI Development e Problem Details. Application contém apenas o ponto inicial para futuros serviços. Sem endpoints de negócio ou integrações externas.

## Validação

Inspeção inicial do README, estrutura e Git confirmou ausência de backend e alterações locais. Dependências restauradas, compilação sem erros/avisos, 11 testes aprovados, snapshot sem alterações pendentes e script SQL gerado offline. API iniciada também no Kestrel, respondendo HTTP 200 com OpenAPI 3.1.1. Nenhuma migração aplicada e nenhuma conexão SQL validada.

A primeira restauração identificou vulnerabilidades transitivas; versões corrigidas foram fixadas sem suprimir avisos. A primeira rodada de testes teve duas falhas de configuração/inspeção dos testes, corrigidas antes da rodada aprovada. Acesso aos caches do SDK/NuGet exigiu permissão do ambiente. Ver detalhes e comandos em [VALIDACAO.md](VALIDACAO.md), e todos os arquivos entregues em [ARQUIVOS.md](ARQUIVOS.md).

## Limites

Não foram criados serviços artificiais, detectores de PII ou mecanismos de IA. Metadados de segurança têm contrato de não armazenar dados pessoais originais; a sanitização deverá ser implementada na tarefa de Cybersecurity. A aprovação técnica final permanece com PM/PO. Nenhum comando Git de alteração foi executado.

## Revisão solicitada pelo Tech Lead — 14/11/2026

Solicitação: corrigir exclusivamente o título de IMAGEM_OCORRENCIA para máximo de 100 caracteres e a cardinalidade de EVENTO_SEGURANCA para 1 → 0..1 MENSAGEM_RISCO, com FK obrigatória e única e sem cascata. Manter uma única migração inicial, atualizar artefatos, testes e documentação, sem alterações ao banco ou fora de `backend/`.

Resultado: limite corrigido em Fluent API, migração, designer, snapshot e SQL; navegação opcional `EventoSeguranca.MensagemRisco` adicionada, preservando a navegação obrigatória da mensagem para o evento; `WithOne`, FK obrigatória e índice único configurados. A migração inicial existente foi corrigida sem criar uma segunda migração. Nenhuma dependência foi adicionada ou alterada. Não foi implementada a validação de entrada do upload, reservada à tarefa futura.

Validação efetivamente executada: restore bem-sucedido após permissão de acesso ao NuGet.Config; build com zero erros/avisos; 13 testes aprovados; EF Core sem mudanças pendentes; script idempotente regenerado e inspecionado offline, com título `nvarchar(100)`, FK NOT NULL para evento existente, índice único e ausência de cascata. Comparação de hashes usada para verificar o escopo e a preservação das dependências. Os comandos e limites da evidência estão na seção de correções de [VALIDACAO.md](VALIDACAO.md).

Nenhuma migração aplicada, conexão SQL ou alteração de banco realizada; nenhum comando Git de alteração. A entrega aguarda revisão final do Product Manager.

## Aplicação controlada de InitialCreate — 14/11/2026

Solicitação do PM/PO: aplicar exclusivamente a migração inicial já aprovada em `slayr\SQLEXPRESS` / `ZelaSP_Dev`, após confirmar novamente identidade, estado ONLINE, banco vazio, ausência de histórico e conexão efetiva; validar o schema por leitura e repetir restore/build/13 testes, sem dados de teste, novas migrações, alterações arquiteturais ou acesso a outros bancos de projeto.

Uso de IA: Codex conduziu inspeção dos artefatos e configurações, elaborou os comandos com verificações de interrupção, executou a aplicação via EF Core após autorização individual do ambiente e comparou metadados reais SQL com o DDL aprovado. Comandos completos fora do sandbox foram apresentados antes da solicitação de execução. Não houve edição de código ou migrações.

Preflight aprovado: servidor/instância/banco corretos, Windows integrada (NTLM), ONLINE e acesso confirmado, zero tabelas e objetos de usuário e histórico ausente. Nenhuma sobrescrita de conexão ou arquivo user-secrets da API encontrado. A factory utiliza a variável de ambiente ou fallback aprovado. O comando fixou a conexão com `--connection`, além de definir a variável somente no processo, e executou uma única vez `dotnet ef database update InitialCreate --project src/ZelaSP.Infrastructure --startup-project src/ZelaSP.Infrastructure --no-build --connection $approved`, a partir de backend/.

Resultado real: migração `20261114191732_InitialCreate` aplicada com sucesso, registrada com ProductVersion `10.0.6`. Confirmadas nove tabelas de domínio e histórico, nove PKs de domínio, sete FKs obrigatórias NO_ACTION, sete índices de FK com unicidade da mensagem de risco, 14 CHECKs ativos/confiáveis, título `nvarchar(100)`, imagem `varbinary(max)`, percentual 0–100, enumerações e correspondência dos metadados com os artefatos aprovados. As nove tabelas de domínio permanecem com zero registros. Restore e build passaram antes e após a aplicação, com zero avisos/erros; 13 testes passaram em ambas as rodadas e o modelo continuou sem mudanças pendentes.

Problema de diagnóstico: a primeira coleta JSON foi incompleta por leitura de um único bloco; somente o SELECT foi repetido com leitura completa dos blocos. Não houve falha nem repetição da migração ou reparo do banco. Evidências, comandos e limites da verificação constam na última seção de [VALIDACAO.md](VALIDACAO.md).

Somente os três documentos autorizados (README, VALIDACAO e prompts) foram editados. Código, dependências e migrações preservados; nenhum outro banco de projeto ou configuração global foi alterado pelos comandos executados; nenhum dado de teste inserido e nenhum comando Git de alteração realizado. A identidade segue com privilégios amplos, sem modificação de permissões. A execução termina nesta etapa, aguardando revisão final do Product Manager.

## TASK-002 — API REST de ocorrências — 15/11/2026

Solicitação: implementar referências, cadastro, detalhe/listagem paginada e filtrada, PATCH de status transacional, histórico, DTOs, validação, erros e OpenAPI, preservando a arquitetura/InitialCreate e os 13 testes originais. Preparar teste de persistência SQL Server sem executá-lo ou gravar dados reais. Documentar contrato Mobile, evidências e pendências; somente backend/, sem operações Git de alteração.

Decisões do PM/Arquiteto obtidas antes de editar: início ABERTA; somente ABERTA → EM_ANALISE → RESOLVIDA, sem saltos/repetições/reabertura, conflitos 409; executor técnico de configuração do servidor; origem APP controlada pelo servidor. As enumerações não foram interpretadas como autorização para inventar outras transições.

Resultado produzido com assistência de IA (Codex): regra estática de domínio, DTOs e serviço de aplicação, contrato de persistência implementado com EF Core, três controllers, DI, tratamento de erros previsto/inesperado, JSON com enums textuais e campos extras rejeitados, OpenAPI com descrições/exemplos, contrato REST em docs/API_CONTRACT.md e procedimento em docs/SQLSERVER_INTEGRATION.md. O PATCH compara o estado anterior e mantém UPDATE/histórico em transação explícita, sem mudar o schema.

Validação efetiva: restore corrigido sem supressão de auditoria; build sem erros/avisos; 52 testes aprovados (13 originais intactos e 39 novos casos de API/persistência SQLite), um teste SQL Server ignorado por autorização ausente; EF Core sem mudanças pendentes. Kestrel serviu OpenAPI 200 e rejeitou POST vazio com 400 antes de acessar banco. Modelagem, migração, snapshot e script SQL preservados. Evidência detalhada e inventário completo em VALIDACAO.md.

Problemas resolvidos: dependência SQLite nativa transitiva vulnerável (fixado bundle 3.0.5 nos testes), nulabilidade de Headers do OpenAPI, parser SQLite incompatível com varbinary(max) (somente DDL da fixture adaptado para BLOB), EventLog indisponível no sandbox (somente logging de testes alterado) e comando EF executado inicialmente fora do diretório do manifesto (repetido corretamente em backend/). Falhas intermediárias foram registradas; o teste SQL Server ignorado não foi apresentado como aprovado.

A revisão do documento OpenAPI em execução também detectou enums publicados inicialmente como inteiros e nulls indevidos nos schemas de requests. O transformer de schema e as opções JSON foram ajustados e os testes do contrato reforçados. Um erro intermediário de tipo Minimum/Maximum da biblioteca foi corrigido; build e os 52 testes executados passaram novamente após o ajuste.

Dependências novas restritas a Tests: EF Core SQLite 10.0.6 e SQLitePCLRaw.bundle_e_sqlite3 3.0.5. Referência Infrastructure → Application adicionada para implementar o contrato da camada de aplicação; não há pacote de persistência em Application/Domain. Nenhum mock foi usado como prova de persistência.

Limites: nenhuma conexão de dados ou escrita no SQL Server foi executada nesta TASK-002, nenhum teste real opt-in foi habilitado, nenhuma migração foi criada/aplicada e não houve seed/limpeza de dados. A prova específica SQL Server requer nova autorização após conferir o estado atual. SQLite comprova comportamento relacional local, não as particularidades do SQL Server. Sem autenticação de usuários, Cybersecurity completo, IA em runtime, Mobile, IoT ou TASK-003. Somente arquivos de backend/ foram alterados; nenhum comando Git de alteração. Entrega sujeita à revisão final do PM/Arquiteto.

## TASK-002 — Execução autorizada de persistência/rollback SQL Server — 15/11/2026

Autorização recebida: executar exclusivamente o teste existente `SqlServerWriteTests.PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao`, em ZelaSP_Dev, criando um bairro, uma categoria, uma ocorrência e um histórico fictícios, além de tentar a resolução com falha injetada para verificar rollback. O PM autorizou expressamente manter os quatro registros; proibiu outras escritas, limpeza, migrações, concorrência simultânea e seed.

Codex reinspecionou teste/interceptor, compilou sem erros/avisos e conferiu ausência de mudanças no modelo. `dotnet test ... --list-tests --filter 'FullyQualifiedName=ZelaSP.Tests.SqlServerWriteTests.PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao'` listou somente esse teste. Antes da escrita, SELECTs confirmaram slayr\SQLEXPRESS, instância SQLEXPRESS, banco ZelaSP_Dev, NTLM, ONLINE, migração 20261114191732_InitialCreate / 10.0.6 e contagens zero nas quatro tabelas. Não houve divergências. O comando completo foi apresentado e autorizado individualmente para execução fora do sandbox.

Comando real, em backend/, com a variável de autorização habilitada somente no processo e removida em finally:

```powershell
dotnet test ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName=ZelaSP.Tests.SqlServerWriteTests.PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao' --logger 'console;verbosity=detailed'
```

Resultado: uma única execução, um teste aprovado, zero falhas/ignorados. Não foi executada a suíte completa com escrita habilitada. Marcador exclusivo `TASK002-FICTICIO-3cd4154b671244f3a466b2803962a9c4`. IDs confirmados por leitura posterior: bairro=1, categoria=1, ocorrência=1, histórico=1. BAIRRO, CATEGORIA_OCORRENCIA, OCORRENCIA e HISTORICO_STATUS passaram individualmente de 0 para 1.

Evidência real: ocorrência recuperada por outro DbContext; transição ABERTA → EM_ANALISE com histórico confirmado; tentativa EM_ANALISE → RESOLVIDA com falha injetada antes da gravação do histórico e descarte da transação sem commit. Outro DbContext e posterior conexão de leitura confirmaram EM_ANALISE, um histórico ABERTA → EM_ANALISE e nenhum histórico RESOLVIDA. As consultas posteriores de conteúdo foram parametrizadas e restritas ao marcador e suas referências; os demais acessos foram contagens agregadas, sem conteúdo pessoal.

Não houve erro, repetição do teste, reparo, limpeza ou modificação de registros preexistentes. Nenhum comando direcionado a outro banco de projeto, migração, alteração global ou operação Git de alteração. Os quatro registros fictícios permanecem autorizadamente. Somente VALIDACAO.md e prompts.md foram editados; código, teste, dependências e InitialCreate preservados.

Os resultados locais anteriores (52 casos) e esta execução SQL (um caso) ocorreram em rodadas separadas; não foram apresentados como uma suíte única executada nesta etapa. Persistência via serviços/EF e rollback foram verificados, sem teste HTTP real, concorrência simultânea ou simulação de falha de rede. Essas ampliações continuam fora do escopo. Entrega aguarda revisão final do Product Manager, sem autorização para commit ou TASK-003. Detalhes e evidências completos na última seção de VALIDACAO.md.

## TASK-002 — Concorrência SQLite autorizada — 15/11/2026

Solicitação: implementar e executar teste real de duas atualizações ABERTA → EM_ANALISE, com conexões/DbContexts SQLite independentes, ambas observando ABERTA antes de escrever, sincronização/timeout e prova de uma confirmação, um conflito e um histórico consistente. Não alterar produção sem diagnóstico prévio aprovado, não alterar contratos/entidades/migrações, não escrever no SQL Server, não realizar Git de alteração ou TASK-003.

Resultado produzido com assistência de IA: somente o arquivo novo `tests/ZelaSP.Tests/OccurrenceConcurrencyTests.cs`, mais esta documentação e VALIDACAO.md. SQLite usa arquivo temporário próprio sob a saída de testes de backend/, WAL, cache privado, pooling desativado e dados fictícios locais. Uma barreira de cinco segundos aguarda duas leituras reais completamente materializadas; o decorator apenas sincroniza e delega todos os resultados e escritas ao store de produção, sem mocks ou conflito pré-programado. Duas Task.Run executam serviços reais independentes. Interceptor somente observa quantidades de linhas dos UPDATEs, que devem ser 1 e 0. Terceira conexão valida o estado confirmado e executor da operação vencedora. Timeouts e cancelamento limitam a espera; erros de locking não são aceitos como conflito esperado.

Evidência: primeiro cenário isolado aprovado, A confirmou uma linha e B recebeu ConflitoEstadoException após zero linhas; ambas haviam lido ABERTA. Dez repetições adicionais aprovadas, com A vencedor em sete e B em três, sempre status final EM_ANALISE e exatamente um histórico ABERTA → EM_ANALISE. Restore passou sem dependências novas; build com zero erros/avisos; suíte completa com 53 aprovados, zero falhas e um SQL Server ignorado (54 descobertos). Os 52 casos locais anteriores, incluindo os 13 da TASK-001, continuam aprovados. EF Core confirmou modelo sem mudanças pendentes. Comandos e resultados completos em VALIDACAO.md.

Limites: SQLite admite somente um escritor por vez; as operações da aplicação se sobrepõem, mas a escrita efetiva é serializada pelo banco. O teste verifica a comparação de estado e atomicidade reais nesse mecanismo, sem afirmar equivalência ao locking SQL Server. Recomenda-se prova específica futura nesse SGBD, com autorização própria; não foi implementada ou executada nesta etapa. Não se confundiu o teste SQL Server sequencial anterior com teste simultâneo.

Nenhuma falha de produção foi identificada no cenário, e nenhum código de produção, dependência, contrato, entidade ou artefato de migração foi alterado. Somente arquivos temporários próprios foram removidos, sem limpar bin/obj. Nenhuma conexão de dados ou escrita SQL Server, nenhum acesso aos registros fictícios persistidos anteriormente, nenhuma operação Git de alteração ou arquivo fora de backend/. A entrega aguarda revisão do Product Manager, sem autorização para commit ou próxima tarefa.
