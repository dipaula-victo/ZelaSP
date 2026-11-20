# Classificação explícita e proteção do sink HTTP

Prova específica de Cybersecurity, em 19/11/2026, branch feature/task-008-academic-deliverables. Não altera entregas C#/SOA ou a numeração de evidências dessas disciplinas.

## Arquivos implementados

- [TextoClassificado.cs](../../../../src/ZelaSP.Application/TextoClassificado.cs): novo tipo imutável e barreira de publicação.
- [RiscoCidadaoService.cs](../../../../src/ZelaSP.Application/RiscoCidadaoService.cs): integração na explicação HTTP e fachada do validador existente.
- [TextoClassificadoTests.cs](../../../../tests/ZelaSP.Tests/TextoClassificadoTests.cs): 25 casos de classificação, propagação, representações e publicação.
- [TaintPublicationTests.cs](../../../../tests/ZelaSP.Tests/TaintPublicationTests.cs): oito casos no controller/serviço reais em TestServer.

Nenhum teste existente foi modificado. Nenhuma mudança em DTO/contrato REST, endpoint, schema, migração, Program.cs, adapter HTTP, ApiExceptionHandler ou configuração persistente nesta implementação. Nenhum novo evento de bloqueio foi produzido.

## Source → classificação → sink

Source não confiável: texto retornado por IExplicacaoCidadaClient. O adapter existente já valida a saída remota; o serviço mantém uma segunda barreira contra implementações ou resultados contaminados desse contrato.

TextoClassificado.Classificar utiliza ProtecaoPii e produz uma classificação explícita: Sensivel, SemDeteccaoReconhecida ou Indeterminada. Conteúdo nulo, acima do limite ou timeout de regex não é liberado; timeout não é propagado, pois a exceção pode reter o input. Conteúdo original fica em campo privado readonly, sem getter público, conversão implícita, construtor público ou setter. ToString/interpolação mostram uma constante; representação padrão de depuração é constante e o campo privado está oculto da expansão padrão. Isso não impede inspeção deliberada da memória/reflection por código privilegiado.

Concatenação suportada: Concatenar entre duas instâncias. Se um operando for sensível, o resultado continua sensível; incerteza também não é removida. A combinação é reclassificada para detectar PII formada entre partes antes inofensivas, por exemplo fragmentos de uma declaração de senha. A operação é limitada antes de alocar conteúdo excessivo. Não há método para remover arbitrariamente a classificação. Operandos originais não mudam.

Sink protegido: texto da ExplicacaoCidadaResponse construída por RiscoCidadaoService.ObterAsync. TentarPublicar libera apenas SemDeteccaoReconhecida, não vazio, até 600 caracteres, sem espaços nas extremidades, controles/caracteres de formato, caracteres proibidos ou links reconhecidos. Na recusa, out é null. O serviço usa exclusivamente esse out, nunca o texto original diretamente. Fallback determinístico também passa por TentarPublicar; falha inesperada do próprio fallback gera exceção constante, sem conteúdo, em vez de liberar texto indevido.

ValidadorExplicacaoCidada.Aceita mantém a assinatura e delega ao mesmo guard. O limite e as regras existentes estão centralizados, evitando versões divergentes entre adapter e serviço. Score, nível, alertas e origem mantêm os contratos existentes.

## Testes novos e simulações

TextoClassificadoTests cobre as cinco categorias reconhecidas, texto comum, propagação nas duas ordens, detecção na fronteira da concatenação, imutabilidade observável, out null na recusa, concatenação permitida, ToString/interpolação constantes, serialização System.Text.Json padrão/IncludeFields sem conteúdo privado, construção controlada, ausência de conversão implícita, formatos inválidos, limites e incerteza.

TaintPublicationTests usa TestServer, OccurrenceApiFactory e SQLite :memory: temporário. Apenas o IExplicacaoCidadaClient é substituído por uma source controlada de texto; serviço, classificação, guard, controller e serialização são reais. As respostas com PII reconhecida, formato inválido ou null resultam em HTTP 200/FALLBACK e sem input original no JSON. Texto permitido permanece idêntico e com origem LLM conforme o contrato. Nesse teste, LLM é apenas o rótulo do resultado aceito da source simulada, não inferência externa. Adapter real e cadeia anterior permanecem cobertos por RiscoAiTests/TaintBarrierTests na regressão.

