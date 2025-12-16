using AgroManagement.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace AgroManagement.Helper
{
    public static class InvoicePdfHelper
    {
        public static byte[] BuildInvoicePdf(string invoiceNo, string username, DateTime invoiceDateUtc, CartVM vm)
        {
            vm ??= new CartVM();
            vm.Items ??= new System.Collections.Generic.List<CartItem>();

            var doc = new PdfDocument();
            doc.Info.Title = $"Invoice {invoiceNo}";

            var page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            using var ms = new MemoryStream();

            var gfx = XGraphics.FromPdfPage(page);
            var tf = new XTextFormatter(gfx);

            // ===== Theme colors (matches your green UI) =====
            var greenDark = XColor.FromArgb(0x0F, 0x51, 0x32);   // #0f5132
            var green = XColor.FromArgb(0x19, 0x87, 0x54);       // #198754
            var mint = XColor.FromArgb(0xEC, 0xFD, 0xF5);        // #ecfdf5
            var border = XColor.FromArgb(0xD6, 0xF5, 0xE7);
            var text = XColor.FromArgb(0x06, 0x2E, 0x1A);

            var brGreenDark = new XSolidBrush(greenDark);
            var brGreen = new XSolidBrush(green);
            var brMint = new XSolidBrush(mint);
            var brText = new XSolidBrush(text);
            var penBorder = new XPen(border, 1);

            // Fonts
            var fTitle = new XFont("Arial", 20, XFontStyle.Bold);
            var fH = new XFont("Arial", 11, XFontStyle.Bold);
            var fB = new XFont("Arial", 10, XFontStyle.Regular);
            var fBStrong = new XFont("Arial", 10, XFontStyle.Bold);
            var fSmall = new XFont("Arial", 9, XFontStyle.Regular);

            double margin = 36;
            double W = page.Width;
            double H = page.Height;

            // Helpers
            static string Money(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);

            void DrawRoundedRect(XGraphics g, XBrush fill, XPen pen, XRect r, double radius)
            {
                // Rounded rectangle path
                var path = new XGraphicsPath();
                var x = r.X; var y = r.Y; var w = r.Width; var h = r.Height; var rr = radius;

                path.AddArc(x, y, rr * 2, rr * 2, 180, 90);
                path.AddLine(x + rr, y, x + w - rr, y);
                path.AddArc(x + w - rr * 2, y, rr * 2, rr * 2, 270, 90);
                path.AddLine(x + w, y + rr, x + w, y + h - rr);
                path.AddArc(x + w - rr * 2, y + h - rr * 2, rr * 2, rr * 2, 0, 90);
                path.AddLine(x + w - rr, y + h, x + rr, y + h);
                path.AddArc(x, y + h - rr * 2, rr * 2, rr * 2, 90, 90);
                path.AddLine(x, y + h - rr, x, y + rr);
                path.CloseFigure();

                g.DrawPath(pen, fill, path);
            }

            // ===== Header =====
            var headerRect = new XRect(margin, margin, W - margin * 2, 110);
            DrawRoundedRect(gfx, brMint, penBorder, headerRect, 14);

            // Top accent bar
            var accentRect = new XRect(headerRect.X, headerRect.Y, headerRect.Width, 38);
            DrawRoundedRect(gfx, brGreenDark, new XPen(greenDark, 1), accentRect, 14);

            // Logo (optional)
            try
            {
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Spesh.png");
                if (File.Exists(logoPath))
                {
                    using var logo = XImage.FromFile(logoPath);
                    gfx.DrawImage(logo, headerRect.X + 14, headerRect.Y + 8, 28, 28);
                }
            }
            catch { /* ignore logo errors */ }

            gfx.DrawString("INVOICE", fH, XBrushes.White,
                new XRect(headerRect.X + 52, headerRect.Y + 8, headerRect.Width, 24),
                XStringFormats.TopLeft);

            // Right side invoice meta on dark bar
            var meta = $"{invoiceNo}   •   {invoiceDateUtc:dd MMM yyyy}";
            gfx.DrawString(meta, fSmall, XBrushes.White,
                new XRect(headerRect.X, headerRect.Y + 10, headerRect.Width - 14, 20),
                XStringFormats.TopRight);

            // Body part of header
            gfx.DrawString("AgroManagement", fTitle, brText,
                new XRect(headerRect.X + 14, headerRect.Y + 48, headerRect.Width, 30),
                XStringFormats.TopLeft);

            gfx.DrawString("Billed To", fSmall, new XSolidBrush(XColor.FromArgb(0x13, 0x4E, 0x35)),
                new XRect(headerRect.X + 16, headerRect.Y + 82, 120, 16),
                XStringFormats.TopLeft);

            gfx.DrawString(username ?? "Unknown", fBStrong, brText,
                new XRect(headerRect.X + 16, headerRect.Y + 96, headerRect.Width - 32, 16),
                XStringFormats.TopLeft);

            double y = headerRect.Bottom + 14;

            // ===== Items Table container =====
            var tableRect = new XRect(margin, y, W - margin * 2, 360);
            DrawRoundedRect(gfx, XBrushes.White, penBorder, tableRect, 14);

            // Table header row
            var th = new XRect(tableRect.X, tableRect.Y, tableRect.Width, 34);
            DrawRoundedRect(gfx, brMint, penBorder, th, 14);

            double colName = tableRect.X + 14;
            double colUnit = tableRect.X + tableRect.Width * 0.52;
            double colPrice = tableRect.X + tableRect.Width * 0.68;
            double colQty = tableRect.X + tableRect.Width * 0.80;
            double colTotal = tableRect.X + tableRect.Width * 0.90;

            gfx.DrawString("Product", fH, new XSolidBrush(greenDark), new XPoint(colName, th.Y + 22));
            gfx.DrawString("Unit", fH, new XSolidBrush(greenDark), new XPoint(colUnit, th.Y + 22));
            gfx.DrawString("Price", fH, new XSolidBrush(greenDark), new XPoint(colPrice, th.Y + 22));
            gfx.DrawString("Qty", fH, new XSolidBrush(greenDark), new XPoint(colQty, th.Y + 22));
            gfx.DrawString("Total", fH, new XSolidBrush(greenDark), new XPoint(colTotal, th.Y + 22));

            // Rows
            double rowY = th.Bottom + 10;
            double rowH = 28;
            int i = 0;

            foreach (var item in vm.Items)
            {
                if (rowY + rowH > tableRect.Bottom - 12)
                {
                    // New page if overflow
                    page = doc.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    tf = new XTextFormatter(gfx);

                    W = page.Width; H = page.Height;
                    y = margin;

                    // Small top header on new pages
                    var mini = new XRect(margin, y, W - margin * 2, 54);
                    DrawRoundedRect(gfx, brGreenDark, new XPen(greenDark, 1), mini, 14);
                    gfx.DrawString($"Invoice {invoiceNo}", fH, XBrushes.White,
                        new XRect(mini.X + 16, mini.Y + 16, mini.Width, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"{invoiceDateUtc:dd MMM yyyy}", fB, XBrushes.White,
                        new XRect(mini.X, mini.Y + 16, mini.Width - 16, 20), XStringFormats.TopRight);

                    y = mini.Bottom + 12;

                    tableRect = new XRect(margin, y, W - margin * 2, 540);
                    DrawRoundedRect(gfx, XBrushes.White, penBorder, tableRect, 14);

                    th = new XRect(tableRect.X, tableRect.Y, tableRect.Width, 34);
                    DrawRoundedRect(gfx, brMint, penBorder, th, 14);

                    colName = tableRect.X + 14;
                    colUnit = tableRect.X + tableRect.Width * 0.52;
                    colPrice = tableRect.X + tableRect.Width * 0.68;
                    colQty = tableRect.X + tableRect.Width * 0.80;
                    colTotal = tableRect.X + tableRect.Width * 0.90;

                    gfx.DrawString("Product", fH, new XSolidBrush(greenDark), new XPoint(colName, th.Y + 22));
                    gfx.DrawString("Unit", fH, new XSolidBrush(greenDark), new XPoint(colUnit, th.Y + 22));
                    gfx.DrawString("Price", fH, new XSolidBrush(greenDark), new XPoint(colPrice, th.Y + 22));
                    gfx.DrawString("Qty", fH, new XSolidBrush(greenDark), new XPoint(colQty, th.Y + 22));
                    gfx.DrawString("Total", fH, new XSolidBrush(greenDark), new XPoint(colTotal, th.Y + 22));

                    rowY = th.Bottom + 10;
                }

                // Light stripe
                if (i % 2 == 0)
                {
                    var stripe = new XRect(tableRect.X + 10, rowY - 4, tableRect.Width - 20, rowH);
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(0xF4, 0xFF, 0xFA)), stripe);
                }

                var lineTotal = item.Price * item.Quantity;

                // Product name (wrap nicely)
                var nameRect = new XRect(colName, rowY - 2, (colUnit - colName) - 10, rowH);
                tf.Alignment = XParagraphAlignment.Left;
                tf.DrawString(item.Name ?? "", fBStrong, brText, nameRect);

                gfx.DrawString(item.UnitLabel ?? "", fB, new XSolidBrush(XColor.FromArgb(0x13, 0x4E, 0x35)),
                    new XRect(colUnit, rowY - 2, 120, rowH), XStringFormats.TopLeft);

                gfx.DrawString($"৳ {Money(item.Price)}", fB, brText,
                    new XRect(colPrice, rowY - 2, 90, rowH), XStringFormats.TopLeft);

                gfx.DrawString(item.Quantity.ToString(CultureInfo.InvariantCulture), fBStrong, brText,
                    new XRect(colQty, rowY - 2, 40, rowH), XStringFormats.TopLeft);

                gfx.DrawString($"৳ {Money(lineTotal)}", fBStrong, brText,
                    new XRect(colTotal, rowY - 2, tableRect.Right - colTotal - 14, rowH), XStringFormats.TopLeft);

                rowY += rowH;
                i++;
            }

            // ===== Totals card =====
            decimal subtotal = vm.Subtotal;
            decimal delivery = 0;
            decimal grand = subtotal + delivery;

            var totalsRect = new XRect(margin, tableRect.Bottom + 14, W - margin * 2, 120);
            if (totalsRect.Bottom > H - margin) totalsRect = new XRect(margin, H - margin - 120, W - margin * 2, 120);

            DrawRoundedRect(gfx, brMint, penBorder, totalsRect, 14);

            var left = totalsRect.X + 16;
            var right = totalsRect.Right - 16;

            gfx.DrawString("Summary", fH, new XSolidBrush(greenDark),
                new XRect(left, totalsRect.Y + 12, totalsRect.Width, 18), XStringFormats.TopLeft);

            void Row(string label, string value, double ry, bool strong = false)
            {
                gfx.DrawString(label, strong ? fBStrong : fB, brText, new XRect(left, ry, 200, 16), XStringFormats.TopLeft);
                gfx.DrawString(value, strong ? fBStrong : fB, brText, new XRect(right - 200, ry, 200, 16), XStringFormats.TopRight);
            }

            Row("Subtotal", $"৳ {Money(subtotal)}", totalsRect.Y + 40);
            Row("Delivery", $"৳ {Money(delivery)}", totalsRect.Y + 60);
            Row("Grand Total", $"৳ {Money(grand)}", totalsRect.Y + 84, strong: true);

            // ===== Footer note =====
            gfx.DrawString("Thank you for your purchase • Fresh delivery & quality assured",
                fSmall, new XSolidBrush(XColor.FromArgb(0x13, 0x4E, 0x35)),
                new XRect(margin, H - margin - 16, W - margin * 2, 16), XStringFormats.TopCenter);

            doc.Save(ms);
            return ms.ToArray();
        }
    }
}
