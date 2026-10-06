using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MandiocaCaipira.App.Services;

public static partial class NormalizadorNomeCliente
{
    private static readonly HashSet<string> Conectivos = new(StringComparer.OrdinalIgnoreCase)
        { "da", "das", "de", "do", "dos", "e" };

    public static string LimparParaExibicao(string? nome) =>
        Espacos().Replace(nome?.Trim() ?? string.Empty, " ");

    public static string NormalizarParaApresentacao(string? nome)
    {
        var limpo = LimparParaExibicao(nome);
        if (limpo.Length == 0) return string.Empty;

        var textInfo = CultureInfo.GetCultureInfo("pt-BR").TextInfo;
        var palavras = limpo.Split(' ');
        for (var index = 0; index < palavras.Length; index++)
        {
            var minuscula = palavras[index].ToLower(CultureInfo.GetCultureInfo("pt-BR"));
            palavras[index] = index > 0 && Conectivos.Contains(minuscula)
                ? minuscula
                : textInfo.ToTitleCase(minuscula);
        }
        return string.Join(' ', palavras);
    }

    public static string NormalizarParaComparacao(string? nome)
    {
        var limpo = LimparParaExibicao(nome).Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(limpo.Length);
        var separadorPendente = false;

        foreach (var caractere in limpo)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(caractere))
            {
                if (separadorPendente && resultado.Length > 0)
                    resultado.Append(' ');
                resultado.Append(char.ToUpperInvariant(caractere));
                separadorPendente = false;
            }
            else
                separadorPendente = true;
        }

        return resultado.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();
}
