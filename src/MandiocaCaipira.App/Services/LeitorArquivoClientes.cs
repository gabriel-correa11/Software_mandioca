using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;

namespace MandiocaCaipira.App.Services;

public sealed class LeitorArquivoClientes
{
    private static readonly string[] CabecalhosNome = ["NOME", "CLIENTE", "NOME CLIENTE", "NOME DO CLIENTE"];

    public Task<IReadOnlyList<string>> LerAsync(string caminho, CancellationToken cancellationToken = default) =>
        Task.Run(() => Ler(caminho, cancellationToken), cancellationToken);

    private static IReadOnlyList<string> Ler(string caminho, CancellationToken cancellationToken)
    {
        if (!File.Exists(caminho))
            throw new FileNotFoundException("O arquivo selecionado não foi encontrado.", caminho);

        return Path.GetExtension(caminho).ToLowerInvariant() switch
        {
            ".xlsx" => LerXlsx(caminho, cancellationToken),
            ".csv" => LerDelimitado(caminho, cancellationToken),
            ".txt" => LerTexto(caminho, cancellationToken),
            _ => throw new NotSupportedException("Selecione um arquivo XLSX, CSV ou TXT.")
        };
    }

    private static IReadOnlyList<string> LerXlsx(string caminho, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(caminho);
        var nomes = new List<string>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var rows = worksheet.RowsUsed().ToList();
            if (rows.Count == 0) continue;
            var headerCell = rows[0].CellsUsed().FirstOrDefault(cell =>
                CabecalhosNome.Contains(NormalizadorNomeCliente.NormalizarParaComparacao(cell.GetString())));
            var column = headerCell?.Address.ColumnNumber ?? -1;
            var start = column > 0 ? 1 : 0;
            for (var index = start; index < rows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = column > 0 ? rows[index].Cell(column).GetString() : rows[index].CellsUsed().FirstOrDefault()?.GetString();
                Adicionar(nomes, value);
            }
        }
        return nomes;
    }

    private static IReadOnlyList<string> LerDelimitado(string caminho, CancellationToken cancellationToken)
    {
        var delimiter = DetectarDelimitador(caminho);
        using var parser = new TextFieldParser(caminho) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(delimiter);
        var records = new List<string[]>();
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            records.Add(parser.ReadFields() ?? []);
        }
        if (records.Count == 0) return [];
        var column = EncontrarColuna(records[0]);
        var result = new List<string>();
        for (var index = column >= 0 ? 1 : 0; index < records.Count; index++)
            Adicionar(result, column >= 0 && column < records[index].Length ? records[index][column] : records[index].FirstOrDefault());
        return result;
    }

    private static IReadOnlyList<string> LerTexto(string caminho, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        foreach (var line in File.ReadLines(caminho))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Adicionar(result, line);
        }
        if (result.Count > 0 && CabecalhosNome.Contains(NormalizadorNomeCliente.NormalizarParaComparacao(result[0])))
            result.RemoveAt(0);
        return result;
    }

    private static int EncontrarColuna(IReadOnlyList<string> cells)
    {
        for (var index = 0; index < cells.Count; index++)
            if (CabecalhosNome.Contains(NormalizadorNomeCliente.NormalizarParaComparacao(cells[index])))
                return index;
        return -1;
    }

    private static string DetectarDelimitador(string caminho)
    {
        var first = File.ReadLines(caminho).FirstOrDefault() ?? string.Empty;
        return new[] { ";", ",", "\t" }.OrderByDescending(delimiter => first.Count(c => c == delimiter[0])).First();
    }

    private static void Adicionar(List<string> nomes, string? value)
    {
        var limpo = NormalizadorNomeCliente.LimparParaExibicao(value);
        if (limpo.Length >= 2 && limpo.Any(char.IsLetter))
            nomes.Add(limpo);
    }
}
