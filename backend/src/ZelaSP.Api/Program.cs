using ZelaSP.Infrastructure;
using ZelaSP.Application;
using ZelaSP.Api;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
// Framework diagnostics can include request values or provider exceptions. Do not emit
// these categories until a structured, sanitized diagnostic pipeline is implemented.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.None);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.AllowInputFormatterExceptionMessages = false;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = _ =>
    new BadRequestObjectResult(new HttpValidationProblemDetails(new Dictionary<string, string[]>
    {
        ["request"] = ["Requisição inválida. Confira os campos, tipos e limites do contrato."]
    }) { Status = 400, Title = "Um ou mais campos são inválidos." }));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ContratoOpenApiTransformer>();
    options.AddSchemaTransformer<ContratoOpenApiTransformer>();
    options.AddDocumentTransformer<ImagensOpenApiTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var connection = builder.Configuration.GetConnectionString("ZelaSP");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Configure ConnectionStrings:ZelaSP por variável de ambiente ou user-secrets.");
builder.Services.AddInfrastructure(connection);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new IdentidadeAuditoria(builder.Configuration["Auditoria:Executor"] ?? ""));
builder.Services.AddScoped<OcorrenciasService>();
builder.Services.AddSingleton(new EquipeAcademica(builder.Configuration["Academico:RmLider"] ?? "",
    builder.Configuration.GetSection("Academico:Executores").Get<string[]>() ?? []));
builder.Services.AddScoped<ImagensService>();
var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program { }
