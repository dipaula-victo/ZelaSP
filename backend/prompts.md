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

## TASK-003 — Imagens de ocorrências — 16/11/2026

Solicitação do PM/Arquiteto: implementar exatamente POST /api/imagens (multipart), GET /api/imagens/{id} (metadados) e GET /api/imagens/{id}/conteudo (binário), mantendo arquitetura, entidade/schema, migração e testes existentes. Validar associação, título após trim, executor oficial, arquivo PNG/JPEG até 2 MiB, extensão/MIME/assinatura, nome seguro e tamanho efetivo; definir RM do líder/data UTC no servidor. Sem autenticação, Cybersecurity, escrita SQL Server, Git de alteração ou TASK-004.

Codex inspecionou repositório/documentação/modelo/testes antes de editar. Branch já era feature/task-003-occurrence-images, sobre merge PR #2; working tree limpo. A entidade existente comportava os requisitos sem migração. Foram implementados contratos/serviço de aplicação, configuração acadêmica única via DI, parser multipart assíncrono com limites reais de bytes e sem buffering automático em arquivo, store EF Core assíncrono/AsNoTracking, controller e OpenAPI. RmLider 554787 e quatro executores vieram da lista fornecida pelo usuário, centralizada em appsettings; não foram duplicadas listas em componentes. Identificação acadêmica não foi confundida com autenticação.

Validação: restore concluído sem dependências novas; build com zero erros/avisos; 57 novos casos aprovados e suíte completa com 110 aprovados, zero falhas e um SQL Server ignorado (111 descobertos). Os 53 casos locais anteriores foram preservados integralmente. EF Core confirmou ausência de diferenças no modelo; InitialCreate, designer, snapshot, script e entidades preservados. Testes usam SQLite relacional real em memória, sem mocks de persistência, e fixtures próprias de pixel magenta PNG/JPEG geradas localmente durante a implementação, codificadas como constantes somente nos testes. Recuperação comparada byte a byte e persistência lida por outro DbContext.

Testes adicionais cobrem ausência de gravação em 32 cenários inválidos, tamanho exato/excessivo, stream sem Length/EOF, cancelamento, multipart malformado e sem Content-Length, epílogo excessivo, nomes Windows/POSIX/Unicode, equipe, RM controlado, MIME/assinaturas, erros genéricos, estado/histórico intactos e OpenAPI multipart/binário. A assinatura foi descrita apenas como reconhecimento de formato, sem garantir integridade ou segurança completa. O teste de fronteira utiliza payload sintético com padding; não foi apresentado como decodificação completa de imagem.

Smoke Kestrel: OpenAPI 200, multipart sem boundary 400 e corpo excessivo 413, antes de acesso a dados. Processo encerrado após teste. Nenhuma requisição válida de escrita foi enviada ao backend com SQL Server. Nenhuma falha intermediária foi ocultada; nesta tarefa os builds/testes executados passaram. A documentação registra comandos, inventário de 20 arquivos e limites da evidência em VALIDACAO.md.

Foram atualizados README, VALIDACAO, prompts e API_CONTRACT, e preparado documento específico IMAGENS_SQLSERVER_INTEGRATION.md, sem implementar/rodar teste real de imagens ou reutilizar autorização SQL da TASK-002. Nenhuma mudança fora de backend/, operação Git de alteração, migração, limpeza de bin/obj, acesso de dados ou escrita SQL Server. Conteúdo binário, conteúdo pessoal e dados acadêmicos não foram adicionados a logs próprios. A entrega aguarda revisão do PM/Arquiteto; prova específica de persistência SQL Server de imagens permanece pendente de autorização.

## TASK-003 — Pré-validação SQL Server somente leitura — 16/11/2026

Solicitação: confirmar exclusivamente slayr\SQLEXPRESS / ZelaSP_Dev, Windows integrada, ONLINE, migração/modelo e contagens de cinco tabelas; revisar lote fictício e origem das fixtures, avaliar tamanhoBytes no provider SQL Server e preparar roteiro exato HTTP/persistência, sem executar escrita ou ler conteúdo pessoal.

Codex reinspecionou procedimento, store/serviço/controller, configuração, factory e fixtures. Um comando completo de SELECTs foi apresentado e autorizado individualmente fora do sandbox. Resultado real: slayr\SQLEXPRESS, SQLEXPRESS, ZelaSP_Dev, NTLM, ONLINE, acesso confirmado, uma migração 20261114191732_InitialCreate / 10.0.6; contagens BAIRRO=1, CATEGORIA_OCORRENCIA=1, OCORRENCIA=1, IMAGEM_OCORRENCIA=0, HISTORICO_STATUS=1. Não foram selecionados conteúdos de registros de domínio. EF Core confirmou nenhuma mudança no modelo.

