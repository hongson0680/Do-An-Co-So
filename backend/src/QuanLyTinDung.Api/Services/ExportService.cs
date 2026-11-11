using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuanLyTinDung.Api.Services;

public interface IExportService
{
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName, string title,
        Dictionary<string, Func<T, object?>> columnMappings);

    byte[] ExportToPdf<T>(IEnumerable<T> data, string title,
        Dictionary<string, Func<T, object?>> columnMappings);
}

public class ExportService : IExportService
{
    public ExportService()
    {
        // Cấu hình license cho QuestPDF (Community - miễn phí cho doanh nghiệp < $1M revenue)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportToExcel<T>(
        IEnumerable<T> data,
        string sheetName,
        string title,
        Dictionary<string, Func<T, object?>> columnMappings)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        // Tiêu đề lớn
        sheet.Cell("A1").Value = title;
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 16;
        sheet.Range($"A1:{GetColumnLetter(columnMappings.Count)}1").Merge();
        sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Row(1).Height = 25;

        // Ngày xuất
        sheet.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Cell("A2").Style.Font.Italic = true;
        sheet.Cell("A2").Style.Font.FontSize = 10;
        sheet.Range($"A2:{GetColumnLetter(columnMappings.Count)}2").Merge();
        sheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // Header row (bắt đầu từ dòng 4)
        int headerRow = 4;
        int col = 1;
        foreach (var key in columnMappings.Keys)
        {
            var cell = sheet.Cell(headerRow, col);
            cell.Value = key;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            col++;
        }
        sheet.Row(headerRow).Height = 22;

        // Data rows
        int row = headerRow + 1;
        int stt = 1;
        foreach (var item in data)
        {
            col = 1;
            foreach (var mapping in columnMappings.Values)
            {
                var value = mapping(item);
                var cell = sheet.Cell(row, col);
                if (value is DateTime dt) cell.Value = dt;
                else if (value is DateTime?) cell.Value = (DateTime)(value as DateTime?)!;
                else if (value is decimal d) cell.Value = d;
                else if (value is decimal?) cell.Value = (decimal)(value as decimal?)!;
                else if (value is int i) cell.Value = i;
                else if (value is long l) cell.Value = l;
                else if (value is double dbl) cell.Value = dbl;
                else if (value is bool b) cell.Value = b ? "Có" : "Không";
                else cell.Value = value?.ToString() ?? "";
                col++;
            }

            // Tô màu xen kẽ
            if (stt % 2 == 0)
            {
                sheet.Range(row, 1, row, columnMappings.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
            }
            stt++;
            row++;
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Đặt border cho toàn bộ bảng
        var dataRange = sheet.Range(headerRow, 1, Math.Max(row - 1, headerRow), columnMappings.Count);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Freeze header
        sheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf<T>(
        IEnumerable<T> data,
        string title,
        Dictionary<string, Func<T, object?>> columnMappings)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily(Fonts.Calibri));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(title).FontSize(18).Bold();
                    col.Item().AlignRight().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).Italic();
                    col.Item().PaddingTop(5);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35); // STT
                        foreach (var _ in columnMappings.Keys)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().AlignCenter().Element(CellHeader).Text("#");
                        foreach (var key in columnMappings.Keys)
                        {
                            header.Cell().AlignCenter().Element(CellHeader).Text(key);
                        }
                    });

                    // Data
                    int stt = 1;
                    foreach (var item in data)
                    {
                        table.Cell().AlignCenter().Element(CellData).Text(stt.ToString());
                        foreach (var mapping in columnMappings.Values)
                        {
                            var value = mapping(item);
                            table.Cell().Element(CellData).Text(FormatValue(value));
                        }
                        stt++;
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Trang ").FontSize(9);
                    t.CurrentPageNumber().FontSize(9);
                    t.Span(" / ").FontSize(9);
                    t.TotalPages().FontSize(9);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer CellHeader(IContainer container) =>
        container.Background("#2563EB").PaddingVertical(6).PaddingHorizontal(4)
            .DefaultTextStyle(t => t.FontColor("#FFFFFF").Bold().FontSize(9));

    private static IContainer CellData(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor("#E5E7EB").PaddingVertical(4).PaddingHorizontal(4);

    private static string FormatValue(object? value)
    {
        if (value == null) return "";
        return value switch
        {
            DateTime dt => dt.ToString("dd/MM/yyyy"),
            decimal d => d.ToString("N0"),
            double dbl => dbl.ToString("N0"),
            int i => i.ToString("N0"),
            long l => l.ToString("N0"),
            bool b => b ? "Có" : "Không",
            _ => value.ToString() ?? ""
        };
    }

    private static string GetColumnLetter(int columnNumber)
    {
        string columnLetter = "";
        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnLetter = Convert.ToChar('A' + modulo) + columnLetter;
            columnNumber = (columnNumber - modulo) / 26;
        }
        return columnLetter;
    }
}
