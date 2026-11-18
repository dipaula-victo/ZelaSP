# TASK-007 — Preparação offline da demonstração

Estado: **PREPARADO, NÃO EXECUTADO**. Somente preparação offline autorizada. Nenhum SQL, teste, build, API, OpenAI ou ESP32 foi executado nesta fase. Nenhum dado histórico é tratado como existente hoje.

## Separação dos artefatos

| Artefato | Finalidade | Autorização futura |
|---|---|---|
| `carga.template.json` | Plano lógico de 24 registros, sem IDs fixos | Revisão do PM |
| `manifesto.template.json` | Destino esperado, IDs, etapas, resultados e aprovações | Preencher observações somente após execução autorizada |
| `Preparar-Lote.ps1` | Materializa um diretório NOVO local com manifesto, corpos e amostras | Executar somente após autorização específica de preparação local; não executado agora |
| `sql/01-preflight.sql` | SELECTs de identidade/schema/baseline | Conexão SQL de leitura |
| `sql/02-criar-referencias.sql` | Somente oito INSERTs lógicos: dois bairros, quatro categorias, dois dispositivos | Escrita SQL específica para um marcador |
| `sql/03-verificar-lote.sql` | SELECTs de referências, estados, históricos, imagens, IoT e eventos próprios | SQL de leitura |
| `sql/04-conferir-risco.sql` | Agregação global na janela do GET | SQL de leitura |
| `sql/05-recuperar-referencias.sql` | Inspeção por marcador após falha/timeout; nunca repara | SQL de leitura |
| `PROCEDIMENTOS_HTTP.md` | Requests individuais e guardas manuais por etapa | API com escrita no banco específico |
| `EVIDENCIAS.md` | T01–T10/E01–E09, esperados e pendências | Execução/capturas futuras |
| `REINICIO_IMAGENS.md` | Reinícios explícitos e renderização pós-recuperação | Processos próprios + API/SQL de leitura |
| `COMPATIBILIDADE_MOBILE.md` | Bloqueios de integração existentes | Correções fora desta preparação |
| `amostras/` e `PII_REVISAO_ESTATICA.md` | Fixtures fictícias e inspeção estática do relato | Não constituem testes executados |

Nenhum arquivo é carregado por Program.cs, startup, migrações, MSBuild, testes ou pipeline. Não existe executor HTTP automático neste pacote. Os SQLs não têm conexão embutida, `USE` ou fallback de destino: recebem parâmetros de um executor a revisar. Colar no SSMS sem vincular os parâmetros deve falhar, não provisionar com valores padrão.

## Lote proposto

Marcador obrigatório: `TASK007-DEMO-` + 32 caracteres hexadecimais minúsculos (GUID no formato N), fornecido explicitamente. Nomes das referências/dispositivos incluem o marcador para distinguir a propriedade do lote; não se reutilizam registros de nomes semelhantes. Os nomes lógicos são Teste Centro, Teste Norte, Lixeira cheia, Descarte irregular, Buraco e Iluminação. Se nomes sem sufixo forem obrigatórios, interromper e obter decisão do PM antes da carga.

Quantidades planejadas: BAIRRO 2, CATEGORIA_OCORRENCIA 4, OCORRENCIA 3, HISTORICO_STATUS 3, IMAGEM_OCORRENCIA 2, DISPOSITIVO 2, LEITURA_SENSOR 3, EVENTO_SEGURANCA 5 e MENSAGEM_RISCO 0. **24 registros novos**, condicionados ao relato produzir os cinco eventos esperados. Isso não foi comprovado por execução nesta fase.

Somente referências são criadas pelo SQL de escrita. Ocorrências, transições, imagens e leituras seguem endpoints existentes. Datas e IDs vêm do servidor. B e C podem receber PATCH somente enquanto identificadas como registros criados pelo próprio lote autorizado; nenhum registro do baseline é alterado. Estados finais A=ABERTA/0 históricos, B=EM_ANALISE/1, C=RESOLVIDA/2. As duas imagens ficam em B. Dispositivo com leitura recebe 89.99, 90.00 e 90.01; o segundo permanece sem leitura.

## Guardas e reexecução

