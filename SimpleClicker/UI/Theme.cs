using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SimpleClicker.UI
{
    internal static class Theme
    {
        public static readonly Color Card = Color.White;
        public static readonly Color CardBorder = Color.FromArgb(231, 233, 239);

        public static readonly Color TextPrimary = Color.FromArgb(32, 36, 43);
        public static readonly Color TextSecondary = Color.FromArgb(131, 139, 151);
        public static readonly Color TextMuted = Color.FromArgb(154, 161, 172);
        public static readonly Color SectionText = Color.FromArgb(111, 120, 133);

        public static readonly Color Accent = Color.FromArgb(251, 114, 153);
        public static readonly Color AccentHover = Color.FromArgb(255, 134, 168);
        public static readonly Color AccentDeep = Color.FromArgb(231, 95, 135);
        public static readonly Color AccentSoft = Color.FromArgb(255, 240, 244);
        public static readonly Color AccentBorder = Color.FromArgb(246, 201, 214);

        public static readonly Color FieldBackground = Color.FromArgb(248, 249, 251);
        public static readonly Color FieldHover = Color.FromArgb(241, 244, 248);
        public static readonly Color FieldPressed = Color.FromArgb(233, 237, 243);
        public static readonly Color FieldBorder = Color.FromArgb(232, 235, 240);

        public static readonly Color HeroFill = Color.FromArgb(248, 249, 252);
        public static readonly Color HeroBorder = Color.FromArgb(237, 240, 244);

        public static readonly Color StatusIdle = Color.FromArgb(194, 200, 210);
        public static readonly Color StatusRunning = Color.FromArgb(53, 179, 126);

        public static readonly Color HeaderIcon = Color.FromArgb(138, 145, 156);
        public static readonly Color HeaderHover = Color.FromArgb(242, 244, 247);
        public static readonly Color HeaderPressed = Color.FromArgb(233, 237, 243);
        public static readonly Color Divider = Color.FromArgb(237, 239, 243);

        public const string UiFontFamily = "Microsoft YaHei UI";
        public const string DisplayFontFamily = "Segoe UI";

        public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            var diameter = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0.5f)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void Prepare(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        public static void DrawCentered(
            Graphics graphics,
            string text,
            Font font,
            Brush brush,
            RectangleF bounds,
            bool ellipsis = false)
        {
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = ellipsis ? StringTrimming.EllipsisCharacter : StringTrimming.None
            })
            {
                graphics.DrawString(text, font, brush, bounds, format);
            }
        }

        public static void DrawLeft(
            Graphics graphics,
            string text,
            Font font,
            Brush brush,
            RectangleF bounds)
        {
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter
            })
            {
                graphics.DrawString(text, font, brush, bounds, format);
            }
        }
    }
}
