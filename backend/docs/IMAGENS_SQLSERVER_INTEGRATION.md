# TASK-003 — Procedimento e resultado de imagens no SQL Server

**Histórico: o bloco PowerShell integral revisado na conversa foi executado uma única vez, com exit code 0, sob autorização específica da TASK-003. Os exemplos abaixo foram preparados em etapa anterior; não representam autorização de repetição.** A autorização anterior da TASK-002 foi consumida pelo teste de ocorrências e não pode ser reutilizada. Este documento não habilita qualquer teste ou seed.

## Escopo proposto para autorização específica

Destino exclusivo `.\SQLEXPRESS` / `ZelaSP_Dev`, servidor efetivo `slayr\SQLEXPRESS`, Windows integrada. Antes de qualquer escrita, apresentar os comandos completos para revisão e conferir por SELECT: identidade, ONLINE/acesso, migração `20261114191732_InitialCreate`, modelo sem diferenças e contagens atuais das tabelas envolvidas. Não reutilizar contagens históricas como estado atual, nem consultar conteúdo pessoal.

Revisar e autorizar um lote fictício exclusivo com marcador próprio:

- Um bairro e uma categoria fictícios novos, sem alterar os anteriores.
- Uma ocorrência fictícia nova, criada pelo serviço existente em ABERTA/APP.
- Duas imagens mínimas próprias (um pixel PNG e um JPEG), associadas somente a essa ocorrência, com título identificando o lote.
- Executor selecionado da configuração oficial, RM do líder fornecido pelo servidor. Não habilitar o teste SQL de TASK-002 para esse fim.

Se aprovado integralmente, o lote confirmaria cinco novos registros: bairro, categoria, ocorrência e duas imagens. A permanência deve ser expressamente autorizada; não prever exclusão ou limpeza automática. HISTORICO_STATUS e todos os registros anteriores devem permanecer intactos. Nenhuma migração, mudança de schema ou configuração global é necessária.

## Evidência a coletar após autorização

1. Guardas antes das escritas e contagens anteriores.
2. Criação controlada das referências/ocorrência fictícias e seus IDs.
3. Upload dos dois arquivos pelo endpoint POST /api/imagens; HTTP 201 e Location de cada um.
4. Metadados obtidos por GET e por DbContext independente: FK, título, nome seguro, MIME, tamanho, data UTC, RM do líder e executor.
5. Recuperação GET /conteudo comparada byte a byte (ou por hash e comprimento) aos arquivos enviados, com MIME correto. Não imprimir bytes em logs.
6. Verificação somente por leitura dos registros do lote: Dados em varbinary(max), tamanho correto, associação íntegra, ocorrência ainda ABERTA e nenhum histórico novo.
7. Contagens posteriores com deltas previstos (+1 bairro, +1 categoria, +1 ocorrência, +2 imagens, +0 históricos), IDs/marcador e registros mantidos.

Em caso de divergência ou falha, interromper e preservar o estado para revisão, sem rollback manual, DROP, TRUNCATE, DELETE ou reparo automático. O planejamento de eventual teste automatizado SQL Server de imagens deve ser revisto separadamente antes de implementação/execução. A prova SQLite local não é apresentada como prova específica do provider SQL Server.

## Pré-validação de leitura — 16/11/2026

Conexão efetivamente verificada: servidor slayr\SQLEXPRESS, instância SQLEXPRESS, banco ZelaSP_Dev, Windows integrada/NTLM, ONLINE, HAS_DBACCESS=1. Histórico com um registro `20261114191732_InitialCreate`, ProductVersion `10.0.6`. EF Core sem alterações pendentes no modelo. Foram lidos apenas identidade, estado, migrações e contagens, sem conteúdo dos registros.

| Tabela | Contagem atual | Delta proposto | Total esperado se não houver outras alterações |
|---|---:|---:|---:|
| BAIRRO | 1 | +1 | 2 |
| CATEGORIA_OCORRENCIA | 1 | +1 | 2 |
| OCORRENCIA | 1 | +1 | 2 |
| IMAGEM_OCORRENCIA | 0 | +2 | 2 |
| HISTORICO_STATUS | 1 | 0 | 1 |

Essas contagens são evidência histórica desta inspeção, não autorização nem garantia do estado no momento futuro. Conferir novamente antes de escrever; qualquer divergência exige nova orientação.

### Projeção tamanhoBytes

