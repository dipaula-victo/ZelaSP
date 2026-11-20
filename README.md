# ZelaSP — Global Solution FIAP 2026.2

**CitizenTech de zeladoria urbana · Engenharia de Software · Turma 3ESPH**

**Acesso rápido:** [Relatórios e evidências](#15-documentação-relatórios-e-evidências) · [Vídeos demonstrativos](#14-vídeos-demonstrativos) · [Execução dos componentes](#13-execução-dos-componentes).

## 1. Apresentação

O ZelaSP reúne registro e acompanhamento de ocorrências urbanas, persistência de imagens, histórico de status e proteção de informações pessoais. Este repositório apresenta as contribuições das disciplinas em uma solução multidisciplinar, com implementações e demonstrações de alcances diferentes.

O backend ASP.NET Core é compartilhado por C# e SOA. O dashboard Risco Cidadão possui integração real de leitura com essa API. Mobile, IoT, infraestrutura Linux e Machine Learning também possuem entregas próprias; sua presença no repositório não significa integração completa entre todos os componentes.

### Sumário

1. [Apresentação](#1-apresentação)
2. [Problema, solução e ODS](#2-problema-solução-e-ods)
3. [Equipe](#3-equipe)
4. [Arquitetura geral e tecnologias](#4-arquitetura-geral-e-tecnologias)
5. [C# Software Development](#5-c-software-development)
6. [SOA & Web Services](#6-soa--web-services)
7. [Cybersecurity](#7-cybersecurity)
8. [Mobile Development](#8-mobile-development)
9. [Physical Computing / IoT](#9-physical-computing--iot)
10. [Operating Systems](#10-operating-systems)
11. [Machine Learning](#11-machine-learning)
12. [Quality Assurance](#12-quality-assurance)
13. [Execução dos componentes](#13-execução-dos-componentes)
14. [Vídeos demonstrativos](#14-vídeos-demonstrativos)
15. [Documentação, relatórios e evidências](#15-documentação-relatórios-e-evidências)
16. [Limitações conhecidas](#16-limitações-conhecidas)

## 2. Problema, solução e ODS

Lixeiras transbordando, descarte irregular e calçadas obstruídas dificultam a conservação dos espaços públicos. O território de referência do projeto é São Paulo, distrito do Jabaquara. A proposta é facilitar o registro de problemas e o acompanhamento de seu tratamento, com informações organizadas por bairro, categoria, imagem e status.

O **ODS principal é o 11 — Cidades e Comunidades Sustentáveis**, especialmente a meta 11.6, relacionada ao impacto ambiental urbano e à gestão de resíduos. ODS 9 e 10 aparecem nas contribuições de infraestrutura, sensores e acesso à informação. Essas relações são objetivos do projeto; não representam impactos urbanos medidos ou parcerias institucionais comprovadas.

As pesquisas documentadas utilizam fontes secundárias, incluindo SP156. Não houve entrevista direta comprovada com moradores. O indicador de impacto urbano proposto deve ser distinguido do **Risco Cidadão**, que resume eventos técnicos de proteção de dados.

## 3. Equipe

| Integrante | RM |
|---|---|
| Djalma Moreira de Andrade Filho | 555530 |
| Felipe Paes de Barros Muller Carioba | 558447 |
| Lucas Rodrigues de Queiroz | 556323 |
| Victor Hugo de Paula | 554787 |

**Turma: 3ESPH.** Djalma é o responsável acadêmico pela entrega de C#. As responsabilidades operacionais de outras disciplinas estão descritas em seus materiais. O RM técnico configurado no backend original não deve ser confundido com a identificação do líder acadêmico nem usado para reinterpretar evidências históricas.

## 4. Arquitetura geral e tecnologias

| Componente | Tecnologias e responsabilidade | Código |
|---|---|---|
| API compartilhada | .NET 10, ASP.NET Core, C#, DI, serviços REST e OpenAPI | [API](backend/src/ZelaSP.Api) |
| Aplicação e domínio | DTOs, serviços, interfaces, regras, PII e classificação de texto | [Application](backend/src/ZelaSP.Application) · [Domain](backend/src/ZelaSP.Domain) |
| Persistência | EF Core, LINQ assíncrono, SQL Server, migrations e transações | [Infrastructure](backend/src/ZelaSP.Infrastructure) |
| Aplicativo | React Native, Expo SDK 58, TypeScript, Expo Router, SQLite e IndexedDB na web | [Mobile](mobile) |
| Sensor simulado | ESP32, HC-SR04, LEDs, buzzer e MQTT no Wokwi | [IoT](IOT) |
| Laboratório Linux | Ubuntu, Bash, systemd, BIND9, SSH e VirtualBox | [Operating Systems](OS) |
| Modelo analítico | Python, notebook, dados SP156 e Random Forest | [Machine Learning](ml) |
| Qualidade | Features, critérios de aceite e casos de teste no Azure DevOps | [QA](qa) |

A solução .NET está em [backend/ZelaSP.sln](backend/ZelaSP.sln), organizada em quatro projetos. Os testes estão em [backend/tests](backend/tests). O laboratório Linux é independente; o modelo de ML não está integrado ao aplicativo. A publicação MQTT do firmware não equivale a uma ponte MQTT → API implementada.

## 5. C# Software Development

Contribui para a implementação e o comportamento interno do backend: orientação a objetos, interfaces, injeção de dependência, ASP.NET Core Web API, EF Core/LINQ, programação assíncrona, imagens persistidas no banco, transações e testes automatizados.

- **Código:** [quatro projetos](backend/src), [solução](backend/ZelaSP.sln) e [testes](backend/tests/ZelaSP.Tests).
- **Relatório acadêmico de C# (PDF):** [documento com evidências](backend/academico/soa/evidencias/RELATORIO_CSHARP_REFERENCIA.pdf).
- **Evidências:** upload e recuperação no SQL Server, COMMIT manual no SQL Server e ROLLBACK após duas escritas em SQLite/TestServer. Esses ambientes são distintos.
- **Material complementar:** [README técnico do backend](backend/README.md), [validações históricas](backend/VALIDACAO.md) e [registro de rollback SQLite](backend/academico/soa/evidencias/E07_REGISTRO_SQLITE.md).

A entrega acadêmica de C# foi concluída. O relatório preserva suas limitações, incluindo a ausência do ícone oficial do ODS 11, aceita pela equipe como pendência acadêmica.

## 6. SOA & Web Services

Contribui para a estruturação do domínio, separação de responsabilidades, contratos HTTP/JSON, disponibilização dos serviços REST, integração com SQL Server, migrations, documentação OpenAPI e testes dos serviços. Utiliza **o mesmo backend de C#**, sem uma segunda implementação.

| Critério oficial | Pontuação | Implementação e comprovação |
|---|---:|---|
| Estruturação do domínio | 3,0 | [Domain](backend/src/ZelaSP.Domain) e [Application](backend/src/ZelaSP.Application) |
| Conexão com banco | 3,0 | [Infrastructure](backend/src/ZelaSP.Infrastructure) e [integração SQL Server](backend/docs/SQLSERVER_INTEGRATION.md) |
| Migrações | 1,5 | [Migrations EF Core](backend/src/ZelaSP.Infrastructure/Migrations) |
| Documentação automática | 1,5 | [Program.cs](backend/src/ZelaSP.Api/Program.cs) e [catálogo de endpoints](backend/academico/soa/ENDPOINTS.md) |
| Testes automatizados | 1,0 | [Testes](backend/tests/ZelaSP.Tests) e [evidências históricas](backend/academico/soa/EVIDENCIAS.md) |

A documentação de SOA foi integrada à `main` pela PR #9. Consulte diretamente a [matriz de rastreabilidade](backend/academico/soa/MATRIZ_RASTREABILIDADE.md), o [contrato detalhado](backend/docs/API_CONTRACT.md), o [PDF de referência](backend/academico/soa/evidencias/RELATORIO_CSHARP_REFERENCIA.pdf) e, como complemento, o [README de SOA](backend/academico/soa/README.md). Notas de preparação e publicação nos documentos históricos devem ser lidas no contexto de sua data.

## 7. Cybersecurity

Implementa parsing e mascaramento de PII, sanitização antes da persistência, auditoria técnica, classificação explícita de textos sensíveis, propagação conservadora por concatenação e bloqueio de publicação no sink HTTP da explicação cidadã. O indicador Risco Cidadão agrega eventos e apresenta pontuação, classificação, contagens e explicação no dashboard.

- **Código:** [serviços e proteções](backend/src/ZelaSP.Application), [testes](backend/tests/ZelaSP.Tests) e [tela Risco Cidadão](mobile/app/%28tabs%29/risco.tsx).
- **PDF definitivo:** [Relatório de Cybersecurity](backend/academico/cybersecurity/docs/RELATORIO_CYBERSECURITY_ZELASP.pdf).
- **Requisitos e evidências:** [matriz](backend/academico/cybersecurity/MATRIZ_REQUISITOS.md), [índice](backend/academico/cybersecurity/evidencias/INDICE.md) e [Taint Analysis](backend/docs/TAINT_ANALYSIS.md).
- **Prova de integração real:** [registro](backend/academico/cybersecurity/evidencias/integracao-real-5188-20261119/README.md), [dashboard](backend/academico/cybersecurity/evidencias/integracao-real-5188-20261119/dashboard.png) e [JSON recebido](backend/academico/cybersecurity/evidencias/integracao-real-5188-20261119/response.json).
- **Taint explícito:** [registro, testes e limites](backend/academico/cybersecurity/evidencias/taint-explicito-20261119/README.md).
- **Material complementar:** [README de Cybersecurity](backend/academico/cybersecurity/README.md).

A integração Expo Web → API → SQL Server retornou **HTTP 200, pontuação 0, classificação BAIXO, contagens zeradas e origem FALLBACK**, com correspondência entre JSON e interface. A demonstração isolou a tela de risco, bloqueando outras chamadas do Mobile no navegador.

O cliente HTTP de LLM e a validação de respostas estão implementados e testados com provedor simulado. **Não há inferência real com LLM comprovada. FALLBACK é determinístico, não inferência.** O vídeo final de Cybersecurity permanece pendente. A documentação foi integrada à `main` pela PR #10.

## 8. Mobile Development

O aplicativo oferece início, listagem e filtros de ocorrências, novo relato, detalhes do protocolo, imagens, status/histórico, Risco Cidadão e ajustes. Utiliza React Native, Expo, TypeScript e Expo Router; a persistência local usa SQLite em Android/iOS e IndexedDB na prévia web.

- **Código:** [telas](mobile/app), [dados e contratos](mobile/src/data) e [serviços](mobile/src/services).
- **Demonstração própria:** [vídeo do aplicativo](https://www.youtube.com/watch?v=sm76ImEubtk).
- **Capturas existentes:** [Início](https://github.com/user-attachments/assets/81a3b4b2-4509-4adf-96ba-807d38ce2072), [Ocorrências](https://github.com/user-attachments/assets/a61e7a23-6ef9-46b7-abba-668a965a18e8), [Novo relato](https://github.com/user-attachments/assets/fbe27f05-4b01-4174-be29-0b918e43d8a3), [Risco Cidadão](https://github.com/user-attachments/assets/141eefac-10e4-40c0-82a8-8c3e86ad9ed9) e [Ajustes](https://github.com/user-attachments/assets/01e9f828-a837-4c02-ba7d-cd2981694f2f).
- **Material complementar:** [README Mobile](mobile/README.md).

O modo padrão é **MOCK**, com dados fictícios e demonstração própria. A integração real comprovada com o backend limita-se ao dashboard Risco Cidadão; não abrange todas as telas ou operações. A prévia Expo Web não comprova câmera, permissões e persistência nativas em dispositivo.

## 9. Physical Computing / IoT

O [firmware ESP32](IOT/sketch.ino) simula a medição de enchimento com HC-SR04, sinalização por LEDs/buzzer e publicação MQTT. Utiliza o broker público `broker.hivemq.com:1883`, tópico `zelasp/RM558447/lixeira/1/leitura`, e saída JSON no monitor serial.

- **Simulação:** [projeto Wokwi](https://wokwi.com/projects/480868029756689409), [diagrama](IOT/diagram.json) e [bibliotecas](IOT/libraries.txt).
- **Evidências:** [captura do sistema](IOT/print-sistema.png) e [vídeo IoT](https://youtu.be/x6i10w2HIa4).
- **Material complementar:** [README IoT](IOT/README.md).

**Divergências verificadas com o contrato real da API:**

| Aspecto | Firmware/documentação IoT | Backend ASP.NET Core |
|---|---|---|
| Payload | Inclui `dispositivoId`, `nivelPercentual` e `lidaEm` | `POST /api/leituras` aceita somente `dispositivoId` e `nivelPercentual`; rejeita campos extras e gera `lidaEm` no servidor |
| Limiar | Alerta local quando `nivel > 90` | `nivelCritico` quando `nivelPercentual >= 90` |
| Transporte | Publica MQTT | Recebe HTTP; não foi localizada ponte MQTT → HTTP no backend |
| Ocorrência automática | Texto e mensagem serial descrevem essa intenção | A leitura registra telemetria; não cria ocorrência automaticamente |

O payload MQTT não deve ser enviado inalterado ao endpoint HTTP. O dispositivo precisa existir no banco e seu ID não pode ser presumido. A comunicação MQTT pública sem TLS não é apropriada para dados pessoais ou credenciais. Esta documentação registra as diferenças sem modificar a entrega IoT.

## 10. Operating Systems

A disciplina possui infraestrutura Linux de laboratório própria, com duas VMs Ubuntu em rede interna VirtualBox: CT-NODE e CT-CLIENT. Inclui armazenamento persistente dedicado, contas nominais, permissões, agente Bash monitorando CPU/RAM/disco/serviços, execução por systemd, auditoria, SSH e DNS autoritativo BIND9.

- **Código e configuração:** [scripts](OS/scripts) e [configurações](OS/configs).
- **PDF:** [Relatório técnico de Operating Systems](OS/Relatorio_Tecnico_ZelaSP_D03.pdf).
- **Evidências:** [capturas do laboratório](OS/Evidencias) e [vídeo OS](https://youtu.be/flA3pD1Umrc).
- **Material complementar:** [README Operating Systems](OS/README.md).

O laboratório documenta `192.168.56.10/24` e `192.168.56.20/24`, na rede `intnet-zelasp`. Esses endereços e os registros DNS representam o ambiente de laboratório; não comprovam hospedagem do backend ou integração operacional de todos os componentes.

## 11. Machine Learning

A entrega analisa solicitações de limpeza urbana do **SP156**, terceiro trimestre de 2026, para classificar pedidos cujo tempo de atendimento excede o percentil 75 do respectivo serviço. Usa Python e Random Forest, separação por endereço, validação cruzada e análise de vazamento de dados.

- **Código, gráficos e resultados:** [notebook executado no Google Colab](ml/notebook_zelasp_ml.ipynb).
- **Relatório:** [Relatório de Machine Learning](ml/RELATORIO_ML.md).
- **Fonte e reprodução:** [dados SP156 e instruções](ml/dados/README.md) e [dependências](ml/requirements.txt).

O relatório registra F1 de teste **0,417** e AUC **0,680** para o modelo principal, com limitações de precisão, viés de registro e generalização temporal. São resultados históricos documentados, não uma execução atual. **O modelo não está integrado ao aplicativo nem à API.** Não há PDF ou vídeo específico de ML identificado nesta estrutura; o notebook e o relatório Markdown são os materiais disponíveis.

## 12. Quality Assurance

A contribuição de Testing, Compliance & Quality Assurance organiza features, critérios de aceite e casos de teste funcionais positivos e negativos no Azure DevOps.

- **Documento:** [Features e Test Cases — PDF](qa/Features%20e%20Test%20Cases%20-%20Azure%20DevOps.pdf).
- **Gestão:** [projeto no Azure DevOps](https://dev.azure.com/RM554787/3ESPH-GS2026-CitizenTech-ZelaSP).
- **Código de testes relacionados:** [backend](backend/tests) e [Mobile](mobile/tests).

O PDF é o material específico disponível em [qa/](qa); não foi identificado README técnico próprio nessa pasta. Casos de teste descritos não equivalem, por si só, a execuções aprovadas. Os registros de testes automatizados devem ser consultados nas evidências das implementações correspondentes.

## 13. Execução dos componentes

Os comandos a seguir são instruções para reprodução, não operações executadas nesta revisão. Partem da raiz do repositório, salvo indicação. Verifique portas, permissões e destinos antes de iniciar processos.

### Backend compartilhado

Requisitos: **SDK .NET 10** (histórico de validação com 10.0.202), SQL Server e schema de desenvolvimento previamente preparado. A configuração histórica utiliza `.\SQLEXPRESS`, `ZelaSP_Dev` e autenticação integrada do Windows. Em outra máquina, configure um destino de desenvolvimento aprovado.

```powershell
dotnet restore .\backend\ZelaSP.sln
dotnet build .\backend\ZelaSP.sln --configuration Debug --no-restore
$env:ConnectionStrings__ZelaSP = 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
$env:RiscoAi__Enabled = 'false'
dotnet run --project .\backend\src\ZelaSP.Api\ZelaSP.Api.csproj --configuration Debug --no-build --no-restore --no-launch-profile -- --environment Development --urls http://127.0.0.1:5187
```

A confiança no certificado desse exemplo corresponde ao SQL Server local de laboratório; não é recomendação para produção. Não versione credenciais nem configure variáveis globais. O startup não aplica migrations nem seed. Consulte o [procedimento SQL Server](backend/docs/SQLSERVER_INTEGRATION.md), a [migração](backend/src/ZelaSP.Infrastructure/Migrations) e o [DDL existente](backend/Migrations/InitialCreate.sql) antes de preparar um banco novo; não reaplique estrutura a um banco existente sem autorização.

API: `http://127.0.0.1:5187`. Documento automático, somente em Development: `http://127.0.0.1:5187/openapi/v1.json`. Não há Swagger UI/Scalar instalado. Consulte os [endpoints](backend/academico/soa/ENDPOINTS.md) para métodos, DTOs e limites. Encerre a instância iniciada nesse terminal com Ctrl+C.

Seleção acadêmica isolada, após compilar:

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-build --no-restore --filter "(FullyQualifiedName~ZelaSP.Tests.AcademicRollbackTests|FullyQualifiedName~ZelaSP.Tests.OccurrenceApiTests|FullyQualifiedName~ZelaSP.Tests.OccurrenceConcurrencyTests)&Category!=SqlServerWrite"
```

Essa seleção teve **42 aprovações históricas**, mas não é a suíte integral nem garante o resultado de uma execução futura. Parte da suíte mais ampla lê `mobile/src/services/privacy.ts`; preserve o repositório completo e não habilite testes de escrita SQL inadvertidamente.

### Mobile e dashboard

Requisitos: Node.js >= 22.13, npm e ambiente compatível com Expo SDK 58; validações históricas utilizaram Node 24.22.1. A partir da raiz:

```powershell
Set-Location .\mobile
npm ci
npm start
```

O padrão MOCK permite a demonstração própria sem conexão ao backend. Como alternativa ao `npm start`, para o recorte de integração do risco use o comando abaixo, ainda na pasta `mobile/` e sem outro Expo ocupando a porta 8081. Configure as variáveis somente nesse terminal, antes de iniciar:

```powershell
$env:EXPO_PUBLIC_DATA_MODE = 'api'
$env:EXPO_PUBLIC_API_URL = 'http://127.0.0.1:5187'
$env:EXPO_PUBLIC_TEST_MODE = 'false'
npm run web -- --host localhost --port 8081
```

Abra **`http://127.0.0.1:8081`**: essa é a origem autorizada pelo CORS em Development, exclusivamente para GET `/api/risco-cidadao`, sem credenciais cross-origin. A prova histórica usou API na porta 5188, mantendo uma instância preexistente em 5187. Não inicie duas instâncias na mesma porta nem encerre processos alheios. Outras chamadas do Mobile não estão liberadas por essa política; a prova isolada e seus bloqueios estão registrados nas evidências de Cybersecurity.

Nunca coloque chaves LLM em `EXPO_PUBLIC_*`, pois são públicas no bundle. Em Android Emulator, o host costuma ser acessado por `10.0.2.2`; dispositivo físico exige endereço acessível na rede e configuração aprovada. Loopback do aparelho não representa o computador.

### Demais componentes

- **IoT:** abra o [Wokwi](https://wokwi.com/projects/480868029756689409); consulte diagrama, firmware e bibliotecas na seção 9. A simulação publica MQTT e não conclui automaticamente a integração HTTP.
- **Operating Systems:** reproduza as duas VMs conforme o [README OS](OS/README.md). Os scripts de provisionamento alteram o sistema e são destinados ao laboratório, não à máquina do leitor sem revisão.
- **Machine Learning:** abra o [notebook](ml/notebook_zelasp_ml.ipynb) no Colab ou ambiente compatível e siga as [instruções de dados](ml/dados/README.md). O CSV externo precisa ser obtido separadamente; as dependências estão em `ml/requirements.txt`.
- **QA:** consulte o PDF e o projeto Azure DevOps da seção 12; não há um serviço adicional a iniciar nessa pasta.

## 14. Vídeos demonstrativos

| Material | Link | Escopo |
|---|---|---|
| Pitch da equipe | [YouTube](https://www.youtube.com/watch?v=az6H0Y_EZFc) | Apresentação do projeto |
| Mobile | [YouTube](https://www.youtube.com/watch?v=sm76ImEubtk) | Demonstração própria do aplicativo |
| Physical Computing / IoT | [YouTube](https://youtu.be/x6i10w2HIa4) | ESP32 simulado e telemetria |
| Operating Systems | [YouTube](https://youtu.be/flA3pD1Umrc) | Infraestrutura Linux de laboratório |

Os links publicados pelos integrantes foram preservados. Não foi localizado vídeo final específico de Cybersecurity nos materiais revisados. O vídeo Mobile não substitui prova de inferência real com LLM.

## 15. Documentação, relatórios e evidências

| Disciplina/material | Acesso direto |
|---|---|
| C# — relatório com evidências | [PDF](backend/academico/soa/evidencias/RELATORIO_CSHARP_REFERENCIA.pdf) |
| SOA — critérios e endpoints | [Matriz](backend/academico/soa/MATRIZ_RASTREABILIDADE.md) · [Endpoints](backend/academico/soa/ENDPOINTS.md) · [Evidências](backend/academico/soa/EVIDENCIAS.md) |
| Cybersecurity — relatório e comprovações | [PDF](backend/academico/cybersecurity/docs/RELATORIO_CYBERSECURITY_ZELASP.pdf) · [Índice](backend/academico/cybersecurity/evidencias/INDICE.md) · [Conformidade](backend/academico/cybersecurity/CONFORMIDADE.md) |
| Mobile — interface e contrato | [Capturas](mobile/README.md#prints-e-vídeo) · [Contrato da API](backend/docs/API_CONTRACT.md) |
| IoT — simulação | [Captura](IOT/print-sistema.png) · [Wokwi](https://wokwi.com/projects/480868029756689409) |
| Operating Systems — relatório e laboratório | [PDF](OS/Relatorio_Tecnico_ZelaSP_D03.pdf) · [Evidências](OS/Evidencias) |
| Machine Learning — análise e dados | [Relatório](ml/RELATORIO_ML.md) · [Notebook](ml/notebook_zelasp_ml.ipynb) · [Dados](ml/dados/README.md) |
| QA — especificação de testes | [PDF](qa/Features%20e%20Test%20Cases%20-%20Azure%20DevOps.pdf) |
| Backend — documentação técnica | [README](backend/README.md) · [Histórico de validação](backend/VALIDACAO.md) |

Documentos específicos preservam as regras de cada disciplina. As exigências e identificadores acadêmicos de C# não são aplicados automaticamente às demais entregas. Registros históricos devem ser interpretados com seu ambiente, data e limitações.

## 16. Limitações conhecidas

- **LLM:** integração implementada e testada com respostas simuladas; nenhuma inferência real comprovada. A prova de dashboard utiliza FALLBACK determinístico. Vídeo final Cybersecurity pendente.
- **Mobile:** integração real comprovada apenas no recorte de risco; outras telas ainda exigem compatibilização e validação. MOCK não é resposta do backend. Web não equivale à validação nativa.
- **IoT:** payload com campo extra, diferença no limiar e ausência de ponte MQTT → API; não há criação automática de ocorrência pelo recebimento de telemetria. Broker público não deve transportar dados sensíveis.
- **ML:** modelo independente, não integrado; resultados limitados ao recorte SP156 documentado e sujeitos a viés de registro e erros de classificação.
- **Infraestrutura:** laboratório OS independente; não equivale a implantação integrada de produção.
- **Segurança:** detecção de PII e propagação explícita de classificação têm escopo definido; não oferecem proteção universal ou rastreamento de todo fluxo. Não há autenticação de usuários/dispositivos na API acadêmica. Identificação de executor não autentica uma pessoa.
- **Imagens:** não há comprovação de OCR/proteção de PII em fotografias. A imagem utilizada nas demonstrações tem origem informada como Folha de S.Paulo, sem autoria específica, URL original ou autorização de reprodução comprovadas.
- **Evidências C#:** COMMIT manual foi demonstrado no SQL Server; ROLLBACK após duas escritas e reinício controlado histórico foram demonstrados em SQLite/TestServer. Recuperação manual SQL Server não comprova isoladamente reinício. A ausência do ícone oficial do ODS 11 permanece uma ressalva conhecida do relatório entregue.
- **Reprodução:** SQL Server e dados externos SP156 precisam ser preparados no ambiente do leitor; parte da suíte depende de arquivo Mobile. Totais históricos de testes não representam execução atual ou aprovação universal da suíte.
- **Impacto social:** não houve medição de impacto urbano nem entrevista direta comprovada com moradores; atores citados não representam parcerias institucionais estabelecidas.