1. Confirmar branch/commit/working tree antes de qualquer etapa futura; registrar diferenças aprovadas.
2. Aprovação explícita separada para preparação local, leitura SQL, referências SQL, mutações HTTP e reinícios. Campos/strings de confirmação são guardas operacionais, **não autorização organizacional nem autenticação**. Os nomes `PM_PO_TASK007_*_APROVADO` nos SQLs são condições futuras; não foram concedidos nesta fase.
3. SQL somente em `.\SQLEXPRESS`, servidor efetivo `slayr\SQLEXPRESS`, `ZelaSP_Dev`, Windows integrada, ONLINE, InitialCreate/10.0.6 e schema verificados. Configuração divergente implica parada; não criar banco/aplicar migração ou procurar outro destino.
4. Capturar baseline de contagens e IDs de eventos, sem ler conteúdo pessoal preexistente. Nenhum total histórico é baseline atual.
5. Um único operador/escritor durante os requests. Marcador completo e IDs retornados formam o manifesto; IDs não são inferidos por ordem/IDENT_CURRENT/MAX.
6. Anotar `EM_ENVIO` e incrementar tentativa no manifesto ANTES de cada operação de escrita. Exigir tentativa zero e pré-condições do roteiro. Não executar lote HTTP inteiro sem checkpoints.
7. Sucesso só é confirmado depois de HTTP/SQL esperado e persistência dos IDs/evidências. Timeout, falta de resposta, falha em salvar manifesto ou resultado inesperado tornam a etapa **AMBIGUA**: interromper todas as escritas.
8. Sem retries, DELETE, TRUNCATE, DROP, UPDATE de reparação, reset de status, limpeza ou exclusão de registros. O SQL de referências só desfaz sua própria transação não confirmada em caso de erro; nunca reverte transação externa.
9. Se o marcador já existe, o SQL de criação aborta. Inspecionar com SELECTs e pedir revisão para retomar. Etapa concluída não é repetida; lote concluído passa a somente leitura. Expiração da janela de sete dias não autoriza alterar datas/eventos para fazê-los voltar ao indicador.

## Limites importantes

- Idempotência é fail-closed, não promessa de exactly-once: schemas/POSTs não têm chave de idempotência. Leituras não têm marcador; correlacionam-se pelo dispositivo exclusivo e IDs capturados. Ambiguidade nunca autoriza reenviar.
- EVENTO_SEGURANCA não tem FK para ocorrência ou marcador do lote. Capturar IDs antes/depois, usar o horário persistido de B e janela exclusiva; cinco categorias/metadados esperados precisam concordar. Se houver concorrência ou dúvida de propriedade, parar. Não modificar origem/ação para inserir marcador e não atribuir causalidade que o schema não prova.
- Indicador é global. Os cinco eventos planejados somam 30 pontos brutos novos, mas a resposta pode ter outros eventos ou já estar saturada. Conferir a janela e a precisão de parâmetros do EF; não afirmar que o total global deve ser 30.
- IA deve permanecer explicitamente desabilitada na demonstração base. Chamada real OpenAI, uso de credenciais e hardware ESP32 exigem autorização própria; nenhuma credencial pertence ao manifesto.
- Sem correção Mobile, endpoints novos, alterações de schema, autenticação ou ocorrências automáticas. Não usar modo mock ou headers de simulação Mobile como prova de backend real.
- T01–T10/E01–E09 são proposta interna. Correspondência oficial: **PENDENTE DE CONFERÊNCIA DOCUMENTAL**.

## Preparação futura, ainda não executada

`Preparar-Lote.ps1` exige todos os argumentos abaixo; substituir o marcador por um GUID próprio e obter aprovação antes. Nenhum comando desta seção foi executado:

```powershell
.\backend\demo\Preparar-Lote.ps1 -Marcador 'TASK007-DEMO-<32-hex-minusculos>' -Instancia '.\SQLEXPRESS' -Banco 'ZelaSP_Dev' -BaseUri 'http://127.0.0.1:5187' -Confirmacao 'APENAS_PREPARACAO_OFFLINE'
```

O placeholder do marcador é deliberadamente inválido. O script só grava um diretório novo sob `backend/demo/execucoes/`; não acessa SQL/HTTP, não gera autorizações de escrita e não executa detector PII. Execução de scripts não está autorizada pelo simples fato de este README existir. Antes da fase real, revisar o comando integral do executor SQL/API, parâmetros, baseline e marcador com PM/PO.

## Ajustes de preparação para revisão

O diretório local execucoes/ é ignorado por backend/.gitignore; templates, scripts, documentação e amostras permanecem fora dessa regra. O [procedimento futuro de execução SQL](sql/PROCEDIMENTO_EXECUCAO.md) é exclusivamente documental e depende de nova aprovação do executor integral.

A correlação PII é operacional, sem FK evento→ocorrência: baseline anterior a B, IDs novos e janela exclusiva são obrigatórios. Igualdade temporal depende de validação real; diferenças exigem interrupção/revisão manual, sem atribuição arbitrária ou alteração de timestamps. CPF matematicamente válido não tem inexistência comprovada; consultar PII_REVISAO_ESTATICA.md.