A expressão real do ImagensStore foi capturada em um diagnóstico offline com o provider SQL Server 10.0.6, sem duplicar a projeção de produção. `ToQueryString` gerou:

```sql
SELECT [i].[id], [i].[ocorrencia_id], [i].[titulo], [i].[nome_arquivo], [i].[mime],
       CAST(DATALENGTH([i].[dados]) AS int), [i].[data_utc], [i].[rm_lider], [i].[executor]
FROM [IMAGEM_OCORRENCIA] AS [i]
WHERE [i].[id] = 2147483647
```

O ID acima foi somente um literal de diagnóstico para gerar SQL, nunca executado. A expressão foi capturada antes do terminal SingleOrDefault; o foco da inspeção é a projeção de colunas/tipo, não a execução de uma consulta de detalhe. Uma guarda de conexão confirmou zero tentativas de abertura. `Dados.Length` é traduzido para DATALENGTH e convertido para int, compatível com tamanhoBytes/int32 e limite de 2 MiB; os bytes não são selecionados como conteúdo no DTO de metadados. A aplicação de UTC ao DateTime ocorre no topo da projeção cliente.

Diagnóstico reproduzível (sem conexão SQL):

```powershell
dotnet test backend/ZelaSP.sln --no-build --no-restore --filter 'FullyQualifiedName=ZelaSP.Tests.ImagensSqlServerProjectionTests.ProjecaoRealDeMetadadosTraduzTamanhoNoSqlServerSemAbrirConexao' --logger 'console;verbosity=detailed'
```

Isso confirma tradução pelo provider instalado, não leitura/materialização de imagem persistida; como há zero imagens e a inspeção atual se limita a contagens, essa última prova fica para a execução autorizada. Referência: [mapeamentos binários do provider SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/functions).

### Origem e identificação das fixtures

Fonte única: `backend/tests/ZelaSP.Tests/ImagemFixtures.cs`. PNG e JPEG foram gerados localmente na implementação da TASK-003 por System.Drawing/Bitmap, um pixel magenta de 1×1, com codificação PNG/JPEG em MemoryStream. Não vieram de fotografias, usuários ou downloads; a origem está documentada no código e em VALIDACAO.md. Os testes não dependem de System.Drawing para executar: usam as constantes Base64, decodificadas para os bytes originais.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| PNG | 120 | `a5c8f1a1213865e9ef89420f46e08085aa9e1c94a1723bf77218d44865f10575` |
| JPEG | 634 | `9d8e6351c67193ebe9c11de358c8e54f127521aa511d6a1dc6003f64877e4860` |

Tamanhos/hashes foram calculados nesta pré-validação sem upload ou escrita SQL. Não usar o payload com padding de 2 MiB do teste de fronteira para a prova real: o lote futuro usa exclusivamente esses dois pixels mínimos.

## Roteiro exato preparado — NÃO EXECUTADO

Os blocos seguintes incluem escritas futuras e não devem ser executados antes de autorização expressa do PM para **cinco registros próprios** e sua permanência. Revisar o marcador, os comandos completos, a conexão e as contagens na nova autorização. Não ativar o teste SQL da TASK-002, não usar IDs dos registros antigos, não repetir POST automaticamente após timeout e não executar limpeza.

### A. Preflight e dados fictícios próprios

1. Repetir a consulta de identidade/ONLINE/NTLM, migração única e cinco contagens. Repetir a conferência de modelo pendente em backend/. Não examinar conteúdos anteriores.
2. Escolher um marcador `TASK003-IMAGENS-<guid com 32 caracteres hexadecimais>` e submetê-lo na autorização. Não usar literalmente os placeholders abaixo.
3. Criar somente as duas referências próprias com o lote SQL abaixo, em conexão explícita `.\SQLEXPRESS` / `ZelaSP_Dev`, Windows integrada. Não existe endpoint de criação de referências. Os IDs retornados são usados na próxima etapa, sem pressupor valores sequenciais.

**SQL preparado para revisão futura; não executado:**