Foi adicionado somente um diagnóstico de teste offline, ImagensSqlServerProjectionTests.cs. Ele captura a expressão do store real antes de execução e gera SQL com ToQueryString, com guardas que impedem abrir conexão. A primeira tentativa falhou por parâmetro EF extraído sem contexto de valores; corrigido apenas o helper escalar id. A execução aprovada confirmou `CAST(DATALENGTH([i].[dados]) AS int)` e zero conexões, sem copiar a projeção de produção ou enumerar registros. Isso confirma compatibilidade de tradução/int32, não materialização real com imagem no SQL Server, que permanece pendente. Build sem erros/avisos; suíte com 111 aprovados e um SQL Server ignorado; nenhum teste com escrita real habilitado.

As fixtures locais documentadas (pixel magenta 1×1 gerado por System.Drawing em memória) foram identificadas por tamanho/hash: PNG 120 bytes e JPEG 634 bytes. Nenhuma imagem real de usuário foi usada. Os hashes completos e fonte única constam no procedimento e em VALIDACAO.md.

IMAGENS_SQLSERVER_INTEGRATION.md foi ampliado com scripts SQL/PowerShell para futura autorização, **não executados**: guardas do destino/contagens/schema, duas referências novas, uma ocorrência própria, dois uploads, 201/Location, GETs, MIME, comparação byte a byte/hash, metadados acadêmicos, DataUtc, snapshots de ocorrência/histórico, leitura SQL independente e deltas finais esperados 2/2/2/2/1. Identificou-se a necessidade de considerar precisão datetime2(3) ao comparar datas; o roteiro registra essa quantização sem relaxar igualdade binária. Não foi feita correção de produção.

A sintaxe dos três blocos PowerShell do documento foi analisada sem executá-los; uma mensagem auxiliar com variável seguida de ':' foi corrigida antes do parse aprovado. Os comandos futuros não constituem autorização: lote de cinco registros próprios e sua permanência devem ser aprovados expressamente, sem retries/limpeza ou uso da autorização anterior da TASK-002. Falha entre passos pode manter lote parcial; preservar e relatar.

Somente o novo teste offline, o procedimento, VALIDACAO e prompts foram alterados nesta pré-validação. Nenhuma mudança em produção/contratos/entidades/fixtures/migrações, nenhuma escrita SQL Server, consulta a dados pessoais, operação Git de alteração ou TASK-004. A execução de persistência real de imagens continua aguardando autorização do PM; a pré-validação não foi declarada como prova dessa execução.

## TASK-003 — Encerramento técnico — 16/11/2026

Solicitação: consolidar com assistência de IA a evidência real aprovada pelo PM, sem repetir escritas ou uploads. Documentação reconciliada em README, VALIDACAO, API_CONTRACT e IMAGENS_SQLSERVER_INTEGRATION; seções anteriores preservadas como histórico.

Execução única anterior: autorização PM_PO_TASK003_SQLSERVER_IMAGE_VALIDATION_APROVADO, marcador TASK003-IMAGENS-7c3fa9b2e1d44a8b9f2056c781a3d0e4, exit 0 em slayr\SQLEXPRESS / ZelaSP_Dev. IDs bairro/categoria/ocorrência 2, PNG 1 e JPEG 2. Uploads 201, GETs 200, bytes e hashes confirmados independentemente no SQL. Ocorrência ABERTA/APP, zero históricos próprios; cinco fictícios mantidos. Contagens finais 2/2/2/2/1; comprimentos e hashes completos em VALIDACAO.md. Autorização consumida, sem reutilizar a da TASK-002.

Validação local agora: build sem avisos/erros; 111 testes aprovados, um SQL Server ignorado, zero falhas. Gate SQL ausente. Primeira consulta EF com startup Api falhou por Design; repetida com startup Infrastructure conforme documentação, sem dependências novas, confirmou modelo sem diferenças. Git somente leitura; 21 arquivos (13 novos/oito modificados), todos backend/. Nenhuma nova escrita SQL, alteração de migração/schema, commit ou TASK-004. Aguarda revisão final e autorização para commit.
## TASK-004 — Motor PII — 17/11/2026

Solicitação: implementar proteção de CPF/TELEFONE/EMAIL/SENHA/SAUDE em relatos, mascaramento antes da persistência/HTTP, eventos técnicos e atomicidade, preservando arquitetura, schema e contratos REST. Sem SQL Server, serviços externos, LLM, autenticação ou Taint Analysis completo.