## Execuções observadas

Ambiente: Windows, .NET SDK 10.0.202, net10.0, Debug, xUnit/VSTest e TestServer em memória. Nenhum SQL Server, listener de API ou provedor externo. Fixtures são dados construídos para teste; validade matemática de CPF não prova inexistência do número.

### Novos testes isolados

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-restore --output .\backend\tests\ZelaSP.Tests\bin\TaintExplicitValidation\net10.0 --filter "FullyQualifiedName~ZelaSP.Tests.TextoClassificadoTests|FullyQualifiedName~ZelaSP.Tests.TaintPublicationTests" --logger "console;verbosity=minimal"
```

**33 aprovados, zero falhas/ignorados, exit 0**; duração informada: 1 s. Resultado observado no terminal, não salvo como log bruto separado; não foi recriado artificialmente.

### Regressão PII/Risco

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-build --no-restore --output .\backend\tests\ZelaSP.Tests\bin\TaintExplicitValidation\net10.0 --filter "(FullyQualifiedName~ZelaSP.Tests.ProtecaoPiiTests|FullyQualifiedName~ZelaSP.Tests.PiiApiTests|FullyQualifiedName~ZelaSP.Tests.RiscoAiTests|FullyQualifiedName~ZelaSP.Tests.RiscoApiTests|FullyQualifiedName~ZelaSP.Tests.RiscoCidadaoTests|FullyQualifiedName~ZelaSP.Tests.TaintBarrierTests|FullyQualifiedName~ZelaSP.Tests.TextoClassificadoTests|FullyQualifiedName~ZelaSP.Tests.TaintPublicationTests)&Category!=SqlServerWrite" --logger "console;verbosity=minimal"
```

**175 aprovados, zero falhas/ignorados, exit 0**; duração apresentada: 5 s. Inclui os 33 novos e os 142 da seleção anterior. Não é suíte integral; não somar a execução isolada como testes distintos. Não seleciona RiscoSqlServerProjectionTests ou teste SQL Server. [Log bruto](regressao-console.txt), capturado por Tee-Object durante a execução.

### Build final

```powershell
dotnet build .\backend\src\ZelaSP.Api\ZelaSP.Api.csproj --configuration Debug --no-restore --output .\backend\.artifacts\taint-explicit-api
```

**Exit 0, zero avisos e erros**, 3,00 s. [Log bruto](build-console.txt). Saída separada preserva os processos/binários preexistentes. Não houve restore ou tentativa de encerrar processos. O build antigo cors-api-validation continua sendo histórico anterior a este guard; não representa esta nova implementação.

`git diff --check`: sem erros, com avisos LF/CRLF em alterações locais preexistentes. Sem Git de escrita. [Integridade dos fontes/logs](INTEGRIDADE.json) registra o estado validado; não inclui a si próprio nem este documento.

## Evidências e limites

Disponíveis: código, logs reais de regressão/build e este registro. Capturas acadêmicas podem mostrar a marcação/propagação, a condição de TentarPublicar, construção do DTO com textoLiberado e o resumo de testes. Nenhuma captura ou resultado foi inventado. A [prova anterior de barreiras integradas](../taint-barreiras-20261119/README.md) e a [integração real com fallback](../integracao-real-5188-20261119/README.md) permanecem históricas, sem reexecução ou alteração.

O mecanismo atende ao recorte de variável explicitamente classificada e recusa de publicação no sink HTTP da explicação. Não implementa tracking global de todas as variáveis, propagação automática pelo runtime, serializadores customizados, reflexão, SQL ou rede. A marcação da descrição original não atravessa banco/rede; a source aqui é a explicação não confiável. Não há guard universal de logs nem afirmação de que código que ignore TextoClassificado esteja protegido.

SemDeteccaoReconhecida não garante ausência de toda PII ou verdade semântica. Valores originais permanecem transitoriamente na memória privada; não há promessa de criptografia/zeroização. Não houve teste determinístico de timeout de regex, embora a decisão fail-closed exista. Rejeição não cria auditoria VAZAMENTO_BLOQUEADO nem altera registros históricos. Não foram reexecutados Expo, SQL Server ou LLM real nesta etapa.
