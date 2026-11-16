# TASK-002 — Validação SQL Server preparada, não executada

**Não executar os comandos de escrita desta página sem autorização específica do PM/PO. A autorização da TASK-002 permite preparar o teste, mas não executá-lo.** A autorização anterior da TASK-001 abrangia somente criação do banco e aplicação de InitialCreate.

## Destino fixo e limites

- Conexão exclusivamente com `Server=.\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true`.
- O teste confirma por SELECT `slayr\SQLEXPRESS`, instância SQLEXPRESS, banco efetivo ZelaSP_Dev e estado ONLINE antes de qualquer escrita.
- Exige exatamente `20261114191732_InitialCreate` aplicada e nenhum modelo pendente. Não executa migrações, criação de banco/schema, DROP, TRUNCATE ou limpeza.
- Imprime contagens anteriores de bairros, categorias, ocorrências e históricos; não imprime conteúdo preexistente.
- Gera um marcador exclusivo `TASK002-FICTICIO-<guid>` e cria apenas um bairro, uma categoria e uma ocorrência fictícios próprios. Não reutiliza nem modifica referências preexistentes.
- Atualiza somente o ID de ocorrência criado nessa execução. Confirma persistência em outro DbContext, aplica ABERTA → EM_ANALISE e verifica o histórico.
- Injeta uma falha na gravação do segundo histórico, após o UPDATE para RESOLVIDA dentro da transação. Outro DbContext deve observar EM_ANALISE e um único histórico, demonstrando rollback real no provider SQL Server.
- Registros fictícios confirmados ficam no banco, com marcador e IDs impressos. Não há DELETE automático. Sua eventual remoção exige escopo e autorização separados. Em caso de falha, preservar os registros e relatar o marcador; não tentar reparar ou limpar o banco.

## Procedimento para autorização futura

Antes de pedir a execução, o responsável deverá conferir novamente o destino e inspecionar os dados atuais somente por leitura. A evidência da TASK-001 não substitui essa inspeção do estado presente. Revisar este procedimento e o arquivo `tests/ZelaSP.Tests/SqlServerWriteTests.cs`, informar ao PO o padrão de marcador planejado, o tipo de registros e sua permanência, e solicitar autorização específica para inserts/updates fictícios e teste de rollback.

Após autorização, executar a partir de `backend/`. Se o ambiente exigir execução fora do sandbox, apresentar o comando completo e solicitar aprovação individual. Não usar permissões amplas persistentes.

```powershell
# APENAS APÓS AUTORIZAÇÃO ESPECÍFICA DE ESCRITA:
$env:ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION = 'PM_PO_TASK002_APROVADO'
try {
    dotnet test ZelaSP.sln --no-restore --filter 'Category=SqlServerWrite' --logger 'console;verbosity=normal'
} finally {
    Remove-Item Env:ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION
}
```

A variável apenas habilita o teste; não concede autorização organizacional nem permissão SQL. O teste também verifica a variável no início de sua execução. Sem o valor exato, o atributo xUnit marca o teste como ignorado antes de abrir conexão. `Remove-Item` acima remove apenas a variável do processo, sem remover arquivos ou dados.

## Execução segura da suíte padrão

```powershell
dotnet test ZelaSP.sln --no-restore --filter 'Category!=SqlServerWrite'
```

A execução normal sem variável também ignora o teste SQL Server. Os demais testes de API usam uma conexão SQLite independente `:memory:` por caso, provider substituído na DI e guarda explícita que impede criação do DDL em qualquer provider diferente de SQLite. Só a fixture temporária adapta `varbinary(max)` para `BLOB`, pois o parser SQLite não aceita `max`; código, modelo e migração SQL Server não são alterados.

## Evidência a coletar futuramente

Registrar autorização, comando, servidor/banco efetivos, contagens anteriores, marcador/IDs fictícios, status em contextos distintos, histórico confirmado, rollback, resultado xUnit e registros remanescentes. A aprovação dessa prova de persistência real permanece pendente; a compilação do teste não comprova que ele passou no SQL Server.
