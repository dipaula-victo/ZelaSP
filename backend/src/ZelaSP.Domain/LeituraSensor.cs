namespace ZelaSP.Domain;

public sealed class LeituraSensor
{
    public int Id { get; set; }
    public int DispositivoId { get; set; }
    public Dispositivo Dispositivo { get; set; } = null!;
    private decimal _nivelPercentual;
    public decimal NivelPercentual
    {
        get => _nivelPercentual;
        set => _nivelPercentual = value is >= 0 and <= 100 ? value : throw new ArgumentOutOfRangeException(nameof(value), "O percentual deve estar entre 0 e 100.");
    }
    public DateTime LidaEm { get; set; }
}
