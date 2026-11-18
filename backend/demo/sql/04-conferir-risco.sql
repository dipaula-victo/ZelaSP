-- SOMENTE LEITURA, NAO EXECUTADO. Parametros @Autorizacao nvarchar(100),
-- @ServidorEsperado nvarchar(128), @BancoEsperado sysname,
-- @InicioUtc/@FimUtc datetime2, com MESMA janela/escala efetiva de parametros do EF.
-- Nao recalcular "agora" nem filtrar somente pelo lote: o endpoint e global.
IF @Autorizacao IS NULL OR @Autorizacao<>N'PM_PO_TASK007_SQL_LEITURA_APROVADO'
 THROW 51400, 'Leitura nao autorizada.', 1;
IF @ServidorEsperado IS NULL OR @BancoEsperado IS NULL OR @ServidorEsperado<>N'slayr\SQLEXPRESS' OR @BancoEsperado<>N'ZelaSP_Dev'
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),N'')<>@ServidorEsperado
 OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'')<>N'SQLEXPRESS'
 OR DB_NAME()<>@BancoEsperado OR ISNULL(CONVERT(nvarchar(32),DATABASEPROPERTYEX(DB_NAME(),'Status')),N'')<>N'ONLINE'
 THROW 51401, 'Destino divergente.', 1;
IF @InicioUtc IS NULL OR @FimUtc IS NULL OR @InicioUtc<>DATEADD(day,-7,@FimUtc)
 THROW 51402, 'Janela explicita de sete dias exigida.', 1;
WITH Validos AS (
 SELECT e.tipo_evento COLLATE Latin1_General_100_BIN2 AS tipo,
        e.tipo_dado COLLATE Latin1_General_100_BIN2 AS categoria,
        CASE WHEN e.tipo_evento=N'VAZAMENTO_BLOQUEADO' THEN 15 ELSE p.peso END AS peso
 FROM dbo.EVENTO_SEGURANCA e
 JOIN (VALUES(N'CPF',6),(N'TELEFONE',3),(N'EMAIL',3),(N'SENHA',10),(N'SAUDE',8)) p(categoria,peso)
   ON e.tipo_dado COLLATE Latin1_General_100_BIN2=p.categoria AND DATALENGTH(e.tipo_dado)=DATALENGTH(p.categoria)
 WHERE e.ocorrido_em>=@InicioUtc AND e.ocorrido_em<=@FimUtc
 AND ((e.tipo_evento COLLATE Latin1_General_100_BIN2=N'PII_DETECTADO' AND DATALENGTH(e.tipo_evento)=26)
   OR (e.tipo_evento COLLATE Latin1_General_100_BIN2=N'VAZAMENTO_BLOQUEADO' AND DATALENGTH(e.tipo_evento)=38))
), Agregado AS (
 SELECT COUNT_BIG(CASE WHEN tipo=N'PII_DETECTADO' AND categoria=N'CPF' THEN 1 END) AS cpf,
 COUNT_BIG(CASE WHEN tipo=N'PII_DETECTADO' AND categoria=N'TELEFONE' THEN 1 END) AS telefone,
 COUNT_BIG(CASE WHEN tipo=N'PII_DETECTADO' AND categoria=N'EMAIL' THEN 1 END) AS email,
 COUNT_BIG(CASE WHEN tipo=N'PII_DETECTADO' AND categoria=N'SENHA' THEN 1 END) AS senha,
 COUNT_BIG(CASE WHEN tipo=N'PII_DETECTADO' AND categoria=N'SAUDE' THEN 1 END) AS saude,
 COUNT_BIG(CASE WHEN tipo=N'VAZAMENTO_BLOQUEADO' THEN 1 END) AS vazamentosBloqueados,
 COALESCE(SUM(CONVERT(decimal(38,0),peso)),0) AS pontuacao_bruta FROM Validos
), Resultado AS (
 SELECT *,CASE WHEN pontuacao_bruta>=100 THEN 100 ELSE CONVERT(int,pontuacao_bruta) END AS pontuacao FROM Agregado
)
SELECT @InicioUtc AS inicioUtc,@FimUtc AS fimUtc,cpf,telefone,email,senha,saude,vazamentosBloqueados,pontuacao_bruta,pontuacao,
 CASE WHEN pontuacao<20 THEN N'BAIXO' WHEN pontuacao<50 THEN N'MEDIO' ELSE N'ALTO' END AS nivel
FROM Resultado;
-- Esperado: igualdade com as contagens/pontos/nivel do GET capturado, mesma janela.
-- Cinco eventos novos somam 30 brutos, mas o total pode conter eventos anteriores/saturacao.
-- Nao seleciona IDs, relatos ou campos livres da auditoria e nao grava MENSAGEM_RISCO.
