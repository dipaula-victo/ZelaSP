-- SOMENTE LEITURA. NAO EXECUTADO. Parametros vinculados pelo executor revisado:
-- @Autorizacao nvarchar(100), @ServidorEsperado nvarchar(128), @BancoEsperado sysname,
-- @Marcador nvarchar(45), @EventosIsoladosConferidos bit,
-- @BairroCentroId/@BairroNorteId, @CategoriaLixeiraId/@CategoriaDescarteId/@CategoriaBuracoId/@CategoriaIluminacaoId,
-- @OcorrenciaAId/@OcorrenciaBId/@OcorrenciaCId, @ImagemPngId/@ImagemJpegId,
-- @DispositivoComLeituraId/@DispositivoSemLeituraId e @Evento1Id..@Evento5Id: todos int, dinamicos do manifesto.
IF @Autorizacao IS NULL OR @Autorizacao<>N'PM_PO_TASK007_SQL_LEITURA_APROVADO'
 THROW 51300, 'Leitura nao autorizada.', 1;
IF @ServidorEsperado IS NULL OR @BancoEsperado IS NULL OR @ServidorEsperado<>N'slayr\SQLEXPRESS' OR @BancoEsperado<>N'ZelaSP_Dev'
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),N'')<>@ServidorEsperado
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'')<>N'SQLEXPRESS'
 OR DB_NAME()<>@BancoEsperado OR ISNULL(CONVERT(nvarchar(32),DATABASEPROPERTYEX(DB_NAME(),'Status')),N'')<>N'ONLINE'
 THROW 51301, 'Destino divergente.', 1;
IF @Marcador IS NULL OR DATALENGTH(@Marcador)<>90
 OR LEFT(@Marcador,13) COLLATE Latin1_General_100_BIN2<>N'TASK007-DEMO-'
 OR SUBSTRING(@Marcador,14,32) COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9a-f]%'
 THROW 51302, 'Marcador invalido.', 1;
IF (SELECT COUNT(*) FROM dbo.BAIRRO WHERE CHARINDEX(@Marcador,nome)>0)<>2
 OR (SELECT COUNT(*) FROM dbo.CATEGORIA_OCORRENCIA WHERE CHARINDEX(@Marcador,nome)>0)<>4
 OR (SELECT COUNT(*) FROM dbo.DISPOSITIVO WHERE CHARINDEX(@Marcador,nome)>0)<>2
 OR (SELECT COUNT(*) FROM dbo.OCORRENCIA WHERE CHARINDEX(@Marcador,descricao)>0)<>3
 OR (SELECT COUNT(*) FROM dbo.IMAGEM_OCORRENCIA WHERE CHARINDEX(@Marcador,titulo)>0)<>2
 THROW 51310, 'Marcador com faltas/duplicacoes: nao selecionar apenas um subconjunto conveniente.', 1;