```sql
-- Conexao exigida: Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true.
DECLARE @marker nvarchar(120) = N'TASK003-IMAGENS-<GUID_APROVADO>';
IF LEN(@marker) <> 48 OR LEFT(@marker,16) <> N'TASK003-IMAGENS-'
   OR RIGHT(@marker,32) LIKE N'%[^0-9a-f]%'
    THROW 50001, 'Substituir marcador por GUID revisado e autorizado.', 1;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),N'') <> N'slayr\SQLEXPRESS'
   OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') <> N'SQLEXPRESS'
   OR DB_NAME() <> N'ZelaSP_Dev'
   OR COALESCE(CONVERT(nvarchar(40),CONNECTIONPROPERTY('auth_scheme')),N'') NOT IN (N'NTLM',N'KERBEROS')
   OR COALESCE(CONVERT(nvarchar(40),DATABASEPROPERTYEX(DB_NAME(),'Status')),N'') <> N'ONLINE'
    THROW 50002, 'Destino ou autenticacao divergente.', 1;
IF (SELECT COUNT_BIG(*) FROM dbo.__EFMigrationsHistory) <> 1
   OR NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
                  WHERE MigrationId=N'20261114191732_InitialCreate' AND ProductVersion=N'10.0.6')
    THROW 50003, 'Historico de migracao divergente.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
               WHERE c.object_id=OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA') AND c.name=N'dados'
                 AND t.name=N'varbinary' AND c.max_length=-1 AND c.is_nullable=0)
    THROW 50006, 'Schema de bytes divergente; nao escrever.', 1;
IF (SELECT COUNT_BIG(*) FROM dbo.BAIRRO) <> 1
   OR (SELECT COUNT_BIG(*) FROM dbo.CATEGORIA_OCORRENCIA) <> 1
   OR (SELECT COUNT_BIG(*) FROM dbo.OCORRENCIA) <> 1
   OR (SELECT COUNT_BIG(*) FROM dbo.IMAGEM_OCORRENCIA) <> 0
   OR (SELECT COUNT_BIG(*) FROM dbo.HISTORICO_STATUS) <> 1
    THROW 50004, 'Contagens divergentes; solicitar nova revisao.', 1;
IF EXISTS (SELECT 1 FROM dbo.BAIRRO WHERE nome=@marker)
   OR EXISTS (SELECT 1 FROM dbo.CATEGORIA_OCORRENCIA WHERE nome=@marker)
    THROW 50005, 'Marcador ja utilizado.', 1;
SET XACT_ABORT ON; -- Somente esta sessao; nao altera configuracoes globais.
BEGIN TRANSACTION;
INSERT INTO dbo.BAIRRO(nome,subprefeitura)
VALUES (@marker,N'Subprefeitura exclusivamente ficticia TASK003');
DECLARE @bairroId int = CONVERT(int,SCOPE_IDENTITY());
INSERT INTO dbo.CATEGORIA_OCORRENCIA(nome) VALUES (@marker);
DECLARE @categoriaId int = CONVERT(int,SCOPE_IDENTITY());
COMMIT;
SELECT @marker AS marcador, @bairroId AS bairroId, @categoriaId AS categoriaId;
```

Não se trata de seed de desenvolvimento ou migração. É um lote único de duas referências fictícias cuja execução/permanência precisará de autorização. XACT_ABORT limita a atomicidade desse lote; não autoriza reparo ou exclusão de dados após falha.

### B. API com conexão aprovada e fixture em memória

