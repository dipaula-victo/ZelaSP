namespace ZelaSP.Domain;

public sealed class Bairro
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Subprefeitura { get; set; } = string.Empty;
}
