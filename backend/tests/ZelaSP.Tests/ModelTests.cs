using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;
using Xunit;

namespace ZelaSP.Tests;

public sealed class ModelTests
{
    private static ZelaSpDbContext CreateContext() => new(new DbContextOptionsBuilder<ZelaSpDbContext>()
        .UseSqlServer("Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true").Options);

    [Fact]
    public void ModelHasNineEntitiesAndSevenRequiredNonCascadingRelationships()
    {
        using var db = CreateContext();
        var entities = db.Model.GetEntityTypes().ToArray();
        Assert.Equal(9, entities.Length);
        Assert.All(entities, e => Assert.Single(e.FindPrimaryKey()!.Properties));
        var keys = entities.SelectMany(e => e.GetForeignKeys()).ToArray();
        Assert.Equal(7, keys.Length);
        Assert.All(keys, k => { Assert.True(k.IsRequired); Assert.Equal(DeleteBehavior.NoAction, k.DeleteBehavior); });
        var expected = new[]
        {
            (typeof(Ocorrencia), typeof(Bairro), "BairroId"),
            (typeof(Ocorrencia), typeof(CategoriaOcorrencia), "CategoriaId"),
            (typeof(ImagemOcorrencia), typeof(Ocorrencia), "OcorrenciaId"),
            (typeof(HistoricoStatus), typeof(Ocorrencia), "OcorrenciaId"),
            (typeof(Dispositivo), typeof(Bairro), "BairroId"),
            (typeof(LeituraSensor), typeof(Dispositivo), "DispositivoId"),
            (typeof(MensagemRisco), typeof(EventoSeguranca), "EventoSegurancaId")
        };
        foreach (var (dependent, principal, property) in expected)
            Assert.Contains(keys, k => k.DeclaringEntityType.ClrType == dependent &&
                k.PrincipalEntityType.ClrType == principal && k.Properties.Single().Name == property);
        // No required image reference on an occurrence: zero images are permitted.
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(Ocorrencia))!.GetForeignKeys(),
            k => k.PrincipalEntityType.ClrType == typeof(ImagemOcorrencia));
    }

    [Fact]
    public void SqlServerTypesConstraintsAndEnumConversionsAreConfigured()
    {
        using var db = CreateContext();
        var model = db.GetService<IDesignTimeModel>().Model;
        var image = model.FindEntityType(typeof(ImagemOcorrencia))!;
        Assert.Equal("varbinary(max)", image.FindProperty("Dados")!.GetColumnType());
        var reading = model.FindEntityType(typeof(LeituraSensor))!;
        Assert.Equal(5, reading.FindProperty("NivelPercentual")!.GetPrecision());
        Assert.Equal(2, reading.FindProperty("NivelPercentual")!.GetScale());
        Assert.Contains(reading.GetCheckConstraints(), c => c.Sql.Contains("<= 100"));
        foreach (var entity in model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            if (property.ClrType == typeof(string)) Assert.NotNull(property.GetMaxLength());
            if (property.ClrType == typeof(DateTime)) Assert.Equal("datetime2(3)", property.GetColumnType());
            if (property.ClrType.IsEnum) Assert.Equal(typeof(string), property.GetTypeMapping().Converter!.ProviderClrType);
        }
    }

    [Theory]
    [InlineData(0)] [InlineData(50.25)] [InlineData(100)]
    public void ValidPercentagesAreAccepted(decimal value) => Assert.Equal(value, new LeituraSensor { NivelPercentual = value }.NivelPercentual);

    [Theory]
    [InlineData(-0.01)] [InlineData(100.01)]
    public void InvalidPercentagesAreRejected(decimal value) => Assert.Throws<ArgumentOutOfRangeException>(() => new LeituraSensor { NivelPercentual = value });

    [Fact]
    public void ImageTitleIsRequiredAndLimitedToOneHundredCharacters()
    {
        using var db = CreateContext();
        var title = db.Model.FindEntityType(typeof(ImagemOcorrencia))!.FindProperty(nameof(ImagemOcorrencia.Titulo))!;
        Assert.False(title.IsNullable);
        Assert.Equal(100, title.GetMaxLength());
        Assert.Equal("nvarchar(100)", title.GetColumnType());
        // Validation of the input's minimum length belongs to the future upload endpoint.
    }

    [Fact]
    public void SecurityEventHasAtMostOneOptionalMessageWithRequiredUniqueForeignKey()
    {
        using var db = CreateContext();
        var message = db.Model.FindEntityType(typeof(MensagemRisco))!;
        var key = Assert.Single(message.GetForeignKeys());
        Assert.Equal(typeof(EventoSeguranca), key.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(MensagemRisco.EventoSegurancaId), Assert.Single(key.Properties).Name);
        Assert.True(key.IsRequired);
        Assert.False(key.Properties.Single().IsNullable);
        Assert.True(key.IsUnique);
        Assert.False(key.IsRequiredDependent);
        Assert.Equal(DeleteBehavior.NoAction, key.DeleteBehavior);
        Assert.Equal(nameof(EventoSeguranca.MensagemRisco), key.PrincipalToDependent!.Name);
        Assert.False(key.PrincipalToDependent.IsCollection);
        Assert.Equal(nameof(MensagemRisco.EventoSeguranca), key.DependentToPrincipal!.Name);
        var index = Assert.Single(message.GetIndexes(), i => i.Properties.SequenceEqual(key.Properties));
        Assert.True(index.IsUnique);
        Assert.Null(index.GetFilter());
        Assert.Null(new EventoSeguranca().MensagemRisco);
    }

    [Fact]
    public void InitialMigrationMatchesModelAndGeneratesNineTablesOffline()
    {
        using var db = CreateContext();
        Assert.Single(db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript();
        Assert.Equal(9, sql.Split("CREATE TABLE [").Skip(1).Count(x => !x.StartsWith("__EFMigrationsHistory")));
        Assert.Contains("varbinary(max)", sql);
        Assert.Contains("[titulo] nvarchar(100) NOT NULL", sql);
        Assert.DoesNotContain("[titulo] nvarchar(200)", sql);
        Assert.Contains("[evento_seguranca_id] int NOT NULL", sql);
        Assert.Contains("FOREIGN KEY ([evento_seguranca_id]) REFERENCES [EVENTO_SEGURANCA] ([id])", sql);
        Assert.Contains("CREATE UNIQUE INDEX [IX_MENSAGEM_RISCO_evento_seguranca_id] ON [MENSAGEM_RISCO] ([evento_seguranca_id]);", sql);
        Assert.DoesNotContain("ON DELETE CASCADE", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }
}
