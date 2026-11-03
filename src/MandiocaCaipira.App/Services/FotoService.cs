using System.Globalization;

namespace MandiocaCaipira.App.Services;

public sealed class FotoService
{
    private static readonly HashSet<string> ExtensoesAceitas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private readonly string _diretorioFotos;

    public FotoService(string diretorioFotos)
    {
        _diretorioFotos = diretorioFotos;
    }

    public string Copiar(string caminhoOrigem, long compraId)
    {
        var extensao = Path.GetExtension(caminhoOrigem);
        if (!ExtensoesAceitas.Contains(extensao))
            throw new ArgumentException("Formato de foto não aceito. Use .jpg, .jpeg ou .png.");

        if (!File.Exists(caminhoOrigem))
            throw new FileNotFoundException("Arquivo de foto não encontrado.");

        Directory.CreateDirectory(_diretorioFotos);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var nomeDestino = $"compra_{compraId}_{timestamp}{extensao}";
        var caminhoDestino = Path.Combine(_diretorioFotos, nomeDestino);
        File.Copy(caminhoOrigem, caminhoDestino, overwrite: true);
        return nomeDestino;
    }

    public string CaminhoCompleto(string nomeRelativo)
    {
        var completo = Path.GetFullPath(Path.Combine(_diretorioFotos, nomeRelativo));
        var diretorio = Path.GetFullPath(_diretorioFotos) + Path.DirectorySeparatorChar;
        if (!completo.StartsWith(diretorio, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Caminho de foto inválido.");
        return completo;
    }

    public bool Existe(string? nomeRelativo) =>
        nomeRelativo is not null && File.Exists(CaminhoCompleto(nomeRelativo));
}
