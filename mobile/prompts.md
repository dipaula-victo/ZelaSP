# Apoio de IA

O desenvolvimento mobile utilizou assistência de IA para estrutura Expo/TypeScript, componentes, persistência, simulador, validação de entradas, testes e documentação. A revisão R03 foi solicitada para concluir o módulo antes de comunicar progresso ao grupo, usando o escopo final ZelaSP e os casos de QA.

Verificações realizadas e limites estão em docs/VALIDACAO.md. Não foram inventadas entrevistas, medições de impacto, prints, vídeo ou aprovação dos testes do backend.

## Cybersecurity — explicação cidadã no painel e fronteira de dados

Solicitação: preservar as telas e entregas C#/SOA, consumir o contrato real de risco e mostrar explicação/origem, sem chamadas reais ao LLM, SQL Server ou mudanças de schema. IA apoiou inspeção dos serviços backend, contrato, gateway, contexto/cache e tela; implementou validação/projeção fechada de risco, cliente GET isolável, exibição LLM/FALLBACK, revalidação de cache e avisos locais do painel. Não reimplementou cadastro/status/imagens nem alterou ApiExceptionHandler. Registros TAINT/bloqueios do mock foram explicitamente diferenciados das barreiras reais.

Validação local com Node24.21.0, sem instalar dependências:26 casos core preservados e50 novos passaram numa rodada de76 aprovações; depois foram acrescentados três casos de fronteira, totalizando **79 aprovados/zero falhas**, com53 casos novos e rede real bloqueada. Exploração inicial: falha de caminho por execução fora de mobile/; primeira rodada nova: uma assertiva incorreta para entrada de espaço, corrigida sem relaxar a implementação. Testes React/typecheck não executados, pois node_modules ausente. Tentativa de teste backend --no-restore bloqueada no build por DLLs da API em uso, MSB3021/MSB3027, sem testes executados; nenhum processo foi encerrado. Limites e roteiro futuro do vídeo em backend/docs/CYBERSECURITY_FLUXO_E_VALIDACAO.md. Não foi produzido PDF final, commit, push ou demonstração externa; evidências simuladas não foram declaradas como integração completa.