IF (SELECT COUNT(*) FROM dbo.BAIRRO WHERE id IN(@BairroCentroId,@BairroNorteId) AND CHARINDEX(@Marcador,nome)>0)<>2
 OR (SELECT COUNT(*) FROM dbo.CATEGORIA_OCORRENCIA WHERE id IN(@CategoriaLixeiraId,@CategoriaDescarteId,@CategoriaBuracoId,@CategoriaIluminacaoId) AND CHARINDEX(@Marcador,nome)>0)<>4
 OR (SELECT COUNT(*) FROM dbo.OCORRENCIA WHERE id IN(@OcorrenciaAId,@OcorrenciaBId,@OcorrenciaCId) AND CHARINDEX(@Marcador,descricao)>0)<>3
 OR (SELECT COUNT(*) FROM dbo.DISPOSITIVO WHERE id IN(@DispositivoComLeituraId,@DispositivoSemLeituraId) AND CHARINDEX(@Marcador,nome)>0)<>2
 OR (SELECT COUNT(*) FROM dbo.IMAGEM_OCORRENCIA WHERE id IN(@ImagemPngId,@ImagemJpegId) AND ocorrencia_id=@OcorrenciaBId AND CHARINDEX(@Marcador,titulo)>0)<>2
 THROW 51303, 'IDs ausentes, duplicados ou sem propriedade do lote: nao mostrar conteudo de outros registros.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE id=@OcorrenciaAId AND bairro_id=@BairroCentroId AND categoria_id=@CategoriaDescarteId)
 OR NOT EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE id=@OcorrenciaBId AND bairro_id=@BairroCentroId AND categoria_id=@CategoriaLixeiraId)
 OR NOT EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE id=@OcorrenciaCId AND bairro_id=@BairroNorteId AND categoria_id=@CategoriaBuracoId)
 OR NOT EXISTS(SELECT 1 FROM dbo.DISPOSITIVO WHERE id=@DispositivoComLeituraId AND bairro_id=@BairroCentroId)
 OR NOT EXISTS(SELECT 1 FROM dbo.DISPOSITIVO WHERE id=@DispositivoSemLeituraId AND bairro_id=@BairroNorteId)
 THROW 51304, 'Associacoes divergentes do plano.', 1;

-- R1/R2: 2 bairros, 4 categorias; nomes devem coincidir exatamente com o plano + marcador.
SELECT id,nome,subprefeitura FROM dbo.BAIRRO WHERE id IN(@BairroCentroId,@BairroNorteId) ORDER BY nome,id;
SELECT id,nome FROM dbo.CATEGORIA_OCORRENCIA WHERE id IN(@CategoriaLixeiraId,@CategoriaDescarteId,@CategoriaBuracoId,@CategoriaIluminacaoId) ORDER BY nome,id;
-- R3: 3 ocorrencias APP; descricao B deve ser igual ao esperado protegido, sem originais.
SELECT id,bairro_id,categoria_id,descricao,status,origem,criada_em FROM dbo.OCORRENCIA
WHERE id IN(@OcorrenciaAId,@OcorrenciaBId,@OcorrenciaCId) ORDER BY id;
-- R4: 0/1/2 historicos; total 3, transicoes e executor tecnico esperados.
SELECT id,ocorrencia_id,status_anterior,status_novo,alterado_em,executor FROM dbo.HISTORICO_STATUS
WHERE ocorrencia_id IN(@OcorrenciaAId,@OcorrenciaBId,@OcorrenciaCId) ORDER BY ocorrencia_id,alterado_em,id;
-- R5: 2 imagens; bytes nao sao impressos. Hash hexadecimal pode estar em caixa alta.
SELECT id,ocorrencia_id,titulo,nome_arquivo,mime,DATALENGTH(dados) AS tamanho_bytes,
 CONVERT(varchar(64),HASHBYTES('SHA2_256',dados),2) AS sha256,data_utc,rm_lider,executor
FROM dbo.IMAGEM_OCORRENCIA WHERE id IN(@ImagemPngId,@ImagemJpegId) ORDER BY id;
-- R6: confirmar tipo fisico, nao somente mapeamento EF.
SELECT c.name AS coluna,t.name AS tipo,c.max_length,c.precision,c.scale,c.is_nullable
FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
WHERE c.object_id=OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA') AND c.name IN(N'dados',N'titulo',N'data_utc');
-- R7: 3 leituras do dispositivo proprio, false/true/true.
SELECT id,dispositivo_id,nivel_percentual,lida_em,
 CAST(CASE WHEN nivel_percentual>=90.00 THEN 1 ELSE 0 END AS bit) AS nivel_critico
