using ZelaSP.Infrastructure;
using ZelaSP.Application;
using ZelaSP.Api;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    builder.Services.AddCors(options => options.AddPolicy("ExpoRiskDevelopment", policy =>
        policy.WithOrigins("http://127.0.0.1:8081").WithMethods("GET")));
// Framework diagnostics can include request values or provider exceptions. Do not emit
// these categories until a structured, sanitized diagnostic pipeline is implemented.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.None);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
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
builder.Services.Configure<RiscoAiOptions>(builder.Configuration.GetSection("RiscoAi"));
builder.Services.AddHttpClient<IExplicacaoCidadaClient, OpenAiExplicacaoCidadaClient>(http => http.Timeout = System.Threading.Timeout.InfiniteTimeSpan)
    .RemoveAllLoggers()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });
builder.Services.AddScoped<RiscoCidadaoService>();
builder.Services.AddScoped<IoTService>();
var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseRouting();
    // Exact path and method: do not enable browser access to other API routes.
    app.UseWhen(context => string.Equals(context.Request.Path.Value, "/api/risco-cidadao", StringComparison.Ordinal) &&
        (HttpMethods.IsGet(context.Request.Method) ||
         (HttpMethods.IsOptions(context.Request.Method) &&
          string.Equals(context.Request.Headers["Access-Control-Request-Method"].ToString(), "GET", StringComparison.Ordinal))),
        branch => branch.UseCors("ExpoRiskDevelopment"));
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program { }
