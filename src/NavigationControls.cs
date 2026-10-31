using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class HeroPanel : Control
    {
        public HeroPanel()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 68;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            const string title = "把灵感，拼成一句话";
            TextRenderer.DrawText(g, title, Fonts.Hero, new Rectangle(0, 0, Width, 39), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            int titleWidth = Theme.Measure(title, Fonts.Hero);
            using (SolidBrush dot = new SolidBrush(Theme.Accent))
            {
                g.FillEllipse(dot, titleWidth + 9, 18, 6, 6);
            }

            TextRenderer.DrawText(g, "从下方分类里点选提示词，它们会按顺序加入上方的输入框。",
                Fonts.Subtitle, new Rectangle(1, 39, Width - 250, 24), Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
        }
    }

    internal class FooterBar : Control
    {
        private string _status;
        private Color _statusColor = Theme.Green;

        public FooterBar()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 24;
        }

        public void ShowStatus(string text, Color color)
        {
            _status = text;
            _statusColor = color;
            Invalidate();
        }

        public void ClearStatus()
        {
            _status = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            using (SolidBrush dot = new SolidBrush(_status == null ? Theme.Green : _statusColor))
            {
                g.FillEllipse(dot, 2, 10, 5, 5);
            }

            string left = _status == null ? "本地运行 · 点击标签即可组合提示词" : _status;
            TextRenderer.DrawText(g, left, Fonts.Small, new Rectangle(15, 0, Width - 360, Height),
                _status == null ? Theme.TextMuted : _statusColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            TextRenderer.DrawText(g, "Ctrl+E 编辑   ·   Ctrl+Enter 复制   ·   Ctrl+F 搜索",
                Fonts.Small, new Rectangle(Width - 355, 0, 355, Height), Theme.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    internal class LanguageSwitch : Control
    {
        private int _selected;
        private int _hover = -1;

        public LanguageSwitch()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Size = new Size(94, 26);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleName = "插入语言";
        }

        public event EventHandler LanguageChanged;

        public PromptLanguage Language
        {
            get { return _selected == 0 ? PromptLanguage.Chinese : PromptLanguage.English; }
            set
            {
                int index = value == PromptLanguage.English ? 1 : 0;
                if (_selected == index) return;
                _selected = index;
                Invalidate();
                if (LanguageChanged != null) LanguageChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            RectangleF outer = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Theme.FillRounded(g, outer, 8f, Theme.SurfaceDeep);
            Theme.DrawRounded(g, outer, 8f, Theme.BorderSoft, 1f);

            int half = Width / 2;
            if (_selected == 0)
                Theme.FillRounded(g, new RectangleF(3f, 3f, half - 4f, Height - 6f), 6f, Theme.AccentSoft);
            else
                Theme.FillRounded(g, new RectangleF(half + 1f, 3f, half - 4f, Height - 6f), 6f, Theme.AccentSoft);

            if (_hover >= 0 && _hover != _selected)
            {
                RectangleF hoverRect = _hover == 0
                    ? new RectangleF(3f, 3f, half - 4f, Height - 6f)
                    : new RectangleF(half + 1f, 3f, half - 4f, Height - 6f);
                Theme.FillRounded(g, hoverRect, 6f, Color.FromArgb(242, 238, 250));
            }

            TextRenderer.DrawText(g, "中文", Fonts.Small,
                new Rectangle(0, 0, half, Height),
                _selected == 0 ? Theme.AccentLight : Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "EN", Fonts.Small,
                new Rectangle(half, 0, half, Height),
                _selected == 1 ? Theme.AccentLight : Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int newHover = e.X < Width / 2 ? 0 : 1;
            if (_hover != newHover)
            {
                _hover = newHover;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            Language = e.X < Width / 2 ? PromptLanguage.Chinese : PromptLanguage.English;
        }
    }

    internal class CategoryBar : Control
    {
        private readonly List<string> _items = new List<string>();
        private readonly List<Rectangle> _rects = new List<Rectangle>();
        private int _selected;
        private int _hover = -1;
        private int _offset;
        private int _maxOffset;
        private int _tabsWidth;
        private bool _showAdd;
        private bool _compact;
        private bool _hoverAdd;
        private bool _hideSelection;
        private Rectangle _addRect;

        public CategoryBar()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 38;
            Cursor = Cursors.Default;
            TabStop = true;
            AccessibleName = "提示词分类";
        }

        public event EventHandler SelectedIndexChanged;
        public event EventHandler AddRequested;

        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                if (value < 0 || value >= _items.Count || value == _selected) return;
                _selected = value;
                EnsureSelectedVisible();
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public bool ShowAdd
        {
            get { return _showAdd; }
            set
            {
                if (_showAdd == value) return;
                _showAdd = value;
                Recalc();
                EnsureSelectedVisible();
                Invalidate();
            }
        }

        public bool Compact
        {
            get { return _compact; }
            set
            {
                if (_compact == value) return;
                _compact = value;
                Recalc();
                EnsureSelectedVisible();
                Invalidate();
            }
        }

        public bool HideSelection
        {
            get { return _hideSelection; }
            set
            {
                if (_hideSelection == value) return;
                _hideSelection = value;
                Invalidate();
            }
        }

        public void SetItems(IEnumerable<string> items)
        {
            SetItems(items, 0);
        }

        public void SetItems(IEnumerable<string> items, int selectedIndex)
        {
            _items.Clear();
            _items.AddRange(items);
            _selected = 0;
            if (selectedIndex > 0 && selectedIndex < _items.Count) _selected = selectedIndex;
            _offset = 0;
            Recalc();
            EnsureSelectedVisible();
            Invalidate();
        }

        private void Recalc()
        {
            _rects.Clear();
            _tabsWidth = Math.Max(0, Width - (_showAdd ? 42 : 0));
            Font font = _compact ? Fonts.Small : Fonts.Tab;
            int x = 0;
            int y = _compact ? 1 : 2;
            int itemHeight = _compact ? 28 : 34;
            int gap = _compact ? 2 : 6;
            for (int i = 0; i < _items.Count; i++)
            {
                int width = Theme.Measure(_items[i], font) + (_compact ? 12 : 30);
                int minWidth = _compact ? 40 : 56;
                if (width < minWidth) width = minWidth;
                _rects.Add(new Rectangle(x, y, width, itemHeight));
                x += width + gap;
            }
            _maxOffset = Math.Max(0, x - _tabsWidth);
            if (_offset > _maxOffset) _offset = _maxOffset;
            _addRect = _showAdd
                ? new Rectangle(Width - 34, (Height - 30) / 2, 34, 30)
                : Rectangle.Empty;
        }

        private void EnsureSelectedVisible()
        {
            if (_selected < 0 || _selected >= _rects.Count) return;
            Rectangle rect = _rects[_selected];
            if (rect.Left - _offset < 0) _offset = rect.Left;
            else if (rect.Right - _offset > _tabsWidth) _offset = rect.Right - _tabsWidth;
            if (_offset < 0) _offset = 0;
            if (_offset > _maxOffset) _offset = _maxOffset;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recalc();
            EnsureSelectedVisible();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            Region saved = g.Clip;
            g.SetClip(new Rectangle(0, 0, Math.Max(0, _tabsWidth), Height));
            for (int i = 0; i < _items.Count && i < _rects.Count; i++)
            {
                Rectangle rect = _rects[i];
                rect.Offset(-_offset, 0);
                if (rect.Right < 0 || rect.Left > _tabsWidth) continue;

                bool selected = !_hideSelection && i == _selected;
                if (selected)
                {
                    RectangleF rf = new RectangleF(rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1f, rect.Height - 1f);
                    Theme.FillRounded(g, rf, _compact ? 8f : 9f, Theme.AccentSoft);
                    Theme.DrawRounded(g, rf, _compact ? 8f : 9f, Theme.AccentBorder, 1f);
                }
                else if (i == _hover)
                {
                    Theme.FillRounded(g, new RectangleF(rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1f, rect.Height - 1f),
                        _compact ? 8f : 9f, Color.FromArgb(242, 238, 250));
                }

                Color color = selected ? Theme.AccentLight : (i == _hover ? Theme.Text : Theme.TextSecondary);
                Font font = _compact
                    ? (selected ? Fonts.SmallBold : Fonts.Small)
                    : (selected ? Fonts.TabSelected : Fonts.Tab);
                TextRenderer.DrawText(g, _items[i], font, rect, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                    | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
            g.Clip = saved;

            if (_showAdd)
            {
                Rectangle add = _addRect;
                Theme.FillRounded(g, new RectangleF(add.X + 0.5f, add.Y + 0.5f, add.Width - 1f, add.Height - 1f),
                    8f, _hoverAdd ? Color.FromArgb(238, 233, 249) : Color.FromArgb(248, 246, 252));
                Theme.DrawRounded(g, new RectangleF(add.X + 0.5f, add.Y + 0.5f, add.Width - 1f, add.Height - 1f),
                    8f, _hoverAdd ? Theme.AccentBorder : Theme.Border, 1f);
                using (Pen pen = new Pen(_hoverAdd ? Theme.AccentLight : Theme.TextSecondary, 1.35f))
                {
                    int cx = add.X + add.Width / 2;
                    int cy = add.Y + add.Height / 2;
                    g.DrawLine(pen, cx - 5, cy, cx + 5, cy);
                    g.DrawLine(pen, cx, cy - 5, cx, cy + 5);
                }
            }
        }

        private int HitTest(Point point)
        {
            if (_showAdd && _addRect.Contains(point)) return -2;
            Point adjusted = new Point(point.X + _offset, point.Y);
            for (int i = 0; i < _rects.Count; i++)
            {
                if (_rects[i].Contains(adjusted)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hover = HitTest(e.Location);
            bool hoverAdd = hover == -2;
            int itemHover = hover >= 0 ? hover : -1;
            if (hoverAdd != _hoverAdd || itemHover != _hover)
            {
                _hoverAdd = hoverAdd;
                _hover = itemHover;
                Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = -1;
            _hoverAdd = false;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int hit = HitTest(e.Location);
            if (hit == -2)
            {
                Focus();
                if (AddRequested != null) AddRequested(this, EventArgs.Empty);
                return;
            }
            if (hit < 0) return;
            Focus();
            SelectedIndex = hit;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_maxOffset <= 0) return;
            _offset -= e.Delta > 0 ? 64 : -64;
            if (_offset < 0) _offset = 0;
            if (_offset > _maxOffset) _offset = _maxOffset;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left && _selected > 0) SelectedIndex = _selected - 1;
            else if (e.KeyCode == Keys.Right && _selected < _items.Count - 1) SelectedIndex = _selected + 1;
        }
    }
}


