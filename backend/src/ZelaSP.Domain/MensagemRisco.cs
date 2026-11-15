namespace ZelaSP.Domain;

public sealed class MensagemRisco
{
    public int Id { get; set; }
    public int EventoSegurancaId { get; set; }
    public EventoSeguranca EventoSeguranca { get; set; } = null!;
    public string Mensagem { get; set; } = string.Empty;
    public string OrigemMensagem { get; set; } = string.Empty;
    public string StatusTraducao { get; set; } = string.Empty;
    public DateTime GeradaEm { get; set; }
}
