using Microsoft.EntityFrameworkCore;
using ZelaSP.Domain;

namespace ZelaSP.Infrastructure;

public sealed class ZelaSpDbContext(DbContextOptions<ZelaSpDbContext> options) : DbContext(options)
{
    public DbSet<Bairro> Bairros => Set<Bairro>();
    public DbSet<CategoriaOcorrencia> CategoriasOcorrencia => Set<CategoriaOcorrencia>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<ImagemOcorrencia> ImagensOcorrencia => Set<ImagemOcorrencia>();
    public DbSet<HistoricoStatus> HistoricosStatus => Set<HistoricoStatus>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<LeituraSensor> LeiturasSensor => Set<LeituraSensor>();
    public DbSet<EventoSeguranca> EventosSeguranca => Set<EventoSeguranca>();
    public DbSet<MensagemRisco> MensagensRisco => Set<MensagemRisco>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bairro>(b =>
        {
            b.ToTable("BAIRRO", t =>
            {
                t.HasCheckConstraint("CK_BAIRRO_Id", "[id] > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            b.Property(x => x.Subprefeitura).HasColumnName("subprefeitura").HasMaxLength(120).IsRequired();
        });
        modelBuilder.Entity<CategoriaOcorrencia>(b =>
        {
            b.ToTable("CATEGORIA_OCORRENCIA", t =>
            {
                t.HasCheckConstraint("CK_CATEGORIA_OCORRENCIA_Id", "[id] > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        });
        modelBuilder.Entity<Ocorrencia>(b =>
        {
            b.ToTable("OCORRENCIA", t =>
            {
                t.HasCheckConstraint("CK_OCORRENCIA_Id", "[id] > 0");
                t.HasCheckConstraint("CK_OCORRENCIA_Status", "[status] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')");
                t.HasCheckConstraint("CK_OCORRENCIA_Origem", "[origem] IN ('APP', 'SENSOR')");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.BairroId).HasColumnName("bairro_id");
            b.HasOne(x => x.Bairro).WithMany().HasForeignKey(x => x.BairroId).OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.CategoriaId).HasColumnName("categoria_id");
            b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(4000).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            b.Property(x => x.CriadaEm).HasColumnName("criada_em").HasColumnType("datetime2(3)");
            b.Property(x => x.Origem).HasColumnName("origem").HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        });
        modelBuilder.Entity<ImagemOcorrencia>(b =>
        {
            b.ToTable("IMAGEM_OCORRENCIA", t =>
            {
                t.HasCheckConstraint("CK_IMAGEM_OCORRENCIA_Id", "[id] > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.OcorrenciaId).HasColumnName("ocorrencia_id");
            b.HasOne(x => x.Ocorrencia).WithMany().HasForeignKey(x => x.OcorrenciaId).OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(100).IsRequired();
            b.Property(x => x.NomeArquivo).HasColumnName("nome_arquivo").HasMaxLength(255).IsRequired();
            b.Property(x => x.Mime).HasColumnName("mime").HasMaxLength(100).IsRequired();
            b.Property(x => x.Dados).HasColumnName("dados").HasColumnType("varbinary(max)").IsRequired();
            b.Property(x => x.DataUtc).HasColumnName("data_utc").HasColumnType("datetime2(3)");
            b.Property(x => x.RmLider).HasColumnName("rm_lider").HasMaxLength(20).IsRequired();
            b.Property(x => x.Executor).HasColumnName("executor").HasMaxLength(120).IsRequired();
        });
        modelBuilder.Entity<HistoricoStatus>(b =>
        {
            b.ToTable("HISTORICO_STATUS", t =>
            {
                t.HasCheckConstraint("CK_HISTORICO_STATUS_Id", "[id] > 0");
                t.HasCheckConstraint("CK_HISTORICO_STATUS_StatusAnterior", "[status_anterior] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')");
                t.HasCheckConstraint("CK_HISTORICO_STATUS_StatusNovo", "[status_novo] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.OcorrenciaId).HasColumnName("ocorrencia_id");
            b.HasOne(x => x.Ocorrencia).WithMany().HasForeignKey(x => x.OcorrenciaId).OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.StatusAnterior).HasColumnName("status_anterior").HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            b.Property(x => x.StatusNovo).HasColumnName("status_novo").HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            b.Property(x => x.AlteradoEm).HasColumnName("alterado_em").HasColumnType("datetime2(3)");
            b.Property(x => x.Executor).HasColumnName("executor").HasMaxLength(120).IsRequired();
        });
        modelBuilder.Entity<Dispositivo>(b =>
        {
            b.ToTable("DISPOSITIVO", t =>
            {
                t.HasCheckConstraint("CK_DISPOSITIVO_Id", "[id] > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.BairroId).HasColumnName("bairro_id");
            b.HasOne(x => x.Bairro).WithMany().HasForeignKey(x => x.BairroId).OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            b.Property(x => x.Local).HasColumnName("local").HasMaxLength(250).IsRequired();
        });
        modelBuilder.Entity<LeituraSensor>(b =>
        {
            b.ToTable("LEITURA_SENSOR", t =>
            {
                t.HasCheckConstraint("CK_LEITURA_SENSOR_Id", "[id] > 0");
                t.HasCheckConstraint("CK_LEITURA_SENSOR_NivelPercentual", "[nivel_percentual] >= 0 AND [nivel_percentual] <= 100");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.DispositivoId).HasColumnName("dispositivo_id");
            b.HasOne(x => x.Dispositivo).WithMany().HasForeignKey(x => x.DispositivoId).OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.NivelPercentual).HasColumnName("nivel_percentual").HasPrecision(5, 2);
            b.Property(x => x.LidaEm).HasColumnName("lida_em").HasColumnType("datetime2(3)");
        });
        modelBuilder.Entity<EventoSeguranca>(b =>
        {
            b.ToTable("EVENTO_SEGURANCA", t =>
            {
                t.HasCheckConstraint("CK_EVENTO_SEGURANCA_Id", "[id] > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.TipoEvento).HasColumnName("tipo_evento").HasMaxLength(80).IsRequired();
            b.Property(x => x.TipoDado).HasColumnName("tipo_dado").HasMaxLength(80).IsRequired();
            b.Property(x => x.Origem).HasColumnName("origem").HasMaxLength(120).IsRequired();
            b.Property(x => x.Destino).HasColumnName("destino").HasMaxLength(120).IsRequired();
            b.Property(x => x.Acao).HasColumnName("acao").HasMaxLength(120).IsRequired();
            b.Property(x => x.OcorridoEm).HasColumnName("ocorrido_em").HasColumnType("datetime2(3)");
        });
        modelBuilder.Entity<MensagemRisco>(b =>
        {
            b.ToTable("MENSAGEM_RISCO", t =>
            {
                t.HasCheckConstraint("CK_MENSAGEM_RISCO_Id", "[id] > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            b.Property(x => x.EventoSegurancaId).HasColumnName("evento_seguranca_id");
            b.HasOne(x => x.EventoSeguranca).WithOne(x => x.MensagemRisco)
                .HasForeignKey<MensagemRisco>(x => x.EventoSegurancaId)
                .IsRequired().OnDelete(DeleteBehavior.NoAction);
            b.Property(x => x.Mensagem).HasColumnName("mensagem").HasMaxLength(2000).IsRequired();
            b.Property(x => x.OrigemMensagem).HasColumnName("origem_mensagem").HasMaxLength(40).IsRequired();
            b.Property(x => x.StatusTraducao).HasColumnName("status_traducao").HasMaxLength(40).IsRequired();
            b.Property(x => x.GeradaEm).HasColumnName("gerada_em").HasColumnType("datetime2(3)");
        });
    }
}
