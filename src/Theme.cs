using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace PromptPalette
{
    internal static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(245, 243, 249);
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);
        public static readonly Color SurfaceDark = Color.FromArgb(249, 247, 253);
        public static readonly Color SurfaceDeep = Color.FromArgb(252, 251, 255);
        public static readonly Color TagArea = Color.FromArgb(255, 255, 255);
        public static readonly Color Card = Color.FromArgb(248, 247, 252);
        public static readonly Color CardHover = Color.FromArgb(240, 236, 249);
        public static readonly Color Border = Color.FromArgb(215, 209, 228);
        public static readonly Color BorderSoft = Color.FromArgb(231, 226, 241);
        public static readonly Color BorderHover = Color.FromArgb(192, 183, 213);
        public static readonly Color Text = Color.FromArgb(29, 26, 45);
        public static readonly Color TextSecondary = Color.FromArgb(73, 68, 92);
        public static readonly Color TextMuted = Color.FromArgb(108, 102, 126);
        public static readonly Color TextFaint = Color.FromArgb(145, 138, 161);
        public static readonly Color DefaultAccent = Color.FromArgb(109, 79, 224);
        public static Color Accent { get; private set; }
        public static Color AccentLight { get; private set; }
        public static Color AccentSoft { get; private set; }
        public static Color AccentBorder { get; private set; }
        public static Color AccentTop { get; private set; }
        public static Color AccentBottom { get; private set; }
        public static Color AccentThumb { get; private set; }
        public static Color CardSelected { get; private set; }
        public static readonly Color Cyan = Color.FromArgb(67, 149, 211);
        public static readonly Color Green = Color.FromArgb(43, 151, 105);
        public static readonly Color Red = Color.FromArgb(210, 70, 79);

        static Theme()
        {
            SetAccent(DefaultAccent);
        }

        public static void SetAccent(Color color)
        {
            Accent = color;
            AccentLight = Mix(color, Color.Black, 0.16);
            AccentSoft = Mix(color, Color.White, 0.90);
            AccentBorder = Mix(color, Color.White, 0.58);
            CardSelected = Mix(color, Color.White, 0.90);
            AccentTop = Mix(color, Color.White, 0.14);
            AccentBottom = Mix(color, Color.Black, 0.10);
            AccentThumb = Mix(color, Color.White, 0.40);
        }

        public static Color Mix(Color a, Color b, double amountOfB)
        {
            double t = Math.Max(0d, Math.Min(1d, amountOfB));
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static void Prepare(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        public static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0f)
            {
                path.AddRectangle(rect);
                return path;
            }
            float diameter = radius * 2f;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180f, 90f);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270f, 90f);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, RectangleF rect, float radius, Color color)
        {
            using (GraphicsPath path = RoundedRect(rect, radius))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRounded(Graphics g, RectangleF rect, float radius, Color color, float width)
        {
            using (GraphicsPath path = RoundedRect(rect, radius))
            using (Pen pen = new Pen(color, width))
            {
                g.DrawPath(pen, path);
            }
        }

        public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            TextRenderer.DrawText(g, text, font, bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        public static void DrawTextRight(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            TextRenderer.DrawText(g, text, font, bounds, color,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        public static void DrawTextCenter(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            TextRenderer.DrawText(g, text, font, bounds, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        public static int Measure(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }
    }

    internal static class Fonts
    {
        private const string CN = "Microsoft YaHei UI";
        private const string EN = "Segoe UI";

        public static readonly Font Brand = new Font(EN, 10.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font BrandSub = new Font(CN, 8.7f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Hero = new Font(CN, 21f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Subtitle = new Font(CN, 9.4f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Section = new Font(CN, 12.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Body = new Font(CN, 9.4f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font BodyBold = new Font(CN, 9.4f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Small = new Font(CN, 8.6f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font SmallBold = new Font(CN, 8.6f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Tiny = new Font(CN, 7.2f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font CardTitle = new Font(CN, 9.3f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font CardTitleSmall = new Font(CN, 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font CardMeta = new Font(EN, 7.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Prompt = new Font(CN, 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Button = new Font(CN, 9.4f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Tab = new Font(CN, 9.3f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font TabSelected = new Font(CN, 9.3f, FontStyle.Bold, GraphicsUnit.Point);
    }
}
