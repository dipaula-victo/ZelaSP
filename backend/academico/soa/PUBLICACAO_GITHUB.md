# Referência final e publicação — ação exclusiva do usuário

Link de entrega: https://github.com/dipaula-victo/ZelaSP.

## Observado localmente na preparação

- Branch:`feature/task-008-academic-deliverables`.
- HEAD:`a9cc40e` — merge PR8/TASK-007.
- `origin` configurado para o repositório acima. Não houve fetch, consulta remota ou publicação; não se confirmou acesso público/privado do professor nem se os arquivos atuais estão online.
- As TASKs001–006, inclusive PII/LLM, possuem fontes rastreados. Existem alterações e arquivos acadêmicos locais fora do commit; evidência disponível no disco não significa evidência disponível no link entregue.

## Alterações preexistentes e decisão de publicação

| Caminho | Estado na inspeção | Tratamento recomendado |
|---|---|---|
| backend/VALIDACAO.md | Modificado localmente | Revisar/publicar o complemento histórico necessário às provas citadas, sem reescrever resultados |
| backend/prompts.md | Modificado localmente | Preservar alterações anteriores; revisar/publicar registros pertinentes |
| backend/tests/ZelaSP.Tests/OccurrenceApiFactory.cs | Modificado localmente | Publicar junto do AcademicRollbackTests se a prova adicional e a seleção42 forem incluídas; injeta interceptor só nos testes |
| backend/tests/ZelaSP.Tests/AcademicRollbackTests.cs | Não rastreado | Necessário para a seleção42 e para localizar a prova pós-duas-escritas; publicar com a factory e E07 |
| backend/src/ZelaSP.Api/ApiExceptionHandler.cs | Modificado localmente | Instrumentação temporária Development para diagnóstico, não requisito de SOA. PM deve decidir manter ou remover em etapa autorizada; não aplicar a cópia C# silenciosamente |
| backend/tests/E05ProcessHost/ | Não rastreado | Host auxiliar histórico de reinício SQLite, não parte da API oficial nem da solução principal. Não é necessário publicar para executar a seleção42; incluir somente se acompanhado dos procedimentos revisados |
| backend/academico/csharp/ | Não rastreado | Não publicar indiscriminadamente a entrega inteira. PDF e registro E07 pertinentes já foram copiados para SOA; os originais não foram alterados |
| backend/academico/soa/ | Nova documentação | Publicar README, matriz, catálogo, índice, checklist, PDF e registro E07 de referência após revisão |
| README.md e backend/README.md | Complementos documentais desta preparação | Publicar os links para o ponto de entrada de SOA |

A correção RM555530 foi feita anteriormente somente na cópia de C#, não no backend original. Não importar esse backend duplicado por engano. SOA referencia backend/src, backend/tests, migrações e configuração do projeto original. A composição da equipe está no README específico.

## Segurança e arquivos que não devem ser publicados

Inspeção estática local: não foram encontrados bin/obj/.vs/node_modules, DLL/EXE/PDB, bancos temporários ou chaves privadas entre os arquivos rastreados examinados. Há diretórios gerados/execuções locais no disco; isso não significa que estejam versionados. `.gitignore` do backend já exclui bin/obj/.vs, .env, appsettings.Local.json, TestResults e demo/execucoes.

Não usar inclusão indiscriminada de backend/academico: a cópia C# contém fontes duplicados, relatórios históricos e manifesto próprios. Não publicar caches, bancos SQLite, ready/proofKey, autorizações/sinais de execução, tokens ou connection strings com senha. Configuração integrada Windows existente não contém senha, mas seu destino é específico da máquina histórica. A inspeção de padrões não certifica ausência universal de segredos; revisar a seleção final do usuário.

## Checklist antes de entregar o link

1. Revisar o diff e decidir sobre o diagnóstico temporário; nenhuma remoção foi feita aqui.
2. Selecionar os documentos e evidências SOA e os arquivos de teste/factory citados; manter o repositório completo para a suíte que consulta Mobile.
3. Validar links e ausência de dados sensíveis no conjunto selecionado. Não executar testes SQL protegidos.
4. O usuário controla revisão, commit e publicação. Esta tarefa não executa esses passos.
5. Confirmar no GitHub que o README, backend completo, migração, testes, PDF e documentos SOA aparecem na referência entregue e que o professor consegue acessá-los.
6. Se a entrega usar a URL raiz, confirmar que a branch padrão aponta para a versão aprovada ou que a navegação para ela está explícita. Informar o commit final para rastreabilidade, sem presumir que uma branch local já foi integrada.
7. Entregar o link no canal solicitado pelo professor. Não há exigência adicional de ZIP/PDF exclusivo de SOA nos slides consultados; o PDF aqui é evidência complementar reaproveitada.

## Limites desta preparação

Não houve nova compilação, restore, teste, API, SQL, migração, inferência LLM ou demonstração. Não houve acesso ao GitHub para conferir disponibilidade, nem alteração de código funcional, commit/push/merge, ZIP ou publicação. O formato documental está preparado; a efetivação da entrega depende da publicação e da conferência de acesso pelo responsável.
