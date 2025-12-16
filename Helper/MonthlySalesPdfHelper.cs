using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AgroManagement.Helper
{
    public static class MonthlySalesPdfHelper
    {
        public static byte[] BuildMonthlySalesReportPdf(string root, int year, int month)
        {
            var sales = SalesHelper.GetAllSales(root)
                .Where(s => s.OrderDateUtc.Year == year && s.OrderDateUtc.Month == month)
                .OrderByDescending(s => s.OrderDateUtc)
                .ToList();

            var totalOrders = sales.Count;
            var totalRevenue = sales.Sum(s => s.TotalAmount);

            var doc = new PdfDocument();
            doc.Info.Title = $"Monthly Sales Report {year}-{month:00}";

            PdfPage page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            using var ms = new MemoryStream();

            var gfx = XGraphics.FromPdfPage(page);
            var tf = new XTextFormatter(gfx);

            // Theme (match invoice)
            var greenDark = XColor.FromArgb(0x0F, 0x51, 0x32);
            var green = XColor.FromArgb(0x19, 0x87, 0x54);
            var mint = XColor.FromArgb(0xEC, 0xFD, 0xF5);
            var border = XColor.FromArgb(0xD6, 0xF5, 0xE7);
            var text = XColor.FromArgb(0x06, 0x2E, 0x1A);

            var brGreenDark = new XSolidBrush(greenDark);
            var brGreen = new XSolidBrush(green);
            var brMint = new XSolidBrush(mint);
            var brText = new XSolidBrush(text);
            var penBorder = new XPen(border, 1);

            var fTitle = new XFont("Arial", 18, XFontStyle.Bold);
            var fH = new XFont("Arial", 10, XFontStyle.Bold);
            var fB = new XFont("Arial", 9, XFontStyle.Regular);
            var fBStrong = new XFont("Arial", 9, XFontStyle.Bold);
            var fSmall = new XFont("Arial", 8, XFontStyle.Regular);

            double margin = 36;
            double W = page.Width;
            double H = page.Height;

            static string Money(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
            static string Short(string? s, int max)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = s.Trim();
                if (s.Length <= max) return s;
                return s.Substring(0, max - 3) + "...";
            }

            void DrawRoundedRect(XGraphics g, XBrush fill, XPen pen, XRect r, double radius)
            {
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

            void NewPage(ref PdfPage p, ref XGraphics g, ref XTextFormatter t, string headerTitle)
            {
                p = doc.AddPage();
                p.Size = PdfSharpCore.PageSize.A4;
                g = XGraphics.FromPdfPage(p);
                t = new XTextFormatter(g);

                W = p.Width;
                H = p.Height;

                var head = new XRect(margin, margin, W - margin * 2, 52);
                DrawRoundedRect(g, brGreenDark, new XPen(greenDark, 1), head, 14);

                g.DrawString(headerTitle, fH, XBrushes.White,
                    new XRect(head.X + 16, head.Y + 16, head.Width, 20),
                    XStringFormats.TopLeft);

                g.DrawString(DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt"), fSmall, XBrushes.White,
                    new XRect(head.X, head.Y + 18, head.Width - 16, 20),
                    XStringFormats.TopRight);
            }

            // ===== Main Header =====
            var headerRect = new XRect(margin, margin, W - margin * 2, 95);
            DrawRoundedRect(gfx, brMint, penBorder, headerRect, 14);

            var accentRect = new XRect(headerRect.X, headerRect.Y, headerRect.Width, 34);
            DrawRoundedRect(gfx, brGreenDark, new XPen(greenDark, 1), accentRect, 14);

            gfx.DrawString("SALES REPORT", fH, XBrushes.White,
                new XRect(headerRect.X + 16, headerRect.Y + 8, headerRect.Width, 20),
                XStringFormats.TopLeft);

            gfx.DrawString($"{year}-{month:00}", fH, XBrushes.White,
                new XRect(headerRect.X, headerRect.Y + 8, headerRect.Width - 16, 20),
                XStringFormats.TopRight);

            gfx.DrawString("Monthly Sales Summary", fTitle, brText,
                new XRect(headerRect.X + 16, headerRect.Y + 44, headerRect.Width, 24),
                XStringFormats.TopLeft);

            gfx.DrawString($"Total Orders: {totalOrders}   •   Total Revenue: ৳ {Money(totalRevenue)}", fB, brText,
                new XRect(headerRect.X + 16, headerRect.Y + 70, headerRect.Width, 20),
                XStringFormats.TopLeft);

            double y = headerRect.Bottom + 12;

            // ===== Table container =====
            var tableRect = new XRect(margin, y, W - margin * 2, H - y - margin);
            DrawRoundedRect(gfx, XBrushes.White, penBorder, tableRect, 14);

            var th = new XRect(tableRect.X, tableRect.Y, tableRect.Width, 30);
            DrawRoundedRect(gfx, brMint, penBorder, th, 14);

            // Columns
            double colInv = tableRect.X + 12;
            double colUser = tableRect.X + tableRect.Width * 0.22;
            double colDate = tableRect.X + tableRect.Width * 0.36;
            double colProd = tableRect.X + tableRect.Width * 0.52;
            double colQty = tableRect.X + tableRect.Width * 0.78;
            double colLine = tableRect.X + tableRect.Width * 0.88;

            gfx.DrawString("Invoice", fH, new XSolidBrush(greenDark), new XPoint(colInv, th.Y + 20));
            gfx.DrawString("User", fH, new XSolidBrush(greenDark), new XPoint(colUser, th.Y + 20));
            gfx.DrawString("Date", fH, new XSolidBrush(greenDark), new XPoint(colDate, th.Y + 20));
            gfx.DrawString("Item", fH, new XSolidBrush(greenDark), new XPoint(colProd, th.Y + 20));
            gfx.DrawString("Qty", fH, new XSolidBrush(greenDark), new XPoint(colQty, th.Y + 20));
            gfx.DrawString("Line", fH, new XSolidBrush(greenDark), new XPoint(colLine, th.Y + 20));

            double rowY = th.Bottom + 8;
            double rowH = 22;
            int rowIndex = 0;

            foreach (var sale in sales)
            {
                // each sale can have multiple lines (items)
                foreach (var item in sale.Items)
                {
                    // page break
                    if (rowY + rowH > H - margin)
                    {
                        NewPage(ref page, ref gfx, ref tf, $"Sales Report {year}-{month:00}");

                        // rebuild table header on new page
                        y = margin + 64;
                        tableRect = new XRect(margin, y, W - margin * 2, H - y - margin);
                        DrawRoundedRect(gfx, XBrushes.White, penBorder, tableRect, 14);

                        th = new XRect(tableRect.X, tableRect.Y, tableRect.Width, 30);
                        DrawRoundedRect(gfx, brMint, penBorder, th, 14);

                        colInv = tableRect.X + 12;
                        colUser = tableRect.X + tableRect.Width * 0.22;
                        colDate = tableRect.X + tableRect.Width * 0.36;
                        colProd = tableRect.X + tableRect.Width * 0.52;
                        colQty = tableRect.X + tableRect.Width * 0.78;
                        colLine = tableRect.X + tableRect.Width * 0.88;

                        gfx.DrawString("Invoice", fH, new XSolidBrush(greenDark), new XPoint(colInv, th.Y + 20));
                        gfx.DrawString("User", fH, new XSolidBrush(greenDark), new XPoint(colUser, th.Y + 20));
                        gfx.DrawString("Date", fH, new XSolidBrush(greenDark), new XPoint(colDate, th.Y + 20));
                        gfx.DrawString("Item", fH, new XSolidBrush(greenDark), new XPoint(colProd, th.Y + 20));
                        gfx.DrawString("Qty", fH, new XSolidBrush(greenDark), new XPoint(colQty, th.Y + 20));
                        gfx.DrawString("Line", fH, new XSolidBrush(greenDark), new XPoint(colLine, th.Y + 20));

                        rowY = th.Bottom + 8;
                        rowIndex = 0;
                    }

                    // stripe
                    if (rowIndex % 2 == 0)
                    {
                        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(0xF4, 0xFF, 0xFA)),
                            new XRect(tableRect.X + 8, rowY - 3, tableRect.Width - 16, rowH));
                    }

                    var lineTotal = item.Price * item.Quantity;

                    // draw cells
                    var invText = Short(sale.OrderId, 14);     // show like "7ecc45332cdc..."
                    var userText = Short(sale.Username, 12);   // show like "VeryLongUs..."

                    gfx.DrawString(invText, fBStrong, brText,
                        new XRect(colInv, rowY, colUser - colInv - 6, rowH),
                        XStringFormats.TopLeft);

                    gfx.DrawString(userText, fB, brText,
                        new XRect(colUser, rowY, colDate - colUser - 6, rowH),
                        XStringFormats.TopLeft);

                    gfx.DrawString(sale.OrderDateUtc.ToLocalTime().ToString("dd MMM yy"), fB, brText, new XRect(colDate, rowY, colProd - colDate - 6, rowH), XStringFormats.TopLeft);

                    // item column with wrap
                    tf.Alignment = XParagraphAlignment.Left;
                    tf.DrawString($"{item.Name} ({item.UnitLabel})", fB, brText,
                        new XRect(colProd, rowY - 2, colQty - colProd - 8, rowH + 6));

                    gfx.DrawString(item.Quantity.ToString(), fBStrong, brText, new XRect(colQty, rowY, colLine - colQty - 6, rowH), XStringFormats.TopLeft);
                    gfx.DrawString($"৳ {Money(lineTotal)}", fBStrong, brText, new XRect(colLine, rowY, tableRect.Right - colLine - 12, rowH), XStringFormats.TopLeft);

                    rowY += rowH;
                    rowIndex++;
                }

                // small separator line after each order
                gfx.DrawLine(new XPen(border, 1), tableRect.X + 10, rowY, tableRect.Right - 10, rowY);
                rowY += 6;
            }

            // footer
            gfx.DrawString("Generated from sales.json • Includes user-wise order lines", fSmall,
                new XSolidBrush(XColor.FromArgb(0x13, 0x4E, 0x35)),
                new XRect(margin, H - margin - 14, W - margin * 2, 14),
                XStringFormats.TopCenter);

            doc.Save(ms);
            return ms.ToArray();
        }
    }
}
