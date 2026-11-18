using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Api;

public sealed class ContratoOpenApiTransformer : IOpenApiDocumentTransformer, IOpenApiSchemaTransformer
{
    private const string Ocorrencia = """{"id":1,"bairroId":1,"categoriaId":1,"descricao":"Descarte fictício para demonstração.","status":"ABERTA","criadaEm":"2026-11-15T12:00:00Z","origem":"APP"}""";

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken ct)
    {
        var type = context.JsonTypeInfo.Type;
        if (type == typeof(StatusOcorrencia) || type == typeof(OrigemOcorrencia) || type == typeof(NivelRisco))
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = Enum.GetNames(type).Select(x => (JsonNode)JsonValue.Create(x)!).ToList();
        }
        // Nullable request properties detect missing values in model binding, but [Required]
        // rejects null at runtime. Publish that effective contract instead of nullable schemas.
        if (type == typeof(CriarOcorrenciaRequest) && schema.Properties is { } create)
        {
            create["bairroId"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = "1", Maximum = "2147483647" };
            create["categoriaId"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = "1", Maximum = "2147483647" };
            create["descricao"] = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 4000 };
        }
        if (type == typeof(AtualizarStatusRequest) && schema.Properties is { } patch)
            patch["status"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = Enum.GetNames<StatusOcorrencia>().Select(x => (JsonNode)JsonValue.Create(x)!).ToList()
            };
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info.Title = "ZelaSP — API de ocorrências";
        document.Info.Version = "v1";
        document.Info.Description = "TASK-002. Datas UTC; enumerações textuais; erros Problem Details. Sem autenticação nesta etapa. Contrato detalhado: backend/docs/API_CONTRACT.md.";
        foreach (var (path, item) in document.Paths)
        foreach (var (method, operation) in item.Operations ?? [])
        {
            var contract = (path, method.ToString().ToUpperInvariant()) switch
            {
                ("/api/risco-cidadao", "GET") => (operation.Summary ?? "Risco Cidadão", operation.Description ?? "Indicador global em sete dias UTC.", """{"pontuacao":0,"nivel":"BAIXO","periodoDias":7,"deteccoes":{"cpf":0,"telefone":0,"email":0,"senha":0,"saude":0},"vazamentosBloqueados":0,"alertas":[],"janela":{"inicioUtc":"2026-11-10T12:00:00Z","fimUtc":"2026-11-17T12:00:00Z"},"explicacao":{"texto":"Nenhum evento reconhecido foi registrado na janela. O indicador não representa perigo no bairro.","origem":"FALLBACK"}}""", (string?)null),
                ("/api/bairros", "GET") => ("Listar bairros", "Ordenação por nome e id; retorna [] quando vazio.", "[{\"id\":1,\"nome\":\"Bairro fictício\",\"subprefeitura\":\"Subprefeitura fictícia\"}]", (string?)null),
                ("/api/categorias-ocorrencia", "GET") => ("Listar categorias", "Ordenação por nome e id; retorna [] quando vazio.", "[{\"id\":1,\"nome\":\"Descarte irregular\"}]", null),
                ("/api/ocorrencias", "POST") => ("Cadastrar ocorrência", "bairroId e categoriaId obrigatórios e positivos, existentes; descricao obrigatória com 1–4000 caracteres. CPF, telefone, e-mail, senha e termos de saúde reconhecidos são mascarados antes da gravação e resposta, com eventos técnicos na mesma unidade atômica. Se o texto protegido exceder 4000 caracteres, retorna 400 sem gravar. Servidor define id, ABERTA, APP e criadaEm UTC. Campos extras são rejeitados. Location aponta ao detalhe.", Ocorrencia, "{\"bairroId\":1,\"categoriaId\":1,\"descricao\":\"Descarte fictício para demonstração.\"}"),
                ("/api/ocorrencias", "GET") => ("Listar ocorrências", "Filtros opcionais combinados por AND. Ordenação criadaEm DESC, id DESC. pagina >= 1 (padrão 1); tamanhoPagina 1–100 (padrão 20); offset deve caber em int32. Lista vazia preserva pagina solicitada e totalPaginas=0 quando não há registros.", "{\"itens\":[" + Ocorrencia + "],\"pagina\":1,\"tamanhoPagina\":20,\"totalRegistros\":1,\"totalPaginas\":1}", null),
                ("/api/ocorrencias/{id}", "GET") => ("Consultar ocorrência", "id inteiro positivo. 404 para ocorrência inexistente.", Ocorrencia, null),
                ("/api/ocorrencias/{id}/status", "PATCH") => ("Atualizar status", "Somente ABERTA → EM_ANALISE → RESOLVIDA. Repetição, salto, reabertura ou concorrência retornam 409. Status e histórico na mesma transação. Executor vem de Auditoria:Executor no servidor; não é aceito no JSON.", Ocorrencia.Replace("ABERTA", "EM_ANALISE"), "{\"status\":\"EM_ANALISE\"}"),
                ("/api/ocorrencias/{id}/historico", "GET") => ("Consultar histórico", "id positivo. Ordenação alteradoEm ASC, id ASC. [] para ocorrência existente sem alterações; 404 para inexistente.", "[{\"id\":1,\"statusAnterior\":\"ABERTA\",\"statusNovo\":\"EM_ANALISE\",\"alteradoEm\":\"2026-11-15T12:05:00Z\",\"executor\":\"zelasp-api\"}]", null),
                _ => ("", "", "null", null)
            };
            operation.Summary = contract.Item1;
            operation.Description = contract.Item2;
            if (operation.RequestBody?.Content is { } requestContent && contract.Item4 is { } request)
                foreach (var media in requestContent.Values) media.Example = JsonNode.Parse(request);
            foreach (var (code, response) in operation.Responses ?? [])
            {
                if (int.TryParse(code, out var statusCode) && statusCode >= 400 && response is OpenApiResponse error)
                {
                    var media = error.Content?.Values.FirstOrDefault() ?? new OpenApiMediaType();
                    error.Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = media };
                }
                var example = code switch
                {
                    "200" or "201" => contract.Item3,
                    "400" => "{\"title\":\"Um ou mais campos são inválidos.\",\"status\":400,\"errors\":{\"campo\":[\"Valor inválido.\"]}}",
                    "404" => "{\"title\":\"Ocorrência não encontrada.\",\"status\":404}",
                    "409" => "{\"title\":\"Transição de status não permitida.\",\"status\":409}",
                    _ => "{\"title\":\"Erro interno ao processar a solicitação.\",\"status\":500}"
                };
                if (response.Content is { } content)
                    foreach (var media in content.Values) media.Example = JsonNode.Parse(example);
                if (code == "201" && response is OpenApiResponse created)
                {
                    created.Headers ??= new Dictionary<string, IOpenApiHeader>();
                    created.Headers["Location"] = new OpenApiHeader
                    {
                        Description = "URL de GET /api/ocorrencias/{id} do recurso criado.",
                        Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                    };
                }
            }
            foreach (var parameter in operation.Parameters ?? [])
            {
                parameter.Description = parameter.Name switch
                {
                    "id" => "Identificador inteiro positivo da ocorrência (obrigatório).",
                    "Pagina" => "Página opcional, a partir de 1; padrão 1.",
                    "TamanhoPagina" => "Quantidade opcional entre 1 e 100; padrão 20.",
                    "BairroId" => "Filtro opcional por identificador positivo de bairro.",
                    "CategoriaId" => "Filtro opcional por identificador positivo de categoria.",
                    "Status" => "Filtro opcional textual: ABERTA, EM_ANALISE ou RESOLVIDA.",
                    _ => parameter.Description
                };
                if (parameter.Name is "Status" && parameter is OpenApiParameter status)
                    status.Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = [JsonValue.Create("ABERTA")!, JsonValue.Create("EM_ANALISE")!, JsonValue.Create("RESOLVIDA")!]
                    };
                if (parameter.In == ParameterLocation.Query && parameter is OpenApiParameter queryParameter && queryParameter.Name is { } name)
                    queryParameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
                if (parameter is OpenApiParameter exampleParameter)
                    exampleParameter.Example = exampleParameter.Name switch
                    {
                        "id" or "pagina" or "bairroId" or "categoriaId" => JsonValue.Create(1),
                        "tamanhoPagina" => JsonValue.Create(20),
                        "status" => JsonValue.Create("EM_ANALISE"),
                        _ => exampleParameter.Example
                    };
            }
        }
        return Task.CompletedTask;
    }
}
