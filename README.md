# ZelaSP
Solução que permite que moradores registrem e acompanhem ocorrências do bairro, como lixeiras cheias e descarte irregular, com foto, histórico de status e monitoramento por sensor (ESP32), além de proteção dos dados pessoais digitados nos relatos.

## Mobile Development & IoT — aplicativo do cidadão

[Documentação do app](mobile/README.md): Ficha da CitizenTech, integrantes, instruções para rodar (`npx expo start`), banco de dados (SQLite), origem dos dados (mock), prints das telas e [vídeo de demonstração](https://www.youtube.com/watch?v=sm76ImEubtk). O código está em `mobile/`, feito com React Native, Expo, TypeScript e Expo Router.

## Backend compartilhado — C# e SOA

C# Software Development e Arquitetura Orientada a Serviços (SOA) e Web Services contribuem para **a mesma API ASP.NET Core**, com código em `backend/src`, solução em `backend/ZelaSP.sln` e testes em `backend/tests`. C# fundamenta a implementação e o comportamento interno; SOA fundamenta a organização, os contratos e a disponibilização dos serviços. Não existem dois backends independentes por disciplina.

O [README do backend](backend/README.md#backend-compartilhado-entre-as-disciplinas) detalha essas contribuições e os procedimentos técnicos. Os serviços podem ser consumidos por diferentes clientes HTTP; o Mobile é um cliente potencial, sem declaração de integração completa nesta documentação.

## Testing, Compliance & Quality Assurance

[Documento de Features e Test Cases (PDF)](qa/Features%20e%20Test%20Cases%20-%20Azure%20DevOps.pdf): features com descrições e critérios de aceite e casos de teste funcionais, positivos e negativos, criados no Azure DevOps. Projeto: [3ESPH-GS2026-CitizenTech-ZelaSP](https://dev.azure.com/RM554787/3ESPH-GS2026-CitizenTech-ZelaSP).

▶️ **Pitch da equipe (até 4 min):** [PITCH](https://www.youtube.com/watch?v=az6H0Y_EZFc)

## C# Software Development — documentação acadêmica

O [Relatório acadêmico de C# (PDF)](backend/academico/soa/evidencias/RELATORIO_CSHARP_REFERENCIA.pdf) reúne a preparação acadêmica da disciplina. Cópias destinadas à entrega e evidências históricas são artefatos documentais; não constituem uma segunda implementação independente do backend.

## SOA e Web Services — Global Solution 2026.2 / 3ESPH

[Documentação de entrega de SOA](backend/academico/soa/README.md): descrição, equipe, arquitetura, SQL Server, migração, OpenAPI, comandos de execução, testes e limitações. Consulte também a [matriz dos cinco critérios](backend/academico/soa/MATRIZ_RASTREABILIDADE.md), o [catálogo de endpoints](backend/academico/soa/ENDPOINTS.md), as [evidências históricas](backend/academico/soa/EVIDENCIAS.md) e o [checklist de publicação](backend/academico/soa/PUBLICACAO_GITHUB.md).

O código oficial do backend está em `backend/src`, com solução em `backend/ZelaSP.sln` e testes em `backend/tests`. A documentação foi preparada localmente; a referência final deve ser revisada e publicada pelo responsável. A existência deste link no arquivo local não comprova sua disponibilidade no GitHub.
