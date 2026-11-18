namespace ZelaSP.Domain;

public static class RegraNivelCritico
{
    public static bool EhCritico(decimal nivelPercentual) => nivelPercentual >= 90.00m;
}
