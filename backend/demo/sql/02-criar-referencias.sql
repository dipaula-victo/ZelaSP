-- ESCRITA FUTURA, NAO EXECUTADA. Somente oito referencias NOVAS.
-- Parametros externos obrigatorios: @Autorizacao nvarchar(100), @ServidorEsperado nvarchar(128),
-- @BancoEsperado sysname, @Marcador nvarchar(45), @PreflightConferido bit, @JanelaExclusiva bit.
-- O executor deve vincular os parametros e salvar intencao EM_ENVIO antes de enviar uma unica vez.
IF @Autorizacao IS NULL OR @Autorizacao <> N'PM_PO_TASK007_REFERENCIAS_APROVADO'
    THROW 51100, 'Criacao de referencias depende de nova autorizacao especifica.', 1;
IF @PreflightConferido IS NULL OR @PreflightConferido<>1 OR @JanelaExclusiva IS NULL OR @JanelaExclusiva<>1
    THROW 51101, 'Pre-condicoes nao confirmadas.', 1;
IF @ServidorEsperado IS NULL OR @BancoEsperado IS NULL OR @ServidorEsperado<>N'slayr\SQLEXPRESS' OR @BancoEsperado<>N'ZelaSP_Dev'
    THROW 51102, 'Destino esperado deve ser explicito e aprovado.', 1;
IF ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),N'')<>@ServidorEsperado
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'')<>N'SQLEXPRESS'
 OR DB_NAME()<>@BancoEsperado OR ISNULL(CONVERT(nvarchar(32),DATABASEPROPERTYEX(DB_NAME(),'Status')),N'')<>N'ONLINE'
 OR CONNECTIONPROPERTY('auth_scheme') NOT IN (N'NTLM',N'KERBEROS') OR CONNECTIONPROPERTY('auth_scheme') IS NULL
    THROW 51103, 'Destino, estado ou autenticacao divergente.', 1;
IF @Marcador IS NULL OR DATALENGTH(@Marcador)<>90
 OR LEFT(@Marcador,13) COLLATE Latin1_General_100_BIN2<>N'TASK007-DEMO-'
 OR SUBSTRING(@Marcador,14,32) COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9a-f]%'
    THROW 51104, 'Marcador invalido.', 1;
IF @@TRANCOUNT<>0 THROW 51105, 'Nao executar dentro de transacao externa.', 1;
IF (SELECT COUNT_BIG(*) FROM dbo.__EFMigrationsHistory)<>1 OR NOT EXISTS
 (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261114191732_InitialCreate' AND ProductVersion=N'10.0.6')
    THROW 51106, 'Historico de migracao divergente; nao corrigir automaticamente.', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name IN
 (N'BAIRRO',N'CATEGORIA_OCORRENCIA',N'OCORRENCIA',N'IMAGEM_OCORRENCIA',N'HISTORICO_STATUS',
  N'DISPOSITIVO',N'LEITURA_SENSOR',N'EVENTO_SEGURANCA',N'MENSAGEM_RISCO'))<>9
    THROW 51112, 'Objetos de dominio divergentes: nao inserir em views ou synonyms.', 1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE is_disabled=0 AND parent_id IN
 (OBJECT_ID(N'dbo.BAIRRO'),OBJECT_ID(N'dbo.CATEGORIA_OCORRENCIA'),OBJECT_ID(N'dbo.OCORRENCIA'),
  OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA'),OBJECT_ID(N'dbo.HISTORICO_STATUS'),OBJECT_ID(N'dbo.DISPOSITIVO'),
  OBJECT_ID(N'dbo.LEITURA_SENSOR'),OBJECT_ID(N'dbo.EVENTO_SEGURANCA'),OBJECT_ID(N'dbo.MENSAGEM_RISCO')))
    THROW 51113, 'Trigger habilitada: efeitos nao revisados impedem o lote.', 1;

