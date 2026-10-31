using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class RoundedPanel : Panel
    {
        private int _radius = 12;
        private Color _fillColor = Theme.Surface;
        private Color _borderColor = Theme.Border;
        private int _borderWidth = 1;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            DoubleBuffered = true;
        }

        public int Radius
        {
            get { return _radius; }
            set { _radius = value; Invalidate(); }
        }

        public Color FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public int BorderWidth
        {
            get { return _borderWidth; }
            set { _borderWidth = value; Invalidate(); }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Theme.Prepare(e.Graphics);
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (rect.Width > 0 && rect.Height > 0)
            {
                Theme.FillRounded(e.Graphics, rect, _radius, _fillColor);
                if (_borderWidth > 0 && _borderColor.A > 0)
                {
                    Theme.DrawRounded(e.Graphics, rect, _radius, _borderColor, _borderWidth);
                }
            }
            base.OnPaint(e);
        }
    }

    internal enum ButtonVariant
    {
        Primary,
        Secondary,
        Ghost,
        Danger
    }

    internal class ModernButton : Button
    {
        private ButtonVariant _variant = ButtonVariant.Primary;
        private bool _hover;
        private bool _pressed;
        private bool _showCopyIcon;

        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            Font = Fonts.Button;
            Cursor = Cursors.Hand;
            TabStop = true;
            DoubleBuffered = true;
        }

        public ButtonVariant Variant
        {
            get { return _variant; }
            set { _variant = value; Invalidate(); }
        }

        public bool ShowCopyIcon
        {
            get { return _showCopyIcon; }
            set { _showCopyIcon = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left) _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color textColor;
            if (_variant == ButtonVariant.Primary)
            {
                Color top = Enabled ? Theme.AccentTop : Color.FromArgb(228, 224, 238);
                Color bottom = Enabled ? Theme.AccentBottom : Color.FromArgb(216, 211, 228);
                if (_hover && Enabled)
                {
                    top = Theme.Mix(Theme.AccentTop, Color.White, 0.10);
                    bottom = Theme.Mix(Theme.AccentBottom, Color.White, 0.05);
                }
                if (_pressed && Enabled)
                {
                    top = Theme.Mix(Theme.Accent, Color.Black, 0.06);
                    bottom = Theme.Mix(Theme.AccentBottom, Color.Black, 0.06);
                }
                using (GraphicsPath path = Theme.RoundedRect(rect, 10f))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(0, 0), new PointF(Width, Height), top, bottom))
                {
                    blendColors(brush);
                    g.FillPath(brush, path);
                }
                textColor = Enabled
                    ? (top.GetBrightness() > 0.62 ? Theme.Text : Color.White)
                    : Theme.TextMuted;
            }
            else if (_variant == ButtonVariant.Secondary)
            {
                Color fill = _hover ? Color.FromArgb(243, 239, 251) : Color.FromArgb(250, 248, 254);
                if (_pressed) fill = Color.FromArgb(236, 231, 247);
                if (!Enabled) fill = Color.FromArgb(246, 244, 249);
                Theme.FillRounded(g, rect, 9f, fill);
                Theme.DrawRounded(g, rect, 9f, _hover && Enabled ? Theme.BorderHover : Theme.Border, 1f);
                textColor = Enabled ? Theme.TextSecondary : Theme.TextMuted;
            }
            else if (_variant == ButtonVariant.Danger)
            {
                Color fill = _hover ? Color.FromArgb(201, 64, 73) : Color.FromArgb(185, 55, 64);
                if (_pressed) fill = Color.FromArgb(169, 46, 55);
                if (!Enabled) fill = Color.FromArgb(228, 209, 212);
                Theme.FillRounded(g, rect, 9f, fill);
                Theme.DrawRounded(g, rect, 9f,
                    _hover && Enabled ? Color.FromArgb(176, 48, 57) : Color.FromArgb(196, 76, 84), 1f);
                textColor = Enabled ? Color.White : Theme.TextMuted;
            }
            else if (_variant == ButtonVariant.Ghost)
            {
                if (_hover && Enabled) Theme.FillRounded(g, rect, 9f, Color.FromArgb(243, 239, 250));
                textColor = Enabled ? Theme.TextSecondary : Theme.TextMuted;
            }
            else
            {
                if (_hover && Enabled) Theme.FillRounded(g, rect, 9f, Color.FromArgb(243, 239, 250));
                textColor = Enabled ? Theme.TextSecondary : Theme.TextMuted;
            }

            if (Focused && ShowFocusCues)
            {
                Theme.DrawRounded(g, new RectangleF(2.5f, 2.5f, Width - 5f, Height - 5f), 8f,
                    Theme.AccentBorder, 1f);
            }

            int iconWidth = _showCopyIcon ? 17 : 0;
            Size textSize = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int total = iconWidth + textSize.Width;
            int startX = (Width - total) / 2;
            int centerY = Height / 2;

            if (_showCopyIcon)
            {
                using (Pen pen = new Pen(textColor, 1.3f))
                {
                    int ix = startX;
                    int iy = centerY - 7;
                    g.DrawRectangle(pen, ix + 3, iy, 9, 10);
                    g.DrawRectangle(pen, ix, iy + 3, 9, 10);
                }
            }

            Rectangle textRect = new Rectangle(startX + iconWidth + (_showCopyIcon ? 8 : 0), 0,
                Width, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.SingleLine);
        }

        private static void blendColors(LinearGradientBrush brush)
        {
            ColorBlend blend = new ColorBlend(3);
            blend.Colors = new Color[] { brush.LinearColors[0], brush.LinearColors[1], brush.LinearColors[1] };
            blend.Positions = new float[] { 0f, 0.55f, 1f };
            brush.InterpolationColors = blend;
        }
    }

    internal enum WindowButtonKind
    {
        Minimize,
        Maximize,
        Close
    }

    internal class WindowButton : Control
    {
        private bool _hover;
        private bool _maximized;

        public WindowButton(WindowButtonKind kind)
        {
            Kind = kind;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Size = new Size(46, 50);
            TabStop = false;
            Cursor = Cursors.Hand;
            if (kind == WindowButtonKind.Minimize) AccessibleName = "最小化";
            if (kind == WindowButtonKind.Maximize) AccessibleName = "最大化";
            if (kind == WindowButtonKind.Close) AccessibleName = "关闭";
        }

        public WindowButtonKind Kind { get; private set; }

        public bool IsMaximized
        {
            get { return _maximized; }
            set { _maximized = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            if (_hover)
            {
                Color hoverColor = Kind == WindowButtonKind.Close
                    ? Color.FromArgb(214, 66, 75)
                    : Color.FromArgb(238, 234, 246);
                Theme.FillRounded(g, new RectangleF(5f, 8f, Width - 10f, Height - 15f), 8f, hoverColor);
            }

            Color icon = _hover && Kind == WindowButtonKind.Close ? Color.White : Theme.TextSecondary;
            using (Pen pen = new Pen(icon, 1.25f))
            {
                int cx = Width / 2;
                int cy = Height / 2;
                if (Kind == WindowButtonKind.Minimize)
                {
                    g.DrawLine(pen, cx - 6, cy + 1, cx + 6, cy + 1);
                }
                else if (Kind == WindowButtonKind.Close)
                {
                    g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
                    g.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
                }
                else if (!_maximized)
                {
                    g.DrawRectangle(pen, cx - 6, cy - 6, 11, 11);
                }
                else
                {
                    g.DrawRectangle(pen, cx - 5, cy - 6, 10, 10);
                    g.DrawRectangle(pen, cx - 7, cy - 4, 10, 10);
                }
            }
        }
    }
}
