# CORS restrito para validação Expo Web

19/11/2026 — feature/task-008-academic-deliverables.

## Alteração

`backend/src/ZelaSP.Api/Program.cs`: política ExpoRiskDevelopment registrada e middleware habilitado somente em Development. Origem exata `http://127.0.0.1:8081`; método GET; sem credenciais, curingas ou liberação genérica de headers. Branch seleciona exatamente `/api/risco-cidadao`, com comparação ordinal, para GET ou OPTIONS que solicite GET. OPTIONS é a negociação CORS, não uma operação de negócio adicional. Outras rotas/métodos não recebem essa política. Roteamento explícito posiciona CORS antes da execução dos controllers.

Não houve alteração de resposta pública, validação, autenticação, detector PII, banco, ApiExceptionHandler ou entregas acadêmicas. CORS controla leitura por navegadores: não é autenticação e não impede clientes não-browser de enviar requisições.

## Verificações observadas

1. Build padrão, Debug/net10.0, sem restore: exit 1, 30 avisos e seis erros MSB3027/MSB3021, por três DLLs da API em uso. Nenhum processo foi encerrado. Tentativas de cópia foram as internas do MSBuild, não reinicializações da API.
2. Build com saída separada, SDK 10.0.202: exit 0, zero avisos, zero erros; 2,51 s.

```powershell
dotnet build .\backend\src\ZelaSP.Api\ZelaSP.Api.csproj --configuration Debug --no-restore --output .\backend\.artifacts\cors-api-validation
```

DLL: `backend/.artifacts/cors-api-validation/ZelaSP.Api.dll`.
SHA-256: `9FAB82077CC49417DC7ABEBF025916C66835EA69E83D5C6CBCE9D1D2D8DD67DA`.
EXE: `backend/.artifacts/cors-api-validation/ZelaSP.Api.exe`.

3. `RiskCorsTests.cs`: 12 aprovados, zero falhas/ignorados; exit 0, tempo total informado 1,9593 s.

```powershell
dotnet test .\backend\tests\ZelaSP.Tests\ZelaSP.Tests.csproj --configuration Debug --no-restore --output .\backend\.artifacts\cors-test-validation --filter "FullyQualifiedName~ZelaSP.Tests.RiskCorsTests" --logger "console;verbosity=normal"
```

TestServer em memória com IRiscoCidadaoStore e IExplicacaoCidadaClient substituídos por implementações locais: nenhum SQL, chamada externa ou listener TCP da aplicação. Testes conferem GET/fallback, origem exata, localhost/porta/HTTPS/origem externa rejeitados, Production sem CORS, rota com barra final sem liberação, OpenAPI sem liberação, POST sem liberação, preflight GET permitido, preflight POST e outra rota negados; sem Access-Control-Allow-Credentials e sem liberação de headers arbitrários.

4. `git diff --check`: sem erros. Índice Git permanece vazio. Avisos LF/CRLF de alterações preexistentes preservados.

Artefatos em `.artifacts/` são saídas locais geradas, não arquivos para publicação. Não foram apagados. Não houve SQL, migração, alteração de registros, LLM real, execução da demonstração integrada ou encerramento de processos existentes.

## Comandos propostos — NÃO executados

Antes: autorização de execução, disponibilidade das portas 5187/8081 e conferência do hash. Como há processo usando DLLs da saída padrão, não presumir que a porta esteja livre; se ocupada, interromper. A pasta bin padrão não foi validada pelo build bem-sucedido: usar exclusivamente a saída separada abaixo.

Terminal visível da API:

```powershell
Set-Location 'C:\Users\switch\Desktop\ZelaSP\backend\src\ZelaSP.Api'
& '..\..\.artifacts\cors-api-validation\ZelaSP.Api.exe' `
  --environment Development `
  --urls 'http://127.0.0.1:5187' `
  --ConnectionStrings:ZelaSP 'Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true' `
  --RiscoAi:Enabled false
```

Terminal Expo:

```powershell
Set-Location 'C:\Users\switch\Desktop\ZelaSP\mobile'
$env:EXPO_PUBLIC_DATA_MODE = 'api'
$env:EXPO_PUBLIC_API_URL = 'http://127.0.0.1:5187'
$env:EXPO_PUBLIC_TEST_MODE = 'false'
$env:EXPO_OFFLINE = '1'
npm.cmd run web -- --host localhost --port 8081
```

Abrir exatamente `http://127.0.0.1:8081`, com perfil isolado. A validação deverá bloquear no navegador todas as chamadas de negócio exceto GET `/api/risco-cidadao`: o bootstrap Mobile ainda tenta outros endpoints com contratos preexistentes incompatíveis. A política CORS não impede que uma requisição GET simples seja enviada; por isso não substitui essa contenção de escopo no navegador. Nenhuma consulta extra ao banco está autorizada por esta preparação.

O comando Expo pode realizar sua checagem de compatibilidade de porta; não aceitar mudança automática de porta. Confirmar origem efetiva e processo próprio. Observar resposta real, CORS, agregado e FALLBACK; não criar eventos para aumentar pontuação. Ainda pendente autorização antes de executar esses comandos ou consultar SQL Server pela API.