FROM dbo.LEITURA_SENSOR WHERE dispositivo_id IN(@DispositivoComLeituraId,@DispositivoSemLeituraId)
ORDER BY dispositivo_id,lida_em,id;
-- R8: 2 dispositivos; o segundo deve ter NULL (nao zero).
WITH R AS (
 SELECT id,dispositivo_id,nivel_percentual,lida_em,
 ROW_NUMBER() OVER(PARTITION BY dispositivo_id ORDER BY lida_em DESC,id DESC) AS ordem
 FROM dbo.LEITURA_SENSOR WHERE dispositivo_id IN(@DispositivoComLeituraId,@DispositivoSemLeituraId)
)
SELECT d.id,d.bairro_id,d.nome,d.local,r.id AS ultima_leitura_id,r.nivel_percentual,r.lida_em
FROM dbo.DISPOSITIVO d LEFT JOIN R r ON r.dispositivo_id=d.id AND r.ordem=1
WHERE d.id IN(@DispositivoComLeituraId,@DispositivoSemLeituraId) ORDER BY d.nome,d.id;

-- R9: EVENTO_SEGURANCA nao possui FK para OCORRENCIA nem marcador do lote.
-- Correlacao exclusivamente operacional, nunca uma relacao garantida pelo schema.
-- Antes de vincular os cinco IDs: confrontar o snapshot anterior a criacao de B
-- com o posterior, em janela exclusiva SEM outros escritores. Nenhum ID escolhido
-- pode constar no baseline. A flag abaixo registra revisao humana, nao comprova causalidade.
-- Igualdade de timestamps depende de validacao real; nao presume propriedade.
-- Divergencia/ambiguidade: interromper e revisar manualmente. Nunca atribuir eventos
-- arbitrariamente nem alterar timestamps para satisfazer qualquer verificacao.
IF @EventosIsoladosConferidos IS NULL OR @EventosIsoladosConferidos<>1
 THROW 51305, 'Correlacao operacional dos eventos ainda nao revisada.', 1;
IF (SELECT COUNT(*) FROM dbo.EVENTO_SEGURANCA WHERE id IN(@Evento1Id,@Evento2Id,@Evento3Id,@Evento4Id,@Evento5Id)
 AND tipo_evento COLLATE Latin1_General_100_BIN2=N'PII_DETECTADO' AND DATALENGTH(tipo_evento)=26
 AND ((tipo_dado COLLATE Latin1_General_100_BIN2=N'CPF' AND DATALENGTH(tipo_dado)=6)
   OR (tipo_dado COLLATE Latin1_General_100_BIN2=N'TELEFONE' AND DATALENGTH(tipo_dado)=16)
   OR (tipo_dado COLLATE Latin1_General_100_BIN2 IN(N'EMAIL',N'SENHA',N'SAUDE') AND DATALENGTH(tipo_dado)=10))
 AND origem=N'API_OCORRENCIAS' AND destino=N'BANCO' AND acao=N'MASCARADO')<>5
 OR (SELECT COUNT(DISTINCT tipo_dado COLLATE Latin1_General_100_BIN2) FROM dbo.EVENTO_SEGURANCA
 WHERE id IN(@Evento1Id,@Evento2Id,@Evento3Id,@Evento4Id,@Evento5Id))<>5
 THROW 51306, 'Eventos nao correspondem a expectativa estatica; nao atribuir propriedade automaticamente.', 1;
-- Expor a comparacao real, sem selecionar IDs pela igualdade dos horarios.
SELECT e.id,e.tipo_evento,e.tipo_dado,e.ocorrido_em,o.criada_em,
 CAST(CASE WHEN e.ocorrido_em=o.criada_em THEN 1 ELSE 0 END AS bit) AS timestamps_iguais
FROM dbo.EVENTO_SEGURANCA e CROSS JOIN dbo.OCORRENCIA o
WHERE o.id=@OcorrenciaBId AND e.id IN(@Evento1Id,@Evento2Id,@Evento3Id,@Evento4Id,@Evento5Id)
ORDER BY e.ocorrido_em,e.id;
IF EXISTS(SELECT 1 FROM dbo.EVENTO_SEGURANCA e CROSS JOIN dbo.OCORRENCIA o
 WHERE o.id=@OcorrenciaBId AND e.id IN(@Evento1Id,@Evento2Id,@Evento3Id,@Evento4Id,@Evento5Id)
 AND e.ocorrido_em<>o.criada_em)
 THROW 51311, 'Timestamps divergentes: interromper para revisao manual, sem alterar datas ou atribuir eventos.', 1;
