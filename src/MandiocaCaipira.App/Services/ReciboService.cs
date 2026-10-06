using System.Globalization;
using MandiocaCaipira.App.Models;
using MandiocaCaipira.App.Repositories;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MandiocaCaipira.App.Services;

public sealed class ReciboService(
    VendaRepository vendas,
    PagamentoRepository pagamentos,
    string diretorioRecibos)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public string GerarVenda(long vendaId)
    {
        var venda = vendas.Obter(vendaId);
        var caminho = Caminho("venda", venda.Id, venda.DataOperacao);
        Gerar(caminho, "COMPROVANTE DE VENDA", $"Venda nº {venda.Id:000000}", venda.Cliente,
            venda.DataOperacao,
            [
                ("Quantidade", Kg(venda.QuantidadeGramas)),
                ("Preço por kg", Moeda(venda.PrecoCentavosPorKg)),
                ("Valor total", Moeda(venda.TotalCentavos)),
                ("Valor recebido", Moeda(venda.RecebidoCentavos)),
                ("Saldo restante", Moeda(venda.SaldoCentavos)),
                ("Situação", venda.Cancelada ? "Cancelada" : venda.SaldoCentavos == 0 ? "Paga" : venda.RecebidoCentavos > 0 ? "Pagamento parcial" : "Pendente")
            ], venda.Cancelada ? $"Venda cancelada. Motivo: {venda.MotivoCancelamento}" : null);
        return caminho;
    }

    public string GerarPagamento(long pagamentoId)
    {
        var documento = pagamentos.ObterDocumento(pagamentoId);
        var p = documento.Pagamento;
        var linhas = new List<(string, string)>
        {
            ("Valor recebido", Moeda(p.ValorCentavos)),
            ("Valor destinado a vendas", Moeda(p.AlocadoCentavos)),
            ("Crédito não utilizado", Moeda(p.CreditoCentavos))
        };
        foreach (var item in documento.Destinacoes)
            linhas.Add(($"Venda nº {item.VendaId:000000} de {item.DataVenda:dd/MM/yyyy}",
                $"{Moeda(item.ValorAplicadoCentavos)} de {Moeda(item.ValorVendaCentavos)}"));
        var observacao = p.EstornadoEm is null
            ? "Pagamento registrado no sistema Mandioca Caipira."
            : $"PAGAMENTO ESTORNADO. Motivo: {p.MotivoEstorno}";
        var caminho = Caminho("pagamento", p.Id, p.DataOperacao);
        Gerar(caminho, "RECIBO / COMPROVANTE DE PAGAMENTO", $"Recibo nº {p.Id:000000}",
            p.Cliente, p.DataOperacao, linhas, observacao);
        return caminho;
    }

    private string Caminho(string tipo, long id, DateOnly data)
    {
        Directory.CreateDirectory(diretorioRecibos);
        return Path.Combine(diretorioRecibos, $"{tipo}-{id:000000}-{data:yyyyMMdd}.pdf");
    }

    private static void Gerar(string caminho, string titulo, string numero, string cliente,
        DateOnly data, IReadOnlyList<(string Rotulo, string Valor)> linhas, string? observacao)
    {
        using var document = new PdfDocument();
        document.Info.Title = titulo;
        document.Info.Author = "Mandioca Caipira";
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        using var gfx = XGraphics.FromPdfPage(page);
        var dark = XColor.FromArgb(45, 55, 48);
        var muted = XColor.FromArgb(95, 103, 98);
        var green = XColor.FromArgb(51, 103, 68);
        var titleFont = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
        var headingFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
        var normalFont = new XFont("Segoe UI", 10);
        var smallFont = new XFont("Segoe UI", 8.5);
        const double left = 58, right = 537;
        gfx.DrawString("MANDIOCA CAIPIRA", titleFont, new XSolidBrush(green), new XPoint(left, 65));
        gfx.DrawString(titulo, headingFont, new XSolidBrush(dark), new XPoint(left, 91));
        gfx.DrawLine(new XPen(green, 1.5), left, 105, right, 105);
        gfx.DrawString(numero, normalFont, XBrushes.Black, new XPoint(left, 134));
        gfx.DrawString($"Data: {data:dd/MM/yyyy}", normalFont, XBrushes.Black, new XPoint(350, 134));
        gfx.DrawString("Cliente", smallFont, new XSolidBrush(muted), new XPoint(left, 166));
        gfx.DrawString(cliente, headingFont, XBrushes.Black, new XPoint(left, 184));
        double y = 220;
        foreach (var (rotulo, valor) in linhas)
        {
            gfx.DrawString(rotulo, normalFont, new XSolidBrush(dark), new XPoint(left, y));
            gfx.DrawString(valor, normalFont, XBrushes.Black,
                new XRect(300, y - 14, right - 300, 20), XStringFormats.TopRight);
            y += 27;
            gfx.DrawLine(new XPen(XColor.FromArgb(225, 228, 226), .5), left, y - 10, right, y - 10);
        }
        if (!string.IsNullOrWhiteSpace(observacao))
        {
            y += 14;
            gfx.DrawString(observacao, smallFont, new XSolidBrush(muted),
                new XRect(left, y, right - left, 50), XStringFormats.TopLeft);
        }
        gfx.DrawLine(new XPen(XColor.FromArgb(190, 195, 191), .7), left, 745, right, 745);
        gfx.DrawString("Comprovante não fiscal. Emitido a partir dos registros locais do sistema.",
            smallFont, new XSolidBrush(muted), new XRect(left, 758, right - left, 20), XStringFormats.TopCenter);
        document.Save(caminho);
    }

    private static string Moeda(long centavos) => (centavos / 100m).ToString("C", PtBr);
    private static string Kg(long gramas) => $"{gramas / 1000m:N3} kg";
}
