namespace ZelaSP.Domain;

public sealed class ImagemOcorrencia
{
    public int Id { get; set; }
    public int OcorrenciaId { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
    public string Titulo { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public string Mime { get; set; } = string.Empty;
    public byte[] Dados { get; set; } = [];
    public DateTime DataUtc { get; set; }
    public string RmLider { get; set; } = string.Empty;
    public string Executor { get; set; } = string.Empty;
}
