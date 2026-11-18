# Durabilidade e exibição após reinício — pendente

Não executado nesta fase. Não confundir leitura por outro DbContext ou modo mock com reinicialização explícita da API e cliente.

## Autorizações e pré-condições

Lote/IDs/uploads confirmados, SQL de leitura autorizado, processos identificados e aprovação para encerrar/iniciar **somente a API e o cliente criados para a demonstração**. Não parar serviço SQL Server, processos de outros projetos ou navegador inteiro de outro usuário. Revisar comandos concretos/PIDs antes de executar; se houver PID/porta já ocupados de propriedade incerta, parar.

1. Registrar commit/binário da API, PID, porta, destino efetivo aprovado e IA desabilitada. Guardar PID do cliente. Manifesto deve conter IDs PNG/JPEG, MIME, comprimentos e hashes fonte.
2. Confirmar SQL de leitura 03 para varbinary(max), DATALENGTH e SHA2_256. Não imprimir blob. Anexar resultado observado, não preencher com hashes esperados sem consulta.
3. Baixar conteúdo antes do reinício e comparar byte a byte/hash com fonte. Registrar HTTP 200, MIME e metadados. Não usar cache do Mobile como origem de prova.
4. Encerrar a API própria e comprovar saída do processo e liberação da porta. Não usar wildcard por nome de processo ou encerramento indiscriminado.
5. Encerrar o processo do cliente de demonstração; registrar encerramento. Um novo DbContext, aba ou sessão mantida não satisfaz essa etapa.
6. Iniciar nova API, mesmo binário/commit e conexão integrada explicitamente fixada para ZelaSP_Dev. Confirmar novo PID/porta/configuração, sem migration/seed/startup que grave. Se não conseguir confirmar o destino, não prosseguir.
7. Iniciar novo processo de cliente HTTP, sem reutilizar sessão/cache. Registrar novo PID. Fazer requests de metadados e GET /api/imagens/{id}/conteudo para os IDs já capturados, não novo upload. Usar cliente sem cache ou política no-cache; não abrir a fonte local fingindo download.
8. Salvar os downloads novos em arquivos próprios de evidência, sem sobrescrever as fontes. Registrar caminho, tamanho e SHA-256, e conferir igualdade byte a byte. Arquivo já existente/ambíguo exige outro nome e registro, nunca limpeza indiscriminada.
9. Repetir SQL de leitura para provar que o blob persistido corresponde aos bytes recuperados. Normalizar somente caixa do hexadecimal do hash, nunca os bytes.
10. Renderizar os **downloads pós-reinício**, registrar caminho/origem e captura visual. Pixels 1×1 podem ser ampliados por zoom, sem resalvar/converter o arquivo; hash se refere ao download original. Captura do visualizador não substitui a comparação binária.
11. Conferir estado/histórico de B preservados e nenhum novo registro. Preencher manifesto.reinicio com PIDs, horários, resultados e arquivos observados. Só marcar CONCLUIDO quando todos os passos foram feitos.

Esperado: PNG 120 bytes e JPEG 634, hashes iguais à fonte/SQL, metadados consistentes, B EM_ANALISE com um histórico, zero novo upload/ocorrência/histórico/evento. Esses números são **expectativas** do lote novo. A prova histórica da TASK-003 confirmou bytes/hashes, mas não este procedimento completo de reinício/renderização.

Se falhar: preservar processos/arquivos conforme estado seguro conhecido, registrar falha e parar. Não reiniciar outro serviço, reenviar POST, editar blobs ou reparar banco. Repetição de leitura após resolução operacional deve ser identificada como nova tentativa de consulta, nunca esconder uma falha anterior.
