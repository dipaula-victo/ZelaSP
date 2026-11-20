using ZelaSP.Domain;

namespace ZelaSP.Application;

public static class ValidadorExplicacaoCidada
{
    public const int MaximoCaracteres = TextoClassificado.MaximoCaracteres;
    public static bool Aceita(string? texto) => TextoClassificado.Classificar(texto).TentarPublicar(out _);
}

public sealed class RiscoCidadaoService(IRiscoCidadaoStore store, IExplicacaoCidadaClient client, TimeProvider clock)
{
    public async Task<RiscoCidadaoResponse> ObterAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var fim = clock.GetUtcNow().UtcDateTime;
        var inicio = fim.AddDays(-7);
        var contagens = await store.ContarAsync(inicio, fim, ct);
        var (pontos, nivel) = CalculadoraRiscoCidadao.Calcular(contagens);
        var texto = await client.GerarAsync(new(pontos, nivel, contagens), ct);
        ct.ThrowIfCancellationRequested();
        var aceita = TextoClassificado.Classificar(texto).TentarPublicar(out var textoLiberado);
        var fallback = pontos == 0
            ? "Nenhum evento reconhecido de exposição de dados foi registrado nos últimos sete dias. O indicador não representa perigo no bairro nem garante ausência de riscos."
            : $"O indicador está em nível {nivel} e soma {pontos} pontos nos últimos sete dias. Informações pessoais foram detectadas ou tiveram seu envio bloqueado. Evite incluí-las nos relatos. O indicador não representa perigo no bairro.";
        var alertas = new List<AlertaRiscoResponse>();
        if (contagens.Cpf > 0) alertas.Add(new("CPF", "Um CPF foi digitado em um relato e foi ocultado. O CPF pode ser usado em fraudes: não o informe em relatos."));
        if (contagens.Telefone > 0) alertas.Add(new("TELEFONE", "Um telefone foi digitado em um relato e foi ocultado. Evite informar telefone em relatos."));
        if (contagens.Email > 0) alertas.Add(new("EMAIL", "Um e-mail foi digitado em um relato e foi ocultado. Evite informar e-mail em relatos."));
        if (contagens.Senha > 0) alertas.Add(new("SENHA", "Uma senha foi digitada em um relato e foi ocultada. Se for sua, troque-a agora."));
        if (contagens.Saude > 0) alertas.Add(new("SAUDE", "Uma informação de saúde foi digitada em um relato e foi ocultada. Dados de saúde são sensíveis: não os informe."));
        if (contagens.VazamentosBloqueados > 0) alertas.Add(new("VAZAMENTO", "O sistema impediu que um dado pessoal aparecesse em um local inseguro."));
        if (!aceita)
        {
            // The deterministic fallback must cross the same publication boundary.
            if (!TextoClassificado.Classificar(fallback).TentarPublicar(out textoLiberado))
                throw new InvalidOperationException("Explicação indisponível para publicação segura.");
        }
        return new(pontos, nivel, 7, new(contagens.Cpf, contagens.Telefone, contagens.Email, contagens.Senha, contagens.Saude),
            contagens.VazamentosBloqueados, alertas, new(inicio, fim), new(textoLiberado!, aceita ? "LLM" : "FALLBACK"));
    }
}
