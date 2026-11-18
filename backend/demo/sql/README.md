# SQLs preparados — nenhum executado

Arquivos não contêm `USE`, conexão, credenciais, comandos de criação/alteração/exclusão de schema ou executor automático. O único arquivo com DML é 02: INSERT nas três tabelas de referências. Não há UPDATE/DELETE/TRUNCATE/DROP. COMMIT/ROLLBACK de 02 controlam somente sua própria transação; @@TRANCOUNT externo diferente de zero impede começar.

## Parâmetros comuns

Vincular por parâmetros tipados no executor que será apresentado ao PM antes de conectar. Não concatenar valores em SQL, substituir IDs na string, usar SQLCMD de fonte não revisada ou colar valores históricos. Parâmetros inexistentes/nulos impedem a execução útil; nenhum destino padrão é inferido.

| Parâmetro | Origem/tipo | Valor permitido/esperado |
|---|---|---|
| @Autorizacao | Aprovação específica futura, nvarchar(100) | 01/03/04/05 exigem PM_PO_TASK007_SQL_LEITURA_APROVADO; 02 exige PM_PO_TASK007_REFERENCIAS_APROVADO |
| @ServidorEsperado | destinoEsperado.servidorEfetivo, nvarchar(128) | slayr\SQLEXPRESS |
| @BancoEsperado | destinoEsperado.banco, sysname | ZelaSP_Dev |
| @Marcador | manifesto.marcador, nvarchar(45) | TASK007-DEMO- + 32 hex minúsculos |
| @PreflightConferido | somente 02, bit | 1 após conferir evidência real do preflight |
| @JanelaExclusiva | somente 02, bit | 1 após confirmar operador/escritor único |
| @EventosIsoladosConferidos | somente 03, bit | 1 após revisão da diferença de IDs/janela e metadados |
| @InicioUtc/@FimUtc | somente 04, datetime2 | Janela capturada do GET e mesma escala efetiva do provider EF; não um novo relógio |

As strings são guardas, não segredos, tokens de autenticação ou aprovações já concedidas. Preparação offline não autoriza nenhuma delas. Conectar exclusivamente a `.\SQLEXPRESS`/ZelaSP_Dev com autenticação integrada; guards de identidade não substituem revisão da connection string e do processo API.

## Mapa dos IDs de 03

Todos int, obtidos dinamicamente e conferidos antes de vincular:

| Parâmetros SQL | Campos manifesto.ids |
|---|---|
| @BairroCentroId/@BairroNorteId | bairroCentro/bairroNorte |
| @CategoriaLixeiraId/@CategoriaDescarteId/@CategoriaBuracoId/@CategoriaIluminacaoId | categoriaLixeira/categoriaDescarte/categoriaBuraco/categoriaIluminacao |
| @OcorrenciaAId/@OcorrenciaBId/@OcorrenciaCId | ocorrenciaA/ocorrenciaB/ocorrenciaC |
| @ImagemPngId/@ImagemJpegId | imagemPng/imagemJpeg |
| @DispositivoComLeituraId/@DispositivoSemLeituraId | dispositivoComLeitura/dispositivoSemLeitura |
| @Evento1Id..@Evento5Id | cinco IDs distintos em eventosPii, correlacionados sob ressalvas documentadas |

## Resultados e evidências

- **01:** identidade/ONLINE/autenticação NTLM ou KERBEROS, InitialCreate/10.0.6, tipos/chaves/índices/CHECKs, triggers, contagens e IDs de eventos antes. Esperado zero registros do marcador e nenhuma trigger habilitada nas tabelas do domínio. O estado físico não é conhecido nesta fase.
- **02:** três resultsets, 2 bairros/4 categorias/2 dispositivos, com chaves lógicas e IDs via OUTPUT. Capturar todos; timeout após commit é ambiguidade, não licença para repetir.
- **03:** R1–R11: referências, descrições protegidas/estados, históricos, metadados/hashes/tipo físico, leituras/última, eventos correlacionados operacionalmente/zero mensagens, órfãos/unicidade e totais para deltas. Guardas finais rejeitam estados/imagens/leituras/mensagens divergentes. Comparação textual sanitizada, contagens globais, nomes exatos, timestamps, integridade e baseline continuam exigindo revisão dos resultsets. Não tratar relatório parcial com THROW como sucesso.
- **04:** uma linha com contagens, pontuação bruta/limitada e nível, inclusive se nenhum evento válido. Deve coincidir com o GET da mesma janela. Pode conter contribuições anteriores: dados pessoais originais não são selecionados.
- **05:** recuperação somente leitura por marcador, com snapshot de IDs/horários dos eventos para confronto com o baseline anterior a B, sem seleção por igualdade temporal. Candidatos não são propriedade comprovada: cruzar com baseline, janela exclusiva e guardas de 03. Nenhum ID é escolhido automaticamente entre duplicados. Referências após confirmação esperadas 2/4/2; antes 0/0/0. Parcialidade, concorrência ou associações incorretas impedem retomada automática.

Metadados de gatilhos/constraints podem revelar estruturas adicionais; divergência exige parar sem alterar schema. A transação de 02 usa bloqueios de faixa/tabela conforme o plano do SQL Server; pode bloquear outros escritores temporariamente e só deve ser executada em janela controlada. Esses efeitos e o compilador SQL **não foram exercitados** nesta preparação. Sintaxe/execução SQL serão verificadas somente após nova autorização, nunca com tentativa contra outro banco.

## Procedimento futuro e ressalvas de correlação

Consultar [PROCEDIMENTO_EXECUCAO.md](PROCEDIMENTO_EXECUCAO.md): documento sem executor ou autorização de conexão. EVENTO_SEGURANCA não possui FK para OCORRENCIA. Os cinco IDs exigem confronto contra baseline anterior ao POST de B e janela exclusiva sem outros escritores. A flag de revisão não comprova vínculo pelo schema. Igualdade exata dos timestamps é verificação real pendente; 03 mostra a comparação e interrompe diante de divergência. Não atribuir eventos arbitrariamente nem alterar horários; parar e solicitar revisão manual. Nenhum SQL foi executado nesta correção.
