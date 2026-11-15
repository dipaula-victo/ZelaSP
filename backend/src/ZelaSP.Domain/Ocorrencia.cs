namespace ZelaSP.Domain;

public sealed class Ocorrencia
{
    public int Id { get; set; }
    public int BairroId { get; set; }
    public Bairro Bairro { get; set; } = null!;
    public int CategoriaId { get; set; }
    public CategoriaOcorrencia Categoria { get; set; } = null!;
    public string Descricao { get; set; } = string.Empty;
    public StatusOcorrencia Status { get; set; }
    public DateTime CriadaEm { get; set; }
    public OrigemOcorrencia Origem { get; set; }
}
