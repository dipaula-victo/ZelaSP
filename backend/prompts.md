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
