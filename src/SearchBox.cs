using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class SearchBox : RoundedPanel
    {
        private readonly TextBox _input;
        private readonly Label _placeholder;
        private bool _focused;

        public SearchBox()
        {
            Radius = 10;
            FillColor = Theme.SurfaceDark;
            BorderColor = Theme.Border;
            BackColor = Theme.Bg;
            Size = new Size(232, 38);

            _input = new TextBox();
            _input.BorderStyle = BorderStyle.None;
            _input.BackColor = FillColor;
            _input.ForeColor = Theme.Text;
            _input.Font = Fonts.Body;
            _input.ShortcutsEnabled = true;

            _placeholder = new Label();
            _placeholder.AutoSize = false;
            _placeholder.BackColor = FillColor;
            _placeholder.ForeColor = Theme.TextMuted;
            _placeholder.Font = Fonts.Body;
            _placeholder.Text = "搜索当前分类";
            _placeholder.Cursor = Cursors.IBeam;
            _placeholder.Click += delegate { _input.Focus(); };

            Controls.Add(_input);
            Controls.Add(_placeholder);
            _placeholder.BringToFront();

            _input.TextChanged += delegate
            {
                UpdatePlaceholder();
                Invalidate();
                if (SearchChanged != null) SearchChanged(this, EventArgs.Empty);
            };
            _input.GotFocus += delegate
            {
                _focused = true;
                BorderColor = Theme.AccentBorder;
                UpdatePlaceholder();
                Invalidate();
            };
            _input.LostFocus += delegate
            {
                _focused = false;
                BorderColor = Theme.BorderSoft;
                UpdatePlaceholder();
                Invalidate();
            };
            _input.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (_input.TextLength > 0)
                    {
                        _input.Clear();
                    }
                    else
                    {
                        _input.Parent.Focus();
                    }
                    e.SuppressKeyPress = true;
                }
            };

            LayoutChildren();
        }

        public event EventHandler SearchChanged;

        public string Query
        {
            get { return _input.Text.Trim(); }
        }

        public void FocusInput()
        {
            _input.Focus();
            _input.SelectAll();
        }

        public void SetPlaceholder(string text)
        {
            _placeholder.Text = text ?? string.Empty;
            UpdatePlaceholder();
        }

        public void RefreshTheme()
        {
            BorderColor = _focused ? Theme.AccentBorder : Theme.Border;
            Invalidate(true);
        }

        public void Clear()
        {
            _input.Clear();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_input != null) LayoutChildren();
        }

        private void LayoutChildren()
        {
            int inputHeight = _input.Font.Height + 2;
            int textWidth = Width - 72;
            if (textWidth < 40) textWidth = 40;
            _input.SetBounds(36, (Height - inputHeight) / 2 + 1, textWidth, inputHeight);
            _placeholder.SetBounds(36, (Height - _placeholder.Font.Height) / 2, textWidth,
                _placeholder.Font.Height);
        }

        private void UpdatePlaceholder()
        {
            bool show = _input.TextLength == 0 && !_focused;
            _placeholder.Visible = show;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (Pen pen = new Pen(_focused ? Theme.AccentLight : Theme.TextMuted, 1.25f))
            {
                g.DrawEllipse(pen, 13, 13, 10, 10);
                g.DrawLine(pen, 22, 22, 27, 27);
            }
            if (_input.TextLength > 0)
            {
                using (Pen pen = new Pen(Theme.TextMuted, 1.3f))
                {
                    g.DrawLine(pen, Width - 23, Height / 2 - 5, Width - 13, Height / 2 + 5);
                    g.DrawLine(pen, Width - 13, Height / 2 - 5, Width - 23, Height / 2 + 5);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.X > Width - 34 && _input.TextLength > 0)
            {
                Clear();
                _input.Focus();
                return;
            }
            _input.Focus();
        }
    }

    internal class EmptyState : Control
    {
        private string _title = "没有找到相关提示词";
        private string _subtitle = "试试其他关键词，或切换一个分类";

        public EmptyState()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.TagArea;
            Visible = false;
        }

        public void SetMessage(string title, string subtitle)
        {
            _title = title;
            _subtitle = subtitle;
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

            int centerX = Width / 2;
            int top = Math.Max(24, Height / 2 - 58);
            using (Pen pen = new Pen(Theme.TextFaint, 2f))
            {
                g.DrawEllipse(pen, centerX - 18, top, 25, 25);
                g.DrawLine(pen, centerX + 5, top + 24, centerX + 15, top + 34);
            }

            TextRenderer.DrawText(g, _title, Fonts.Section,
                new Rectangle(0, top + 46, Width, 28), Theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, _subtitle, Fonts.Small,
                new Rectangle(0, top + 74, Width, 22), Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
