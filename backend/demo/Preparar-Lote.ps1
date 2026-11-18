[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^TASK007-DEMO-[0-9a-f]{32}$')][string]$Marcador,
    [Parameter(Mandatory=$true)][ValidateSet('.\SQLEXPRESS')][string]$Instancia,
    [Parameter(Mandatory=$true)][ValidateSet('ZelaSP_Dev')][string]$Banco,
    [Parameter(Mandatory=$true)][uri]$BaseUri,
    [Parameter(Mandatory=$true)][ValidateSet('APENAS_PREPARACAO_OFFLINE')][string]$Confirmacao
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
# OFFLINE ONLY. No SQL client, HTTP, detector, app, build, tests or migrations are invoked.
if (-not $BaseUri.IsAbsoluteUri -or -not $BaseUri.IsLoopback -or $BaseUri.Scheme -notin @('http','https') -or
    $BaseUri.UserInfo -or $BaseUri.Query -or $BaseUri.Fragment -or $BaseUri.AbsolutePath -ne '/') {
    throw 'Use URL base local explicita, sem usuario, query, fragmento ou subdiretorio.'
}
if ($Marcador -cnotmatch '^TASK007-DEMO-[0-9a-f]{32}$') { throw 'Marcador deve usar exatamente o formato aprovado.' }
$manifesto = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifesto.template.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$carga = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'carga.template.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$manifesto.marcador = $Marcador
$manifesto.preparadoEmUtc = [DateTime]::UtcNow.ToString('o')
$manifesto.destinoEsperado.instanciaConexao = $Instancia
$manifesto.destinoEsperado.banco = $Banco
$manifesto.destinoEsperado.apiBaseUri = $BaseUri.AbsoluteUri.TrimEnd('/')
# Execution permissions stay false: preparing files does not grant database/API permission.
$pii = ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'amostras/relato-pii.template.txt'))).Trim().Replace('{MARCADOR}', $Marcador)
$esperado = ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'amostras/relato-pii-protegido.esperado.txt'))).Trim().Replace('{MARCADOR}', $Marcador)
if ($pii.Length -gt 4000 -or $esperado.Length -gt 4000) { throw 'Template excede limite do contrato.' }
$fontes = @(@{ Nome='pixel-ficticio.png'; Bytes=120; Hash=$manifesto.amostras.png.sha256Esperado },
            @{ Nome='pixel-ficticio.jpeg'; Bytes=634; Hash=$manifesto.amostras.jpeg.sha256Esperado })
foreach ($fonte in $fontes) {
    $path = Join-Path $PSScriptRoot ('amostras/' + $fonte.Nome)
    if ((Get-Item -LiteralPath $path).Length -ne $fonte.Bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $fonte.Hash) {
        throw 'Amostra diverge da referencia; nao continuar.'
    }
}
$raiz = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'execucoes'))
$saida = [IO.Path]::GetFullPath((Join-Path $raiz $Marcador))
if (-not $saida.StartsWith($raiz + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Destino fora da pasta de lotes.' }
if (Test-Path -LiteralPath $saida) { throw 'Lote local ja existe. Nao sobrescrever ou retomar automaticamente.' }
if (-not (Test-Path -LiteralPath $raiz)) { [void][IO.Directory]::CreateDirectory($raiz) }
# New-Item fails for an existing batch instead of replacing it. No cleanup on failure.
[void](New-Item -ItemType Directory -Path $saida -ErrorAction Stop)
function Write-NewJson([string]$Nome, $Objeto) {
    $file = Join-Path $saida $Nome
    $stream = [IO.File]::Open($file, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $writer = New-Object IO.StreamWriter($stream, (New-Object Text.UTF8Encoding($false)))
        try { $writer.Write(($Objeto | ConvertTo-Json -Depth 30)) } finally { $writer.Dispose() }
    } finally { $stream.Dispose() }
}
Write-NewJson 'manifesto.json' $manifesto
Write-NewJson 'plano.json' $carga
Write-NewJson 'requests-pendentes.json' ([ordered]@{
    estado='PLANEJADO_NAO_ENVIADO'
    criarA=[ordered]@{bairroId=$null; categoriaId=$null; descricao=$carga.ocorrencias[0].descricaoTemplate.Replace('{MARCADOR}',$Marcador)}
    criarB=[ordered]@{bairroId=$null; categoriaId=$null; descricao=$pii}
    criarC=[ordered]@{bairroId=$null; categoriaId=$null; descricao=$carga.ocorrencias[2].descricaoTemplate.Replace('{MARCADOR}',$Marcador)}
    descricaoBEsperada=$esperado
    aviso='IDs nulos devem ser preenchidos a partir de referencias aprovadas. Nao enviar este envelope como DTO da API.'
})
foreach ($fonte in $fontes) { [IO.File]::Copy((Join-Path $PSScriptRoot ('amostras/' + $fonte.Nome)), (Join-Path $saida $fonte.Nome), $false) }
Write-Output ('Preparacao local somente: ' + $saida)
Write-Output 'Nenhum SQL ou HTTP executado. Manifesto nao contem resultados observados nem autorizacao de escrita.'
