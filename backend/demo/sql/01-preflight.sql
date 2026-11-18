-- LEITURA SOMENTE. NAO EXECUTADO. Parametros obrigatorios, vinculados pelo executor revisado:
-- @Autorizacao nvarchar(100), @ServidorEsperado nvarchar(128), @BancoEsperado sysname,
-- @Marcador nvarchar(45). Nunca inferir destino nem reutilizar autorizacoes anteriores.
IF @Autorizacao <> N'PM_PO_TASK007_SQL_LEITURA_APROVADO' OR @Autorizacao IS NULL
    THROW 51000, 'Leitura SQL nao autorizada para esta etapa.', 1;
IF @ServidorEsperado <> N'slayr\SQLEXPRESS' OR @BancoEsperado <> N'ZelaSP_Dev'
    OR @ServidorEsperado IS NULL OR @BancoEsperado IS NULL
    THROW 51001, 'Destino esperado deve ser explicitamente o aprovado.', 1;
IF ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')),N'') <> @ServidorEsperado
    OR ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY('InstanceName')),N'') <> N'SQLEXPRESS'
    OR DB_NAME() <> @BancoEsperado OR ISNULL(CONVERT(nvarchar(32),DATABASEPROPERTYEX(DB_NAME(), 'Status')),N'') <> N'ONLINE'
    THROW 51002, 'Identidade ou estado do destino divergente.', 1;
IF @Marcador IS NULL OR DATALENGTH(@Marcador) <> 90
    OR LEFT(@Marcador,13) COLLATE Latin1_General_100_BIN2 <> N'TASK007-DEMO-'
    OR SUBSTRING(@Marcador,14,32) COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9a-f]%'
    THROW 51003, 'Marcador invalido.', 1;

SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) AS servidor,
       CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) AS instancia,
       DB_NAME() AS banco, DATABASEPROPERTYEX(DB_NAME(),'Status') AS estado,
       CONNECTIONPROPERTY('auth_scheme') AS autenticacao;
-- Autenticacao precisa ser confirmada como integrada (NTLM/KERBEROS); nao imprimir segredos.
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT t.name AS tabela, c.name AS coluna, ty.name AS tipo, c.max_length,
       c.precision, c.scale, c.is_nullable
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE t.name IN (N'BAIRRO',N'CATEGORIA_OCORRENCIA',N'OCORRENCIA',N'IMAGEM_OCORRENCIA',
 N'HISTORICO_STATUS',N'DISPOSITIVO',N'LEITURA_SENSOR',N'EVENTO_SEGURANCA',N'MENSAGEM_RISCO')
ORDER BY t.name,c.column_id;
SELECT OBJECT_NAME(parent_object_id) AS tabela, name, delete_referential_action_desc, is_disabled, is_not_trusted
FROM sys.foreign_keys WHERE parent_object_id IN
 (OBJECT_ID(N'dbo.OCORRENCIA'),OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA'),OBJECT_ID(N'dbo.HISTORICO_STATUS'),
  OBJECT_ID(N'dbo.DISPOSITIVO'),OBJECT_ID(N'dbo.LEITURA_SENSOR'),OBJECT_ID(N'dbo.MENSAGEM_RISCO'));
SELECT OBJECT_NAME(i.object_id) AS tabela,i.name,i.is_primary_key,i.is_unique,c.name AS coluna,ic.key_ordinal
FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
WHERE i.object_id IN (OBJECT_ID(N'dbo.BAIRRO'),OBJECT_ID(N'dbo.CATEGORIA_OCORRENCIA'),OBJECT_ID(N'dbo.OCORRENCIA'),
 OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA'),OBJECT_ID(N'dbo.HISTORICO_STATUS'),OBJECT_ID(N'dbo.DISPOSITIVO'),
 OBJECT_ID(N'dbo.LEITURA_SENSOR'),OBJECT_ID(N'dbo.EVENTO_SEGURANCA'),OBJECT_ID(N'dbo.MENSAGEM_RISCO'))