Resultado com assistência de IA: motor puro ProtecaoPii em Application, integrado à criação e respostas de ocorrências; store grava ocorrência sanitizada e eventos em um único SaveChanges relacional. Categorias e rótulos aprovados, CPF com verificadores, formatos convencionais e limitações documentadas. Precedência determinística e proteção de rótulos existentes. Eventos contêm somente categoria/classificações constantes/UTC. Entrada e resultado acima de 4000 são rejeitados sem gravar. Binding HTTP não ecoa valores ou nomes arbitrários; logs de frameworks e diagnósticos de exceções tratadas são suprimidos, com redução de observabilidade registrada.

Validação real local: restore inicialmente bloqueado pelo sandbox ao ler NuGet.Config, depois concluído com aprovação individual fora do sandbox. Build sem avisos/erros. Primeiros testes novos tiveram falhas de desserialização dos enums no cliente e uma expectativa que confundia telefone válido com CPF incompleto; corrigidos apenas os testes novos. Suíte final com 165 aprovados, zero falhas e um SQL Server ignorado: 111 anteriores + 54 novos. EF, usando Infrastructure como projeto e startup, confirmou ausência de mudanças no modelo. Rollback após primeiro INSERT real, leitura em DbContext independente, conteúdo sanitizado no banco/respostas e ausência de sentinelas nos logs foram verificados com SQLite, sem mocks de persistência. Não se afirma execução SQL Server nesta task.

Teste anterior de concorrência recebeu apenas adaptação da assinatura/delegação de CriarAsync para o parâmetro interno opcional de eventos, sem mudanças de assertions. Inventário completo, comandos, resultados e limitações em VALIDACAO.md; API_CONTRACT/README/OpenAPI atualizados. Nenhuma dependência, entidade, InitialCreate, snapshot ou DDL alterados, nenhum arquivo fora de backend/, nenhum comando Git de alteração, commit ou TASK-005. Autorização SQL antiga nunca habilitada/reutilizada. Entrega para Code Review do PM/Arquiteto.
## TASK-004 — Correção de senhas após Code Review — 17/11/2026

Solicitação do PM: eliminar fragmentos de senhas sem aspas com múltiplas palavras, sem alterar contratos, persistência, auditoria ou outros detectores. IA auxiliou a substituir o reconhecimento de um token por expressão introdutória + scanner conservador. O valor sem aspas termina em vírgula/ponto e vírgula, quebra de linha, próxima expressão de senha ou fim; espaços e pontos internos não encerram. Aspas fechadas na mesma linha preservam o comportamento anterior; aspa não fechada usa fallback conservador. Rótulos existentes permanecem idempotentes e marcadores dentro de aspas não geram falsos trechos adicionais.

Validação executada: build zero erros/avisos; 187 testes aprovados, um SQL Server ignorado, zero falhas. Mantidos 165 casos anteriores, com expectativa de senha com espaços atualizada e delimitador explícito em uma amostra de múltiplos tipos, conforme justificado em VALIDACAO.md. Adicionados 18 casos unitários e quatro de HTTP/SQLite, com idempotência, múltiplos valores, pontuação, quebras de linha, aspas e ausência de fragmentos nas respostas/banco. EF com projeto e startup Infrastructure confirmou nenhuma mudança no modelo. Nenhum SQL Server, dependência nova, migração, alteração de persistência/auditoria/contrato ou Git de alteração. Seis arquivos atualizados, todos backend/. Aguarda nova revisão do PM antes de commit ou TASK-005.
## TASK-004 — Revisão final de rótulo + sufixo — 17/11/2026

O PM solicitou fechar a lacuna senha: [CPF OCULTADO] SegredoFicticio123. Removido o atalho que confiava no prefixo do valor: agora só o conteúdo delimitado completo igual a um rótulo é considerado protegido. Acrescentados testes para os cinco rótulos, sufixos, valores realmente mascarados, reaplicações, HTTP e SQLite, sem modificar stores/auditoria ou contratos REST.

A primeira regressão identificou cinco falhas de idempotência de senhas originalmente entre aspas: remover as aspas da saída tornava a narrativa seguinte parte de um valor sem aspas na segunda passagem. Corrigido preservando as aspas ao redor do rótulo, mantendo a fronteira do valor. Ajustadas as expectativas correspondentes e as amostras antigas que presumiam narrativa após rótulo sem delimitador. Nenhuma assertion de idempotência removida.

Resultado observado: build zero avisos/erros; 203 aprovados, zero falhas, um SQL Server ignorado (204 totais), incluindo 16 novos casos. EF com projeto/startup Infrastructure sem diferenças pendentes; git diff --check sem problemas. Sem SQL Server, dependências, schema, migrações, contratos, Git de alteração, Mobile ou TASK-005. Detector, dois arquivos de testes e README/VALIDACAO/prompts foram editados nesta revisão, preservando alterações prévias. Aguarda nova revisão do PM.
## TASK-005 — Risco Cidadão e adapter LLM — 17/11/2026

