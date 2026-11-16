using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ZelaSP.Application;

public sealed class EquipeAcademica
{
    public string RmLider { get; }
    public ReadOnlyCollection<string> Executores { get; }

    public EquipeAcademica(string rmLider, IEnumerable<string> executores)
    {
        var membros = executores.ToArray();
        if (!Regex.IsMatch(rmLider, "^[0-9]{6}$") || membros.Length == 0 ||
            membros.Distinct(StringComparer.Ordinal).Count() != membros.Length ||
            membros.Any(x => x is null || x.Length > 120 || !Regex.IsMatch(x, "^[A-Za-z]+_RM[0-9]{6}$")) ||
            !membros.Any(x => x.EndsWith("_RM" + rmLider, StringComparison.Ordinal)))
            throw new ArgumentException("Configure a equipe acadêmica e o RM do líder corretamente.");
        RmLider = rmLider;
        Executores = Array.AsReadOnly(membros);
    }

    public bool Permite(string? executor) => executor is not null && Executores.Contains(executor, StringComparer.Ordinal);
}
