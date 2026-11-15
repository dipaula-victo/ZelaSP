namespace ZelaSP.Domain;

public sealed class Dispositivo
{
    public int Id { get; set; }
    public int BairroId { get; set; }
    public Bairro Bairro { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
}
