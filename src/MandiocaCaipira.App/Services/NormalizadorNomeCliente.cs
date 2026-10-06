using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MandiocaCaipira.App.Services;

public static partial class NormalizadorNomeCliente
{
    public static string LimparParaExibicao(string? nome) =>
        Espacos().Replace(nome?.Trim() ?? string.Empty, " ");

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
