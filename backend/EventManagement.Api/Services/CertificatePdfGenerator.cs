using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.WPFonts;

namespace EventManagement.Api.Services;

public sealed record CertificatePdfModel(
    string AttendeeName,
    string EventTitle,
    DateTimeOffset EventDate,
    string SignatoryName,
    Guid RegistrationId,
    byte[]? EventLogo);

public interface ICertificatePdfGenerator
{
    byte[] Generate(CertificatePdfModel model);
}

public sealed class CertificatePdfGenerator : ICertificatePdfGenerator
{
    private const string FontFamily = "Certificate Sans";

    static CertificatePdfGenerator()
    {
        GlobalFontSettings.FontResolver = CertificateFontResolver.Instance;
    }

    public byte[] Generate(CertificatePdfModel model)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"Certificate - {model.EventTitle}";
        document.Info.Author = model.SignatoryName;
        var page = document.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        page.Size = PdfSharp.PageSize.A4;

        using var graphics = XGraphics.FromPdfPage(page);
        var width = page.Width.Point;
        var height = page.Height.Point;
        var navy = XColor.FromArgb(22, 43, 67);
        var gold = XColor.FromArgb(199, 152, 52);
        var muted = XColor.FromArgb(88, 99, 110);

        graphics.DrawRectangle(new XSolidBrush(XColors.White), 0, 0, width, height);
        graphics.DrawRectangle(new XPen(navy, 8), 20, 20, width - 40, height - 40);
        graphics.DrawRectangle(new XPen(gold, 2), 31, 31, width - 62, height - 62);

        var contentTop = 58d;
        if (model.EventLogo is { Length: > 0 })
        {
            try
            {
                using var logoStream = new MemoryStream(model.EventLogo);
                using var logo = XImage.FromStream(logoStream);
                const double boxWidth = 100;
                const double boxHeight = 65;
                var scale = Math.Min(boxWidth / logo.PixelWidth, boxHeight / logo.PixelHeight);
                var logoWidth = logo.PixelWidth * scale;
                var logoHeight = logo.PixelHeight * scale;
                graphics.DrawImage(logo, (width - logoWidth) / 2, contentTop, logoWidth, logoHeight);
                contentTop += boxHeight + 14;
            }
            catch
            {
                // Certificate generation must still succeed if a previously uploaded image
                // is corrupt or uses a format PDFsharp cannot decode (for example WebP).
            }
        }

        DrawCentered(graphics, "CERTIFICATE OF ATTENDANCE", 27, XFontStyleEx.Bold,
            new XSolidBrush(navy), contentTop, width, 38);
        contentTop += 58;
        DrawCentered(graphics, "This certificate is presented to", 13, XFontStyleEx.Regular,
            new XSolidBrush(muted), contentTop, width, 24);
        contentTop += 35;
        DrawCentered(graphics, model.AttendeeName, 29, XFontStyleEx.Bold,
            new XSolidBrush(gold), contentTop, width, 42);
        contentTop += 55;
        DrawCentered(graphics, "for confirmed attendance at", 13, XFontStyleEx.Regular,
            new XSolidBrush(muted), contentTop, width, 24);
        contentTop += 30;
        DrawCentered(graphics, model.EventTitle, 22, XFontStyleEx.Bold,
            new XSolidBrush(navy), contentTop, width, 58);
        contentTop += 66;
        DrawCentered(graphics, model.EventDate.ToString("MMMM d, yyyy"), 13,
            XFontStyleEx.Regular, new XSolidBrush(muted), contentTop, width, 24);

        var signatureY = height - 124;
        graphics.DrawLine(new XPen(navy, 1), width / 2 - 115, signatureY, width / 2 + 115, signatureY);
        DrawCentered(graphics, model.SignatoryName, 14, XFontStyleEx.Bold,
            new XSolidBrush(navy), signatureY + 8, width, 22);
        DrawCentered(graphics, "Event Organizer", 10, XFontStyleEx.Regular,
            new XSolidBrush(muted), signatureY + 29, width, 18);

        var idFont = new XFont(FontFamily, 7, XFontStyleEx.Regular);
        graphics.DrawString($"Certificate ID: {model.RegistrationId:N}", idFont,
            new XSolidBrush(muted), new XRect(48, height - 57, width - 96, 14),
            XStringFormats.BottomRight);

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static void DrawCentered(
        XGraphics graphics,
        string text,
        double fontSize,
        XFontStyleEx style,
        XBrush brush,
        double y,
        double pageWidth,
        double height)
    {
        var font = new XFont(FontFamily, fontSize, style);
        graphics.DrawString(text, font, brush, new XRect(55, y, pageWidth - 110, height),
            XStringFormats.TopCenter);
    }

    private sealed class CertificateFontResolver : IFontResolver
    {
        private const string Regular = "certificate-regular";
        private const string Bold = "certificate-bold";
        public static CertificateFontResolver Instance { get; } = new();

        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) =>
            new(bold ? Bold : Regular, mustSimulateBold: false, mustSimulateItalic: italic);

        public byte[] GetFont(string faceName) => faceName switch
        {
            Bold => FontDataHelper.SegoeWPBold,
            _ => FontDataHelper.SegoeWP
        };
    }
}
