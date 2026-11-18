-- SOMENTE LEITURA, NAO EXECUTADO. Parametros @Autorizacao, @ServidorEsperado,
-- @BancoEsperado, @Marcador com os mesmos tipos de 01-preflight.sql.
IF @Autorizacao IS NULL OR @Autorizacao<>N'PM_PO_TASK007_SQL_LEITURA_APROVADO'
    THROW 51500, 'Leitura nao autorizada.', 1;
IF @ServidorEsperado IS NULL OR @BancoEsperado IS NULL OR @ServidorEsperado<>N'slayr\SQLEXPRESS' OR @BancoEsperado<>N'ZelaSP_Dev'
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),N'')<>@ServidorEsperado
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'')<>N'SQLEXPRESS'
 OR DB_NAME()<>@BancoEsperado OR ISNULL(CONVERT(nvarchar(32),DATABASEPROPERTYEX(DB_NAME(),'Status')),N'')<>N'ONLINE'
    THROW 51501, 'Destino divergente.', 1;
IF @Marcador IS NULL OR DATALENGTH(@Marcador)<>90
 OR LEFT(@Marcador,13) COLLATE Latin1_General_100_BIN2<>N'TASK007-DEMO-'
 OR SUBSTRING(@Marcador,14,32) COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9a-f]%'
    THROW 51502, 'Marcador invalido.', 1;
SELECT id,nome,subprefeitura FROM dbo.BAIRRO WHERE CHARINDEX(@Marcador,nome)>0 ORDER BY id;
SELECT id,nome FROM dbo.CATEGORIA_OCORRENCIA WHERE CHARINDEX(@Marcador,nome)>0 ORDER BY id;
SELECT id,bairro_id,nome,local FROM dbo.DISPOSITIVO WHERE CHARINDEX(@Marcador,nome)>0 ORDER BY id;
-- Recupera somente IDs/marcador para revisar HTTP ambiguo, sem exibir relato livre.
SELECT id,bairro_id,categoria_id,status,criada_em FROM dbo.OCORRENCIA WHERE CHARINDEX(@Marcador,descricao)>0 ORDER BY id;
SELECT id,ocorrencia_id,titulo,mime,DATALENGTH(dados) AS bytes,
 CONVERT(varchar(64),HASHBYTES('SHA2_256',dados),2) AS sha256
FROM dbo.IMAGEM_OCORRENCIA WHERE CHARINDEX(@Marcador,titulo)>0 ORDER BY id;
-- Snapshot de IDs/horarios, nao atribuicao causal: comparar com baseline capturado
-- ANTES de B, em janela exclusiva sem outros escritores. Nao selecionar por igualdade
-- de timestamps: ela depende de validacao real e nao demonstra vinculo por si so.
-- Sem FK evento->ocorrencia. Ambiguidade exige parada/revisao manual, nunca atribuicao
-- arbitraria ou alteracao de timestamps. Nao exibir campos livres de outros eventos.
SELECT e.id AS evento_snapshot_id,e.ocorrido_em
FROM dbo.EVENTO_SEGURANCA e
ORDER BY e.id;
-- Esperado apos referencias: 2/4/2. Antes: 0/0/0. Qualquer parcialidade/duplicacao
-- ou relacao divergente impede retomada; este arquivo nunca seleciona automaticamente
-- um ID entre duplicados nem autoriza UPDATE/novo POST.