SELECT COUNT_BIG(*) AS mensagens_do_lote FROM dbo.MENSAGEM_RISCO
WHERE evento_seguranca_id IN(@Evento1Id,@Evento2Id,@Evento3Id,@Evento4Id,@Evento5Id);

-- R10: orfaos globais apenas agregados, sem conteudo de registros anteriores.
SELECT N'OCORRENCIA_BAIRRO' AS relacao,COUNT_BIG(*) AS orfaos FROM dbo.OCORRENCIA o LEFT JOIN dbo.BAIRRO b ON b.id=o.bairro_id WHERE b.id IS NULL
UNION ALL SELECT N'OCORRENCIA_CATEGORIA',COUNT_BIG(*) FROM dbo.OCORRENCIA o LEFT JOIN dbo.CATEGORIA_OCORRENCIA c ON c.id=o.categoria_id WHERE c.id IS NULL
UNION ALL SELECT N'IMAGEM_OCORRENCIA',COUNT_BIG(*) FROM dbo.IMAGEM_OCORRENCIA i LEFT JOIN dbo.OCORRENCIA o ON o.id=i.ocorrencia_id WHERE o.id IS NULL
UNION ALL SELECT N'HISTORICO_OCORRENCIA',COUNT_BIG(*) FROM dbo.HISTORICO_STATUS h LEFT JOIN dbo.OCORRENCIA o ON o.id=h.ocorrencia_id WHERE o.id IS NULL
UNION ALL SELECT N'DISPOSITIVO_BAIRRO',COUNT_BIG(*) FROM dbo.DISPOSITIVO d LEFT JOIN dbo.BAIRRO b ON b.id=d.bairro_id WHERE b.id IS NULL
UNION ALL SELECT N'LEITURA_DISPOSITIVO',COUNT_BIG(*) FROM dbo.LEITURA_SENSOR l LEFT JOIN dbo.DISPOSITIVO d ON d.id=l.dispositivo_id WHERE d.id IS NULL
UNION ALL SELECT N'MENSAGEM_EVENTO',COUNT_BIG(*) FROM dbo.MENSAGEM_RISCO m LEFT JOIN dbo.EVENTO_SEGURANCA e ON e.id=m.evento_seguranca_id WHERE e.id IS NULL;
SELECT evento_seguranca_id,COUNT_BIG(*) AS quantidade FROM dbo.MENSAGEM_RISCO GROUP BY evento_seguranca_id HAVING COUNT_BIG(*)>1;

