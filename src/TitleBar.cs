using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class TitleBar : Control
    {
        private readonly Form _form;
        private bool _dragging;
        private Point _dragCursor;
        private Point _dragForm;

        public TitleBar(Form form)
        {
            _form = form;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 50;
            Dock = DockStyle.Top;
            AccessibleName = "窗口标题栏";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            RectangleF logoRect = new RectangleF(24f, 12f, 26f, 26f);
            using (GraphicsPath logoPath = Theme.RoundedRect(logoRect, 8f))
            using (LinearGradientBrush logoBrush = new LinearGradientBrush(
                logoRect, Theme.AccentTop, Theme.Cyan, 45f))
            {
                g.FillPath(logoBrush, logoPath);
            }

            PointF[] star = StarPoints(new PointF(37f, 25f), 7.2f, 2.8f, 8);
            using (SolidBrush starBrush = new SolidBrush(Color.White))
            {
                g.FillPolygon(starBrush, star);
            }

            DrawText(g, "PromptPalette", Fonts.Brand, Theme.Text,
                new Rectangle(60, 10, 180, 30), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            int brandWidth = Theme.Measure("PromptPalette", Fonts.Brand);

            using (Pen divider = new Pen(Theme.BorderSoft, 1f))
            {
                g.DrawLine(divider, 60 + brandWidth + 13, 20, 60 + brandWidth + 13, 31);
            }

            DrawText(g, "AI 提示词灵感板", Fonts.BrandSub, Theme.TextMuted,
                new Rectangle(60 + brandWidth + 24, 10, 220, 30),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            using (Pen line = new Pen(Theme.BorderSoft, 1f))
            {
                g.DrawLine(line, 0, Height - 1, Width, Height - 1);
            }
        }

        private static void DrawText(Graphics g, string text, Font font, Color color, Rectangle rect,
            TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, rect, color,
                flags | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private static PointF[] StarPoints(PointF center, float outer, float inner, int points)
        {
            PointF[] result = new PointF[points * 2];
            double start = -Math.PI / 2.0;
            double step = Math.PI / points;
            for (int i = 0; i < points * 2; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                result[i] = new PointF(
                    center.X + (float)Math.Cos(start + step * i) * radius,
                    center.Y + (float)Math.Sin(start + step * i) * radius);
            }
            return result;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _form.Activate();
            if (_form.WindowState == FormWindowState.Maximized) return;
            _dragging = true;
            _dragCursor = Cursor.Position;
            _dragForm = _form.Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            Point now = Cursor.Position;
            _form.Location = new Point(
                _dragForm.X + (now.X - _dragCursor.X),
                _dragForm.Y + (now.Y - _dragCursor.Y));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            Capture = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (_form.WindowState == FormWindowState.Maximized)
                _form.WindowState = FormWindowState.Normal;
            else
                _form.WindowState = FormWindowState.Maximized;
        }
    }
}
