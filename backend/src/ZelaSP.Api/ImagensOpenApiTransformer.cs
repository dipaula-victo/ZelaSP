using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using ZelaSP.Application;

namespace ZelaSP.Api;

public sealed class ImagensOpenApiTransformer(EquipeAcademica equipe) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info.Title = "ZelaSP — API de ocorrências e imagens";
        document.Info.Description = "TASK-002 e TASK-003. Ocorrências e imagens; datas UTC; erros Problem Details. Identificação acadêmica não é autenticação. Contrato: backend/docs/API_CONTRACT.md.";
        foreach (var (path, item) in document.Paths.Where(x => x.Key.StartsWith("/api/imagens", StringComparison.Ordinal)))
        foreach (var (method, operation) in item.Operations ?? [])
        {
            var upload = method.ToString().Equals("Post", StringComparison.OrdinalIgnoreCase);
            var binary = path.EndsWith("/conteudo", StringComparison.Ordinal);
            operation.Summary = upload ? "Enviar imagem" : binary ? "Recuperar conteúdo binário" : "Consultar metadados da imagem";
            operation.Description = upload
                ? "multipart/form-data: ocorrenciaId positivo existente, titulo de 1–100 após trim, executor exato da equipe e um arquivo PNG/JPEG até 2.097.152 bytes. Extensão/MIME/assinatura devem corresponder. Servidor define RM do líder e UTC. Campos extras/duplicados são rejeitados. Sem autenticação. Assinatura não garante integridade completa."
                : binary ? "Bytes originais recuperados do banco como stream; image/png ou image/jpeg; nunca utiliza caminho local."
                : "Metadados sem Dados/bytes. ID positivo; 404 quando inexistente.";
            if (upload)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new()
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object, AdditionalPropertiesAllowed = false,
                                Required = new HashSet<string> { "ocorrenciaId", "titulo", "executor", "arquivo" },
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["ocorrenciaId"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = "1", Maximum = "2147483647" },
                                    ["titulo"] = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 100, Description = "Limites aplicados após trim." },
                                    ["executor"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = equipe.Executores.Select(x => (JsonNode)JsonValue.Create(x)!).ToList(), Description = "Identificador acadêmico exato; não comprova autenticação." },
                                    ["arquivo"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary", MaxLength = LimitesImagem.MaximoBytes, Description = "PNG (.png) ou JPEG (.jpg/.jpeg), até 2.097.152 bytes. MIME, extensão e assinatura devem corresponder." }
                                }
                            },
                            Encoding = new Dictionary<string, OpenApiEncoding>
                            {
                                ["arquivo"] = new() { ContentType = "image/png, image/jpeg" }
                            }
                        }
                    }
                };
            }
            var metadata = new JsonObject
            {
                ["id"] = 1, ["ocorrenciaId"] = 1, ["titulo"] = "Imagem fictícia",
                ["nomeArquivo"] = "ficticia.png", ["mime"] = "image/png", ["tamanhoBytes"] = 68,
                ["dataUtc"] = "2026-11-16T12:00:00Z", ["rmLider"] = equipe.RmLider, ["executor"] = equipe.Executores[0]
            };
            foreach (var (code, response) in operation.Responses ?? [])
            {
                if (response is not OpenApiResponse editable) continue;
                if (code == "200" && binary)
                {
                    editable.Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["image/png"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } },
                        ["image/jpeg"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } }
                    };
                }
                else if (code is "200" or "201")
                {
                    foreach (var media in editable.Content?.Values ?? []) media.Example = metadata.DeepClone();
                }
                else
                {
                    var media = editable.Content?.Values.FirstOrDefault() ?? new OpenApiMediaType();
                    media.Example = JsonNode.Parse(code switch
                    {
                        "400" => "{\"title\":\"Um ou mais campos são inválidos.\",\"status\":400,\"errors\":{\"arquivo\":[\"Formato incompatível.\"]}}",
                        "404" => "{\"title\":\"Imagem ou ocorrência não encontrada.\",\"status\":404}",
                        "413" => "{\"title\":\"O arquivo ou corpo da requisição excede o limite permitido.\",\"status\":413}",
                        _ => "{\"title\":\"Erro interno ao processar a solicitação.\",\"status\":500}"
                    });
                    editable.Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = media };
                }
                if (code == "201")
                {
                    editable.Headers ??= new Dictionary<string, IOpenApiHeader>();
                    editable.Headers["Location"] = new OpenApiHeader
                    {
                        Description = "GET /api/imagens/{id} da imagem criada.", Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                    };
                }
            }
        }
        return Task.CompletedTask;
    }
}