ORDER BY tabela,i.name,ic.key_ordinal;
SELECT OBJECT_NAME(parent_object_id) AS tabela,name,definition,is_disabled,is_not_trusted
FROM sys.check_constraints ORDER BY tabela,name;
SELECT OBJECT_NAME(parent_id) AS tabela,name,is_disabled,is_instead_of_trigger
FROM sys.triggers WHERE parent_id IN
 (OBJECT_ID(N'dbo.BAIRRO'),OBJECT_ID(N'dbo.CATEGORIA_OCORRENCIA'),OBJECT_ID(N'dbo.OCORRENCIA'),
  OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA'),OBJECT_ID(N'dbo.HISTORICO_STATUS'),OBJECT_ID(N'dbo.DISPOSITIVO'),
  OBJECT_ID(N'dbo.LEITURA_SENSOR'),OBJECT_ID(N'dbo.EVENTO_SEGURANCA'),OBJECT_ID(N'dbo.MENSAGEM_RISCO'));
-- Modelo nao define triggers: qualquer trigger habilitada exige parar e revisar efeitos.
SELECT N'BAIRRO' AS tabela,COUNT_BIG(*) AS quantidade FROM dbo.BAIRRO
UNION ALL SELECT N'CATEGORIA_OCORRENCIA',COUNT_BIG(*) FROM dbo.CATEGORIA_OCORRENCIA
UNION ALL SELECT N'OCORRENCIA',COUNT_BIG(*) FROM dbo.OCORRENCIA
UNION ALL SELECT N'HISTORICO_STATUS',COUNT_BIG(*) FROM dbo.HISTORICO_STATUS
UNION ALL SELECT N'IMAGEM_OCORRENCIA',COUNT_BIG(*) FROM dbo.IMAGEM_OCORRENCIA
UNION ALL SELECT N'DISPOSITIVO',COUNT_BIG(*) FROM dbo.DISPOSITIVO
UNION ALL SELECT N'LEITURA_SENSOR',COUNT_BIG(*) FROM dbo.LEITURA_SENSOR
UNION ALL SELECT N'EVENTO_SEGURANCA',COUNT_BIG(*) FROM dbo.EVENTO_SEGURANCA
UNION ALL SELECT N'MENSAGEM_RISCO',COUNT_BIG(*) FROM dbo.MENSAGEM_RISCO;
-- IDs e horarios, sem conteudo livre/pessoal, para diferenca antes/depois na janela exclusiva.
SELECT id,ocorrido_em FROM dbo.EVENTO_SEGURANCA ORDER BY id;
SELECT N'BAIRRO' AS tabela,COUNT_BIG(*) AS registros_com_marcador FROM dbo.BAIRRO WHERE CHARINDEX(@Marcador,nome)>0
UNION ALL SELECT N'CATEGORIA_OCORRENCIA',COUNT_BIG(*) FROM dbo.CATEGORIA_OCORRENCIA WHERE CHARINDEX(@Marcador,nome)>0
UNION ALL SELECT N'DISPOSITIVO',COUNT_BIG(*) FROM dbo.DISPOSITIVO WHERE CHARINDEX(@Marcador,nome)>0
UNION ALL SELECT N'OCORRENCIA',COUNT_BIG(*) FROM dbo.OCORRENCIA WHERE CHARINDEX(@Marcador,descricao)>0
UNION ALL SELECT N'IMAGEM_OCORRENCIA',COUNT_BIG(*) FROM dbo.IMAGEM_OCORRENCIA WHERE CHARINDEX(@Marcador,titulo)>0;
-- Esperado: ONLINE/integrada, InitialCreate 10.0.6, nove tabelas, sete FKs NO_ACTION,
-- nove PKs de dominio, sete indices FK (um unico), 14 CHECKs ativas/confiaveis,
-- tipos do modelo e zero registros do marcador ANTES da criacao. Qualquer divergencia: parar.
