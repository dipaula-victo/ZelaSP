namespace ZelaSP.Domain;

/// <summary>
/// Metadados de segurança. Nunca preencher os campos com valores pessoais originais,
/// relatos, imagens ou conteúdo capturado; utilizar apenas classificações e identificadores técnicos.
/// A detecção e sanitização de PII pertencem à futura tarefa de Cybersecurity.
/// </summary>
public sealed class EventoSeguranca
{
    public int Id { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string TipoDado { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public DateTime OcorridoEm { get; set; }
    public MensagemRisco? MensagemRisco { get; set; }
}