Solicitação aprovada pelo PM: implementar indicador global de sete dias UTC, pesos/classificação determinísticos e contrato compatível com Mobile, mais adapter HTTP real para OpenAI Responses API, sem chamada externa real ou SQL Server. Nenhuma mudança de schema/dependências, sem persistir explicação agregada ou produzir VAZAMENTO_BLOQUEADO.

Com assistência de IA, implementados cálculo Domain, contratos/serviço Application, consulta agrupada e adapter Infrastructure, controller/configuração/OpenAPI. Alertas fixos do Mobile preservados; janela/explicacao adicionados separadamente. Cliente externo recebe só números e enum, constrói payload por allowlist e usa endpoint fixo, modelo configurável, JSON estruturado, store=false, timeout cinco segundos e nenhum retry/redirect. Configuração Enabled=false; chave somente em configuração segura externa. Nenhuma credencial real inserida, solicitada ou exposta. Validação de saída, fallback e cancelamento separados; GET não grava. Documentação manual source/transform/sink em docs/TAINT_ANALYSIS.md.

Protocolo consultado na referência oficial OpenAI; integração implementada com HTTP/JSON nativos, sem SDK. Testes usam exclusivamente handler simulado e SQLite temporário. Primeiros 46 novos casos passaram; diagnóstico offline SQL adicional falhou por parâmetro EF.Functions extraído sem contexto no helper, corrigido apenas no helper e aprovado com zero conexões. Última suíte: 253 aprovados, zero falhas e um SQL Server ignorado (203 anteriores intactos + 50 novos). Build zero avisos/erros; EF usando projeto/startup Infrastructure sem diferenças; git diff --check sem problemas. Restore não necessário, dependências inalteradas.

Inventário completo de 19 arquivos (11 novos/oito modificados), comandos e evidências em VALIDACAO.md. Nenhum arquivo fora de backend/ alterado, nenhum acesso SQL Server, inferência OpenAI real, migração, alteração Git, commit ou TASK-006. Fonte Mobile foi somente lida para conferência; não executado teste ponta a ponta Mobile. Permanecem pendentes Code Review, autorização específica para eventual validação remota real, exibição da explicação no Mobile e avaliações futuras de volume/custo/limitações heurísticas.
## TASK-006 — Dispositivos e leituras IoT — 18/11/2026

Solicitação aprovada pelo PM: fluxo mínimo GET /api/dispositivos e POST /api/leituras, regra pura >=90.00m, UTC de recebimento, precisão decimal sem arredondar, corpo 1 KiB sem depender de Content-Length. Preservar schema/contratos anteriores, sem SQL Server, Mobile, autenticação ou ocorrências automáticas.

Resultado com assistência de IA: regra Domain; DTOs, interface e serviço Application; store EF assíncrono com projeção de última leitura em uma consulta; controllers e filtro de corpo limitado na API; DI/OpenAPI. Servidor controla ID/data/classificação, rejeita campos extras/duplicados e não fabrica Location. Conteúdo numérico é validado também no token original para impedir arredondamento prévio pelo parser decimal. Os demais módulos/entidades existentes não foram modificados.

Validação observada: primeira rodada teve 11 falhas por CHECK numérica aplicada a TEXT na fixture SQLite. Ajustada apenas a afinidade do DDL temporário para NUMERIC, mantendo CHECK e modelo de produção intactos. Depois, casos de dezenas de casas decimais revelaram duas aceitações indevidas, corrigidas com validação do token antes do model binding; uma falha intermediária de compilação de MultipleOf foi corrigida usando decimal em vez de string. Nenhuma expectativa foi relaxada. Build final zero avisos/erros; 317 aprovados, zero falhas e um SQL Server ignorado (253 anteriores + 64 novos). EF com projeto/startup Infrastructure sem diferenças; SQL do store traduzido offline com zero tentativas de conexão. git diff --check sem problemas. Restore não necessário, sem dependências novas.

Inventário de 18 arquivos e comandos completos em VALIDACAO.md. README/API_CONTRACT descrevem HTTP reproduzível para ESP32, endereço/IP/porta, regra crítica, validações, UTC, ausência de autenticação e duplicação por retry. Apenas teste simulado HTTP/SQLite, sem ESP32 físico, firmware, SQL Server ou OpenAI reais. Nenhum arquivo fora de backend/, alteração Git, commit, seed real, schema/migração, automação ou TASK-007. Aguarda Code Review do PM/Arquiteto antes de qualquer commit.