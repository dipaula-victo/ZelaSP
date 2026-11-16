using ZelaSP.Infrastructure;
using ZelaSP.Application;
using ZelaSP.Api;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.AllowInputFormatterExceptionMessages = false;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ContratoOpenApiTransformer>();
    options.AddSchemaTransformer<ContratoOpenApiTransformer>();
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
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program { }
