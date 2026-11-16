using System.Buffers;

namespace ZelaSP.Application;

public static class LimitesImagem
{
    public const int MaximoBytes = 2 * 1024 * 1024;

    // Consume at most limit + 1 bytes, independent of headers, Length or seek support.
    public static async Task CopiarLimitadoAsync(Stream origem, Stream destino, int limite, CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        var total = 0;
        try
        {
            while (true)
            {
                var read = await origem.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, limite - total + 1)), ct);
                if (read == 0) break;
                if (total + read > limite) throw new ConteudoMuitoGrandeException();
                await destino.WriteAsync(buffer.AsMemory(0, read), ct);
                total += read;
            }
        }
        finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
    }
}
