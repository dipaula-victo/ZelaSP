namespace ZelaSP.Domain;

public sealed class HistoricoStatus
{
    public int Id { get; set; }
    public int OcorrenciaId { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
    public StatusOcorrencia StatusAnterior { get; set; }
    public StatusOcorrencia StatusNovo { get; set; }
    public DateTime AlteradoEm { get; set; }
    public string Executor { get; set; } = string.Empty;
}