-- R11: deltas devem ser comparados ao baseline, nao a totais historicos.
SELECT N'BAIRRO' AS tabela,COUNT_BIG(*) AS quantidade FROM dbo.BAIRRO
UNION ALL SELECT N'CATEGORIA_OCORRENCIA',COUNT_BIG(*) FROM dbo.CATEGORIA_OCORRENCIA
UNION ALL SELECT N'OCORRENCIA',COUNT_BIG(*) FROM dbo.OCORRENCIA
UNION ALL SELECT N'HISTORICO_STATUS',COUNT_BIG(*) FROM dbo.HISTORICO_STATUS
UNION ALL SELECT N'IMAGEM_OCORRENCIA',COUNT_BIG(*) FROM dbo.IMAGEM_OCORRENCIA
UNION ALL SELECT N'DISPOSITIVO',COUNT_BIG(*) FROM dbo.DISPOSITIVO
UNION ALL SELECT N'LEITURA_SENSOR',COUNT_BIG(*) FROM dbo.LEITURA_SENSOR
UNION ALL SELECT N'EVENTO_SEGURANCA',COUNT_BIG(*) FROM dbo.EVENTO_SEGURANCA
UNION ALL SELECT N'MENSAGEM_RISCO',COUNT_BIG(*) FROM dbo.MENSAGEM_RISCO;
-- Guardas finais: relatorio parcial nao deve ser interpretado como verificacao concluida.
IF NOT EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE id=@OcorrenciaAId AND status='ABERTA' AND origem='APP')
 OR NOT EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE id=@OcorrenciaBId AND status='EM_ANALISE' AND origem='APP')
 OR NOT EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE id=@OcorrenciaCId AND status='RESOLVIDA' AND origem='APP')
 OR (SELECT COUNT(*) FROM dbo.HISTORICO_STATUS WHERE ocorrencia_id=@OcorrenciaAId)<>0
 OR (SELECT COUNT(*) FROM dbo.HISTORICO_STATUS WHERE ocorrencia_id=@OcorrenciaBId)<>1
 OR (SELECT COUNT(*) FROM dbo.HISTORICO_STATUS WHERE ocorrencia_id=@OcorrenciaCId)<>2
 OR NOT EXISTS(SELECT 1 FROM dbo.HISTORICO_STATUS WHERE ocorrencia_id=@OcorrenciaBId AND status_anterior='ABERTA' AND status_novo='EM_ANALISE')
 OR NOT EXISTS(SELECT 1 FROM dbo.HISTORICO_STATUS WHERE ocorrencia_id=@OcorrenciaCId AND status_anterior='ABERTA' AND status_novo='EM_ANALISE')
 OR NOT EXISTS(SELECT 1 FROM dbo.HISTORICO_STATUS WHERE ocorrencia_id=@OcorrenciaCId AND status_anterior='EM_ANALISE' AND status_novo='RESOLVIDA')
 THROW 51307, 'Estado ou historico final divergente.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.IMAGEM_OCORRENCIA WHERE id=@ImagemPngId AND mime=N'image/png' AND DATALENGTH(dados)=120
 AND CONVERT(varchar(64),HASHBYTES('SHA2_256',dados),2)='A5C8F1A1213865E9EF89420F46E08085AA9E1C94A1723BF77218D44865F10575'
 AND rm_lider=N'554787' AND executor=N'VictorHugoDePaula_RM554787')
 OR NOT EXISTS(SELECT 1 FROM dbo.IMAGEM_OCORRENCIA WHERE id=@ImagemJpegId AND mime=N'image/jpeg' AND DATALENGTH(dados)=634
 AND CONVERT(varchar(64),HASHBYTES('SHA2_256',dados),2)='9D8E6351C67193EBE9C11DE358C8E54F127521AA511D6A1DC6003F64877E4860'
 AND rm_lider=N'554787' AND executor=N'VictorHugoDePaula_RM554787')
 THROW 51308, 'Imagem ou metadados divergem das amostras aprovadas.', 1;
IF (SELECT COUNT(*) FROM dbo.LEITURA_SENSOR WHERE dispositivo_id=@DispositivoComLeituraId)<>3
 OR (SELECT COUNT(*) FROM dbo.LEITURA_SENSOR WHERE dispositivo_id=@DispositivoSemLeituraId)<>0
 OR (SELECT COUNT(DISTINCT nivel_percentual) FROM dbo.LEITURA_SENSOR WHERE dispositivo_id=@DispositivoComLeituraId AND nivel_percentual IN(89.99,90.00,90.01))<>3
 OR EXISTS(SELECT 1 FROM dbo.MENSAGEM_RISCO WHERE evento_seguranca_id IN(@Evento1Id,@Evento2Id,@Evento3Id,@Evento4Id,@Evento5Id))
 THROW 51309, 'Leituras ou mensagens divergem do plano.', 1;
-- Cada resultado tem de ser confrontado com manifesto/roteiro. Divergencia = parada,
-- nunca justificativa para alterar registro, data ou executar novamente um POST.
