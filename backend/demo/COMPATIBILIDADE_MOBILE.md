# Incompatibilidades observadas por leitura — sem correção nesta tarefa

Fontes: mobile/src/services/gateway.ts, contract.ts, privacy.ts, data/types.ts; controllers/DTOs backend e docs/API_CONTRACT.md. Não houve execução do Mobile ou alteração de seus arquivos. As conclusões abaixo são inspeção de contrato, não novas evidências de execução.

| Mobile atual | Backend atual | Consequência |
|---|---|---|
| GET /api/categorias | GET /api/categorias-ocorrencia | Catálogo não usa a rota existente |
| Listagem deve ser array | PaginaOcorrenciasResponse com itens/totais | Parser rejeita formato |
| Detalhe deve conter historico | GET de histórico separado | Parser rejeita DTO sem array |
| Cadastro envia origem | Servidor fixa APP e rejeita extras | POST incompatível |
| Status usa POST + executor | PATCH com somente status | Método/payload incompatíveis |
| Catálogo de executores em /api/equipe | Rota inexistente | Seleção de executor prejudicada |
| Guarda PII própria | Regras diferentes e aspas preservadas em senha protegida | Possível rejeição de texto já protegido; precisa revisão do integrante |
| Headers X-Simular-Falha/X-Simular-Log-Inseguro | Sem injeção equivalente na API | Não comprovam rollback/bloqueio real |
| Painel de risco com alertas fixos | Campos/alertas preservados; explicacao extra | Contrato de risco compatível por inspeção/testes históricos; tela não mostra explicação nova |
| Sem tipos/consumidor de dispositivos/leituras | TASK-006 fornece telemetria | Nenhuma demonstração IoT Mobile pronta |

Não foi identificada política CORS para frontend web em origem diferente. App nativo e browser têm necessidades de rede distintas; não afirmar que permitir rede/CORS resolve os demais contratos. Não adicionar permissões amplas, aliases de endpoints ou mudanças de DTO backend nesta preparação.

Para demonstração backend usar cliente HTTP independente e identificação explícita de fonte real. Screenshots/mock Mobile podem demonstrar o módulo Mobile, mas não integração real SQL/API. Alinhamento deve ser aprovado e executado pelo responsável em tarefa própria. README Mobile referencia arquivos em mobile/docs não localizados no inventário e declara prints/vídeo pendentes; conferir pacote final com o integrante.
