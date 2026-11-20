# Prova integrada das barreiras de Taint Analysis

Execução local autorizada em 19/11/2026, branch feature/task-008-academic-deliverables. Esta prova é específica de Cybersecurity e não altera a numeração ou entregas de C#/SOA.

## Implementação e ambiente

Teste: [TaintBarrierTests.cs](../../../../tests/ZelaSP.Tests/TaintBarrierTests.cs), método `PiiSanitizadaAuditadaEAgregadaComSaidaContaminadaRejeitadaSemEscritasNoGet`.

Windows, SDK .NET 10.0.202, framework net10.0, configuração Debug; VSTest/xUnit, ASP.NET Core TestServer e SQLite `Data Source=:memory:` da OccurrenceApiFactory existente. Não há Kestrel, navegador, porta TCP ou SQL Server nesta prova. As escritas autorizadas são exclusivamente preparação/POST em SQLite efêmero e arquivos locais de build/evidência.

As constantes de entrada/resultado protegido são as mesmas fixtures de PiiApiTests, reproduzidas no novo arquivo porque as constantes originais são privadas; nenhum teste existente foi modificado. São valores construídos para teste, não coletados de pessoas. Dígitos verificadores válidos de CPF não comprovam que um número seja inexistente ou não atribuído.

O teste reutiliza RiscoAiTests.Handler e Envelope. O HttpMessageHandler é simulado; o adapter OpenAiExplicacaoCidadaClient, os serviços, a calculadora, o store e os validadores são reais. Options com Enabled=true e credencial explicitamente fictícia existem somente na instância isolada do teste; não habilitam o LLM da aplicação. O handler não encaminha requisições à rede.

O wrapper ObservedClient apenas delega ao adapter real e observa seu retorno, sem substituí-lo. Isso permite afirmar que o adapter retornou null antes do fallback da Application, e não apenas que a interface apresentou uma mensagem genérica.

## Fluxo e asserções alcançadas

| Etapa | Observado por asserções no teste aprovado |
|---|---|
| Source HTTP | POST de relato de teste contendo CPF, telefone, email, senha e termo de saúde |
| Transform | Descrição exatamente mascarada com cinco rótulos; resposta POST HTTP 201 sem valores originais |
| Sink de persistência | SQLite confirmado; ocorrência sanitizada, sem valores originais |
| Auditoria real | Cinco categorias, um evento PII_DETECTADO por categoria; ação MASCARADO, origem API_OCORRENCIAS, destino BANCO e timestamp da ocorrência |
| Agregação real | Pontuação 30, MEDIO, contagens 1/1/1/1/1, zero vazamentos bloqueados |
| Request do adapter | Método e endpoint esperados; input somente periodoDias, pontuacao, nivel, deteccoes e vazamentosBloqueados; chaves internas exatas |
| Exclusão de PII | Corpo externo sem originais, relato, rótulos, campos de executor, arquivo ou identificadores individuais |
| Nova source não confiável | Handler retorna envelope de provedor com uma senha fictícia contaminando a explicação |
| Rejeição real | Adapter invocado uma vez e retornando null; não houve retry |
| Sink HTTP público | GET HTTP 200, explicacao.origem=FALLBACK; texto aceito pelo validador e sem senha/valores originais |
| Ausência de escrita no GET | Interceptor cobre Reader/Scalar/NonQuery, síncronos e assíncronos; exige SQLite e SELECT; exatamente um comando observado |
| Estado posterior | Snapshot completo da ocorrência e dos eventos idêntico ao anterior; zero histórico/mensagens; nenhum VAZAMENTO_BLOQUEADO criado |

O source é a descrição HTTP, seguida por uma nova source na resposta externa simulada. Transforms/sinks são os caminhos de produção; a resposta contaminada é fixture de teste. Não foi inserido texto contaminado em entidade persistente nem enviado dado pessoal a um provedor.

## Comandos e resultados reais

Executados a partir da raiz do repositório. Saída de build separada, no mesmo nível de profundidade do bin padrão para preservar a referência relativa ao Mobile que existe em RiscoApiTests. Não foi necessário encerrar processo preexistente nem restaurar pacotes.

### Novo teste isolado

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-restore --output .\backend\tests\ZelaSP.Tests\bin\TaintValidation\net10.0 --filter "FullyQualifiedName~ZelaSP.Tests.TaintBarrierTests" --logger "console;verbosity=normal"
```

Resultado observado: **1 executado, 1 aprovado, zero falhas/ignorados, exit 0**, tempo total 2,8020 s. Build de todos os projetos necessários concluído sem diagnóstico de compilação na saída. A saída isolada foi observada no terminal; não há arquivo bruto separado dessa primeira execução. Este parágrafo é registro documental, não recriação de log.

### Regressão pertinente

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-build --no-restore --output .\backend\tests\ZelaSP.Tests\bin\TaintValidation\net10.0 --filter "(FullyQualifiedName~ZelaSP.Tests.ProtecaoPiiTests|FullyQualifiedName~ZelaSP.Tests.PiiApiTests|FullyQualifiedName~ZelaSP.Tests.RiscoAiTests|FullyQualifiedName~ZelaSP.Tests.RiscoApiTests|FullyQualifiedName~ZelaSP.Tests.RiscoCidadaoTests|FullyQualifiedName~ZelaSP.Tests.TaintBarrierTests)&Category!=SqlServerWrite" --logger "console;verbosity=minimal"
```

Stdout/stderr foram encaminhados pelo PowerShell para Tee-Object nesta execução; log bruto preservado em [regressao-console.txt](regressao-console.txt).

Resultado: **142 executados, 142 aprovados, zero falhas/ignorados, exit 0**; duração apresentada pelo runner: 5 s. Inclui novamente o novo caso; não somar a execução isolada como um teste distinto. Não é a suíte integral do backend. O filtro não seleciona teste SQL Server nem RiscoSqlServerProjectionTests.

SHA-256 do teste executado: EF651B7FBD5AD610EA296BEA87E89697C41DDF87BA2C094080776603CDE50A1E.
SHA-256 do log bruto: BBA5AFF5C722E37AAFFFB8B90C68DE99ECB0311CDA907E558623A8BEE3258BF1.

`git diff --check` passou; apenas avisos LF/CRLF de alterações preexistentes. Nenhum Git de escrita, SQL Server, migração, LLM real ou alteração de código funcional. ApiExceptionHandler e entregas C#/SOA não foram modificados.

## Evidências e limites perante o professor

Disponíveis: código do teste e seu encadeamento, log real da regressão e este registro. Capturas de código/log ainda podem ser produzidas manualmente; não foram fabricadas screenshots. A evidência de integração real anterior permanece separada em [API com fallback](../integracao-real-5188-20261119/README.md).

Esta execução comprova barreiras concretas: sanitização antes do banco, exclusão estrutural no request externo e rejeição de resposta contaminada antes da resposta pública. A auditoria registra detecção/mascaramento; rejeição do texto da IA não produz evento próprio e não foi inventado como bloqueio auditado.

Não comprova rastreamento geral de variáveis, propagação explícita de um marcador de taint, guard universal de logs, toda categoria possível de PII, inferência LLM real ou execução SQL Server desse cenário. Não testa tentativa de console.log/logger.info com variável marcada. Se a avaliação exigir literalmente esse mecanismo, a lacuna permanece e requer decisão funcional separada; este teste não deve ser apresentado como substituto integral.