DECLARE @Bairros TABLE(id int NOT NULL,nome nvarchar(120) NOT NULL);
DECLARE @Categorias TABLE(id int NOT NULL,nome nvarchar(120) NOT NULL);
DECLARE @Dispositivos TABLE(id int NOT NULL,nome nvarchar(120) NOT NULL,bairro_id int NOT NULL);
DECLARE @Centro nvarchar(120)=N'Teste Centro ['+@Marcador+N']';
DECLARE @Norte nvarchar(120)=N'Teste Norte ['+@Marcador+N']';
-- HOLDLOCK/UPDLOCK serializam a verificacao e criacao cooperativa, sem criar indice/schema.
-- Podem bloquear outras escritas durante a transacao curta: exigir janela exclusiva.
BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS(SELECT 1 FROM dbo.BAIRRO WITH(UPDLOCK,HOLDLOCK) WHERE CHARINDEX(@Marcador,nome)>0)
 OR EXISTS(SELECT 1 FROM dbo.CATEGORIA_OCORRENCIA WITH(UPDLOCK,HOLDLOCK) WHERE CHARINDEX(@Marcador,nome)>0)
 OR EXISTS(SELECT 1 FROM dbo.DISPOSITIVO WITH(UPDLOCK,HOLDLOCK) WHERE CHARINDEX(@Marcador,nome)>0)
 OR EXISTS(SELECT 1 FROM dbo.OCORRENCIA WHERE CHARINDEX(@Marcador,descricao)>0)
 OR EXISTS(SELECT 1 FROM dbo.IMAGEM_OCORRENCIA WHERE CHARINDEX(@Marcador,titulo)>0)
    THROW 51107, 'Marcador ja existe: interromper e inspecionar, sem recriar ou reparar.', 1;
 INSERT INTO dbo.BAIRRO(nome,subprefeitura)
 OUTPUT inserted.id,inserted.nome INTO @Bairros(id,nome)
 VALUES(@Centro,N'Subprefeitura exclusivamente fictícia'),(@Norte,N'Subprefeitura exclusivamente fictícia');
 IF (SELECT COUNT(*) FROM @Bairros)<>2 THROW 51108, 'Quantidade inesperada de bairros.', 1;
 INSERT INTO dbo.CATEGORIA_OCORRENCIA(nome)
 OUTPUT inserted.id,inserted.nome INTO @Categorias(id,nome)
 VALUES(N'Lixeira cheia ['+@Marcador+N']'),(N'Descarte irregular ['+@Marcador+N']'),
       (N'Buraco ['+@Marcador+N']'),(N'Iluminação ['+@Marcador+N']');
 IF (SELECT COUNT(*) FROM @Categorias)<>4 THROW 51109, 'Quantidade inesperada de categorias.', 1;
 DECLARE @CentroId int=(SELECT id FROM @Bairros WHERE nome=@Centro);
 DECLARE @NorteId int=(SELECT id FROM @Bairros WHERE nome=@Norte);
 IF @CentroId IS NULL OR @NorteId IS NULL THROW 51110, 'IDs dinamicos nao obtidos.', 1;
 INSERT INTO dbo.DISPOSITIVO(bairro_id,nome,local)
 OUTPUT inserted.id,inserted.nome,inserted.bairro_id INTO @Dispositivos(id,nome,bairro_id)
 VALUES(@CentroId,N'Sensor fictício com leitura ['+@Marcador+N']',N'Ponto exclusivamente fictício Centro'),
       (@NorteId,N'Sensor fictício sem leitura ['+@Marcador+N']',N'Ponto exclusivamente fictício Norte');
 IF (SELECT COUNT(*) FROM @Dispositivos)<>2 THROW 51111, 'Quantidade inesperada de dispositivos.', 1;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 -- Apenas a transacao propria ainda nao confirmada; nao apaga registros confirmados.
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
SELECT @Marcador AS marcador,CASE WHEN nome=@Centro THEN N'centro' ELSE N'norte' END AS chave,id,nome FROM @Bairros ORDER BY chave;
SELECT @Marcador AS marcador,CASE nome
 WHEN N'Lixeira cheia ['+@Marcador+N']' THEN N'lixeira'
 WHEN N'Descarte irregular ['+@Marcador+N']' THEN N'descarte'
 WHEN N'Buraco ['+@Marcador+N']' THEN N'buraco' ELSE N'iluminacao' END AS chave,id,nome FROM @Categorias ORDER BY chave;
SELECT @Marcador AS marcador,CASE WHEN bairro_id=@CentroId THEN N'comLeitura' ELSE N'semLeitura' END AS chave,id,nome,bairro_id FROM @Dispositivos ORDER BY chave;
-- Se perder a resposta apos COMMIT, nao repetir: marcar AMBIGUA, executar apenas leitura
-- autorizada de recuperacao e submeter ao PM. Nenhuma etapa HTTP e acionada por este arquivo.