Em um terminal separado, após conferir configurações/overrides e a autorização de execução futura:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$approved = 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
$env:ConnectionStrings__ZelaSP = $approved
dotnet run --project backend/src/ZelaSP.Api --no-build --no-restore -- --urls http://127.0.0.1:5187 --ConnectionStrings:ZelaSP $approved
```

O argumento de linha de comando fixa a conexão com precedência sobre os providers padrão de configuração da aplicação revisada; não depender somente de variável de ambiente. Confirmar que esse processo iniciou corretamente na porta exclusiva, sem usar outra API já em execução; interromper se houver conflito de porta, configuração posterior divergente ou alteração do código/configuração revisados. O startup não grava dados. Conferir também a configuração acadêmica efetiva aprovada: RM 554787, quatro executores oficiais e ausência de override divergente. Não imprimir conteúdo pessoal em logs. Não alterar a configuração para facilitar o teste.

**PowerShell preparado para revisão futura; não executado.** Rodar da raiz do repositório somente após revisar os IDs/marcador devolvidos pela etapa A e o escopo dos três POSTs (uma ocorrência e duas imagens):

```powershell
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$marker = 'TASK003-IMAGENS-<GUID_APROVADO>'
$bairroId = 0       # Substituir exclusivamente pelo ID retornado para o novo lote.
$categoriaId = 0    # Substituir exclusivamente pelo ID retornado para o novo lote.
if ($marker -notmatch '^TASK003-IMAGENS-[0-9a-f]{32}$' -or $bairroId -le 0 -or $categoriaId -le 0) {
    throw 'Preencher somente valores revisados e autorizados.'
}
$source = Get-Content backend/tests/ZelaSP.Tests/ImagemFixtures.cs -Raw
$png = [Convert]::FromBase64String([regex]::Match($source,'Png => Convert.FromBase64String\("([^"]+)"\)').Groups[1].Value)
$jpeg = [Convert]::FromBase64String([regex]::Match($source,'Jpeg => Convert.FromBase64String\("([^"]+)"\)').Groups[1].Value)
function Hash-Bytes([byte[]]$bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
if ($png.Length -ne 120 -or (Hash-Bytes $png) -ne 'a5c8f1a1213865e9ef89420f46e08085aa9e1c94a1723bf77218d44865f10575' -or
    $jpeg.Length -ne 634 -or (Hash-Bytes $jpeg) -ne '9d8e6351c67193ebe9c11de358c8e54f127521aa511d6a1dc6003f64877e4860') {
    throw 'Fixtures divergentes; interromper antes de POST.'
}
$academic = (Get-Content backend/src/ZelaSP.Api/appsettings.json -Raw | ConvertFrom-Json).Academico
if ($academic.RmLider -cne '554787') { throw 'RM de configuracao divergente.' }
$executor = $academic.Executores[0] # Identificador oficial da configuracao revisada; nao e autenticacao.
$referenceConnection = New-Object System.Data.SqlClient.SqlConnection 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;ApplicationIntent=ReadOnly;Connect Timeout=8'
try {
    $referenceConnection.Open()
    $referenceCheck = $referenceConnection.CreateCommand()
    $referenceCheck.CommandText = "SELECT CASE WHEN CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))=N'slayr\SQLEXPRESS' AND CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName'))=N'SQLEXPRESS' AND DB_NAME()=N'ZelaSP_Dev' AND EXISTS (SELECT 1 FROM dbo.BAIRRO WHERE id=@bairroId AND nome=@marker) AND EXISTS (SELECT 1 FROM dbo.CATEGORIA_OCORRENCIA WHERE id=@categoriaId AND nome=@marker) THEN 1 ELSE 0 END;"
    $null = $referenceCheck.Parameters.Add('@bairroId',[Data.SqlDbType]::Int)
    $referenceCheck.Parameters['@bairroId'].Value = $bairroId
    $null = $referenceCheck.Parameters.Add('@categoriaId',[Data.SqlDbType]::Int)
    $referenceCheck.Parameters['@categoriaId'].Value = $categoriaId
    $null = $referenceCheck.Parameters.Add('@marker',[Data.SqlDbType]::NVarChar,120)
    $referenceCheck.Parameters['@marker'].Value = $marker
    if ($referenceCheck.ExecuteScalar() -ne 1) { throw 'IDs nao pertencem ao lote proprio ou destino diverge; nao enviar POST.' }
    $referenceCheck.Dispose()
} finally { $referenceConnection.Dispose() }
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$client = New-Object System.Net.Http.HttpClient($handler)
$client.BaseAddress = [Uri]'http://127.0.0.1:5187'
$client.Timeout = [TimeSpan]::FromSeconds(30)
$images = @()
try {
    # OpenAPI is read-only and reflects the server's effective academic configuration.
    $openapi = $client.GetStringAsync('/openapi/v1.json').GetAwaiter().GetResult() | ConvertFrom-Json
    $uploadContract = $openapi.paths.'/api/imagens'.post
    $serverMembers = @($uploadContract.requestBody.content.'multipart/form-data'.schema.properties.executor.enum)
    $memberDiff = @(Compare-Object -ReferenceObject @($academic.Executores) -DifferenceObject $serverMembers -CaseSensitive)
    if ($serverMembers.Count -ne 4 -or $memberDiff.Count -ne 0 -or
        $uploadContract.responses.'201'.content.'application/json'.example.rmLider -cne '554787') {
        throw 'Configuracao academica efetiva do servidor divergente; nao enviar POST.'
    }
    # POST adicional explicitamente incluido no lote: uma ocorrencia ficticia propria.
    $body = @{ bairroId=$bairroId; categoriaId=$categoriaId; descricao=$marker } | ConvertTo-Json -Compress
    $json = New-Object System.Net.Http.StringContent($body,[Text.Encoding]::UTF8,'application/json')
    try { $created = $client.PostAsync('/api/ocorrencias',$json).GetAwaiter().GetResult() }
    finally { $json.Dispose() }
    try {
        if ([int]$created.StatusCode -ne 201) { throw 'Cadastro ficticio falhou; parar sem repetir.' }
        $occurrence = $created.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
        if ($occurrence.id -le 0 -or $occurrence.status -cne 'ABERTA' -or $occurrence.origem -cne 'APP' -or
            $occurrence.bairroId -ne $bairroId -or $occurrence.categoriaId -ne $categoriaId) { throw 'Ocorrencia divergente.' }
    } finally { $created.Dispose() }
    $before = $client.GetStringAsync('/api/ocorrencias/' + $occurrence.id).GetAwaiter().GetResult()
    $beforeHistory = $client.GetStringAsync('/api/ocorrencias/' + $occurrence.id + '/historico').GetAwaiter().GetResult()
    if (($beforeHistory | ConvertFrom-Json).Count -ne 0) { throw 'Historico inicial inesperado.' }
    $files = @(
        @{ Name='task003-pixel-ficticio.png'; Mime='image/png'; Bytes=$png; Title=($marker+' PNG') },
        @{ Name='task003-pixel-ficticio.jpeg'; Mime='image/jpeg'; Bytes=$jpeg; Title=($marker+' JPEG') }
    )
    foreach ($file in $files) {
        $form = New-Object System.Net.Http.MultipartFormDataContent
        try {
            $form.Add((New-Object System.Net.Http.StringContent([string]$occurrence.id)),'ocorrenciaId')
            $form.Add((New-Object System.Net.Http.StringContent($file.Title)),'titulo')
            $form.Add((New-Object System.Net.Http.StringContent($executor)),'executor')
            $part = New-Object System.Net.Http.ByteArrayContent -ArgumentList (,$file.Bytes)
            $part.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue($file.Mime)
            $form.Add($part,'arquivo',$file.Name) # Sem rmLider/dataUtc do cliente.
            $uploaded = $client.PostAsync('/api/imagens',$form).GetAwaiter().GetResult()
            try {
                if ([int]$uploaded.StatusCode -ne 201) { throw 'Upload falhou; preservar estado, sem repetir.' }
                $raw = $uploaded.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                $metadata = $raw | ConvertFrom-Json
                $location = New-Object Uri($client.BaseAddress,$uploaded.Headers.Location)
                if ($metadata.id -le 0 -or $location.GetLeftPart([UriPartial]::Authority) -ne $client.BaseAddress.GetLeftPart([UriPartial]::Authority) -or
                    $location.AbsolutePath -ne ('/api/imagens/'+$metadata.id)) { throw 'Location divergente.' }
                if ($metadata.ocorrenciaId -ne $occurrence.id -or $metadata.titulo -cne $file.Title -or
                    $metadata.nomeArquivo -cne $file.Name -or $metadata.mime -cne $file.Mime -or
                    $metadata.tamanhoBytes -ne $file.Bytes.Length -or $metadata.rmLider -cne '554787' -or
                    $metadata.executor -cne $executor -or
                    $null -ne $metadata.PSObject.Properties['dados']) { throw 'Metadados divergentes.' }
                $metadataResponse = $client.GetAsync('/api/imagens/'+$metadata.id).GetAwaiter().GetResult()
                try {
                    if ([int]$metadataResponse.StatusCode -ne 200) { throw 'GET de metadados nao retornou 200.' }
                    $foundRaw = $metadataResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    $found = $foundRaw | ConvertFrom-Json
                } finally { $metadataResponse.Dispose() }
                foreach ($name in @('id','ocorrenciaId','titulo','nomeArquivo','mime','tamanhoBytes','rmLider','executor')) {
                    if ($found.$name -cne $metadata.$name) { throw 'GET de metadados divergente.' }
                }
                # Compare raw ISO literals independently of ConvertFrom-Json date coercion.
                $postUtc = [regex]::Match($raw,'"dataUtc"\s*:\s*"([^"]+)"').Groups[1].Value
                $getUtc = [regex]::Match($foundRaw,'"dataUtc"\s*:\s*"([^"]+)"').Groups[1].Value
                if ($postUtc -notmatch 'Z$' -or $getUtc -notmatch 'Z$') { throw 'Data nao esta em UTC.' }
                $postTime = [DateTimeOffset]::Parse($postUtc,[Globalization.CultureInfo]::InvariantCulture)
                $getTime = [DateTimeOffset]::Parse($getUtc,[Globalization.CultureInfo]::InvariantCulture)
                $deltaMs = [Math]::Abs(($postTime-$getTime).TotalMilliseconds)
                if ($deltaMs -gt 1) { throw 'Data persistida diverge alem da precisao datetime2(3).' }
                Write-Output ('Diferenca temporal por precisao datetime2(3), ms=' + $deltaMs)
                $content = $client.GetAsync('/api/imagens/'+$metadata.id+'/conteudo').GetAwaiter().GetResult()
                try {
                    if ([int]$content.StatusCode -ne 200 -or $content.Content.Headers.ContentType.MediaType -cne $file.Mime) { throw 'Resposta binaria divergente.' }
                    $received = $content.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
                    if ($received.Length -ne $file.Bytes.Length) { throw 'Comprimento binario divergente.' }
                    for ($i=0; $i -lt $received.Length; $i++) {
                        if ($received[$i] -ne $file.Bytes[$i]) { throw 'Byte recuperado divergente.' }
                    }
                    if ((Hash-Bytes $received) -cne (Hash-Bytes $file.Bytes)) { throw 'Hash divergente.' }
                } finally { $content.Dispose() }
                $images += $metadata
                Write-Output ('Imagem ficticia confirmada: id=' + $metadata.id + ', bytes=' + $metadata.tamanhoBytes + ', MIME=' + $metadata.mime)
            } finally { $uploaded.Dispose() }
        } finally { $form.Dispose() }
    }
    $after = $client.GetStringAsync('/api/ocorrencias/'+$occurrence.id).GetAwaiter().GetResult()
    $afterHistory = $client.GetStringAsync('/api/ocorrencias/'+$occurrence.id+'/historico').GetAwaiter().GetResult()
    if ($before -cne $after -or $beforeHistory -cne $afterHistory) { throw 'Ocorrencia ou historico alterado.' }
    Write-Output ('Lote mantido: ' + $marker + '; ocorrenciaId=' + $occurrence.id + '; imagens=' + (($images | ForEach-Object { $_.id }) -join ','))
} finally { $client.Dispose(); $handler.Dispose() }
```

O roteiro não inclui retentativa automática de POST. Um timeout pode ocorrer após commit: interromper e inspecionar o lote por leitura, sem reenviar nem apagar. Os requests são sequenciais e o lote inteiro não é uma transação única entre HTTP/referências; falha intermediária pode deixar um subconjunto confirmado, que deve ser mantido para revisão. O script preparado foi revisado como texto, **não foi executado**, nem mesmo parcialmente contra o SQL Server.

Ressalva temporal: o POST devolve DataUtc da entidade em memória; SQL Server armazena datetime2(3), enquanto o relógio .NET pode fornecer mais casas fracionárias. O roteiro registra essa diferença e exige UTC/variação máxima de 1 ms entre POST/GET, sem exigir igualdade textual indevida. GET e SELECT independente devem concordar na precisão armazenada. Isso é uma consideração identificada por inspeção, não resultado observado com imagens reais nesta etapa; nenhuma alteração de produção foi feita. A igualdade dos bytes continua sendo exata, sem tolerância.

### C. Leitura independente final — somente registros do lote

Após os POSTs autorizados, uma nova conexão SqlClient com a mesma identidade/instância/banco deve executar os SELECTs abaixo, parametrizados com os IDs retornados e o marcador, nunca IDs presumidos. Nenhuma seleção de bytes/conteúdo pessoal preexistente:

```sql
-- Parametros obrigatorios: @marker, @ocorrenciaId, @pngId, @jpegId (todos do lote autorizado).
SELECT c.name AS coluna, t.name AS tipo, c.max_length
FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
WHERE c.object_id=OBJECT_ID(N'dbo.IMAGEM_OCORRENCIA') AND c.name=N'dados';
-- Esperado: dados / varbinary / -1, isto e, varbinary(max).

SELECT i.id,i.ocorrencia_id,i.titulo,i.nome_arquivo,i.mime,
       CAST(DATALENGTH(i.dados) AS int) AS tamanhoBytes,i.data_utc,i.rm_lider,i.executor
FROM dbo.IMAGEM_OCORRENCIA i
JOIN dbo.OCORRENCIA o ON o.id=i.ocorrencia_id AND o.id=@ocorrenciaId AND o.descricao=@marker
JOIN dbo.BAIRRO b ON b.id=o.bairro_id AND b.nome=@marker
JOIN dbo.CATEGORIA_OCORRENCIA c ON c.id=o.categoria_id AND c.nome=@marker
WHERE i.id IN (@pngId,@jpegId);
-- Exatamente duas imagens: 120/image/png e 634/image/jpeg; IDs/FKs/metadados correspondentes ao HTTP.

SELECT o.id,o.bairro_id,o.categoria_id,o.status,o.origem,o.criada_em
FROM dbo.OCORRENCIA o
JOIN dbo.BAIRRO b ON b.id=o.bairro_id AND b.nome=@marker
JOIN dbo.CATEGORIA_OCORRENCIA c ON c.id=o.categoria_id AND c.nome=@marker
WHERE o.id=@ocorrenciaId AND o.descricao=@marker;
-- ABERTA/APP; IDs/data e demais campos iguais ao snapshot antes dos uploads.

SELECT COUNT_BIG(*) AS historicos_do_lote
FROM dbo.HISTORICO_STATUS h JOIN dbo.OCORRENCIA o ON o.id=h.ocorrencia_id
WHERE o.id=@ocorrenciaId AND o.descricao=@marker;
-- Zero.

SELECT (SELECT COUNT_BIG(*) FROM dbo.BAIRRO) AS bairros,
       (SELECT COUNT_BIG(*) FROM dbo.CATEGORIA_OCORRENCIA) AS categorias,
       (SELECT COUNT_BIG(*) FROM dbo.OCORRENCIA) AS ocorrencias,
       (SELECT COUNT_BIG(*) FROM dbo.IMAGEM_OCORRENCIA) AS imagens,
       (SELECT COUNT_BIG(*) FROM dbo.HISTORICO_STATUS) AS historicos;
-- Esperado para o baseline desta revisao, sem terceiros: 2,2,2,2,1.
```

Registrar respostas/códigos/Location, IDs/marcador, hashes/quantidades/MIME e resultado de cada asserção, sem imprimir bytes. A verificação HTTP de GET usa novos escopos de DbContext; a consulta independente SQL demonstra que os metadados e tamanhos estão persistidos no schema real. Conferir os cinco totais e seus deltas sem ler registros antigos. Não basta aceitar apenas HTTP 201 como prova.

## Segurança e limites da autorização futura

O procedimento não faz UPDATE, DELETE, TRUNCATE, migração ou alterações em registros existentes; os únicos INSERTs planejados são as duas referências próprias, a nova ocorrência pelo endpoint existente e duas imagens pelos novos endpoints. A identidade SQL local tem privilégios amplos; o isolamento depende das guardas de destino, IDs/marcador e escopo revisado, não de permissões restritas. Contagens globais não detectam sozinhas toda modificação feita por terceiros durante a execução. Não há autenticação de usuários; executor é somente declaração acadêmica validada por lista. Esta pré-validação não comprova os futuros POSTs, transações, materialização com dados ou persistência de imagens no SQL Server.

## Resultado da execução única autorizada — 16/11/2026

O bloco integral revisado na conversa, e não uma execução automática de todos os exemplos históricos acima, terminou com exit code 0. Autorização consumida: PM_PO_TASK003_SQLSERVER_IMAGE_VALIDATION_APROVADO. Marcador: TASK003-IMAGENS-7c3fa9b2e1d44a8b9f2056c781a3d0e4.

Destino slayr\SQLEXPRESS / ZelaSP_Dev, Windows integrada. IDs bairro/categoria/ocorrência 2, PNG 1 e JPEG 2. Uploads 201, GETs 200, bytes idênticos e hashes confirmados por SQL independente. PNG 120 bytes; JPEG 634 bytes. Contagens passaram de 1/1/1/0/1 para 2/2/2/2/1. Ocorrência preservada ABERTA/APP, nenhum histórico criado. Cinco fictícios mantidos por autorização. Sem migração/schema, limpeza ou alterações preexistentes. Evidências completas e hashes em [VALIDACAO.md](../VALIDACAO.md).

Este marcador foi utilizado e a autorização encerrada. Não repetir POSTs ou executar os exemplos deste documento sem nova revisão/autorização. Nenhuma escrita adicional ocorreu no encerramento técnico.