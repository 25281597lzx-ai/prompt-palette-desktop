using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal sealed class AccentSwatch : Control
    {
        private bool _hover;

        public AccentSwatch(Color color, string label)
        {
            AccentColor = color;
            LabelText = label;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Size = new Size(64, 82);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public Color AccentColor { get; private set; }
        public string LabelText { get; private set; }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
            }
        }

        private bool _selected;

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

            RectangleF ring = new RectangleF(8f, 2f, 48f, 48f);
            if (_selected || _hover)
            {
                Theme.FillRounded(g, ring, 24f,
                    _selected ? Theme.AccentSoft : Color.FromArgb(245, 242, 251));
            }

            RectangleF circle = new RectangleF(14f, 8f, 36f, 36f);
            Theme.FillRounded(g, circle, 18f, AccentColor);
            if (_selected)
            {
                using (Pen pen = new Pen(Color.White, 2f))
                {
                    g.DrawLines(pen, new PointF[]
                    {
                        new PointF(circle.X + 10f, circle.Y + 18f),
                        new PointF(circle.X + 15f, circle.Y + 23f),
                        new PointF(circle.X + 25f, circle.Y + 12f)
                    });
                }
            }

            TextRenderer.DrawText(g, LabelText, Fonts.Tiny,
                new Rectangle(0, 56, Width, 20),
                _selected ? Theme.Text : Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    internal class SettingsDialog : DialogForm
    {
        private static readonly Color[] PaletteColors = new Color[]
        {
            Color.FromArgb(109, 79, 224),
            Color.FromArgb(47, 111, 208),
            Color.FromArgb(30, 150, 168),
            Color.FromArgb(43, 154, 118),
            Color.FromArgb(209, 122, 46),
            Color.FromArgb(208, 68, 126)
        };

        private static readonly string[] PaletteNames = new string[]
        {
            "紫罗兰", "海盐蓝", "青瓷", "薄荷绿", "暖杏橙", "玫瑰粉"
        };

        private readonly AccentSwatch[] _swatches = new AccentSwatch[PaletteColors.Length];
        private readonly Label _hexLabel;
        private readonly CheckBox _adultCheck;

        public SettingsDialog(Color currentAccent, bool showAdult)
            : base("设置", new Size(640, 520))
        {
            SelectedColor = currentAccent;

            Label title = new Label();
            title.AutoSize = false;
            title.BackColor = Theme.Bg;
            title.ForeColor = Theme.Text;
            title.Font = Fonts.BodyBold;
            title.Text = "界面主题色";
            title.SetBounds(24, 54, 592, 24);
            Controls.Add(title);

            Label desc = new Label();
            desc.AutoSize = false;
            desc.BackColor = Theme.Bg;
            desc.ForeColor = Theme.TextMuted;
            desc.Font = Fonts.Small;
            desc.Text = "按钮、标签选中态、分类高亮和强调色会一起变化。";
            desc.SetBounds(24, 78, 592, 20);
            Controls.Add(desc);

            for (int i = 0; i < PaletteColors.Length; i++)
            {
                AccentSwatch swatch = new AccentSwatch(PaletteColors[i], PaletteNames[i]);
                swatch.SetBounds(24 + i * 96, 108, 64, 82);
                swatch.Click += delegate(object sender, EventArgs e)
                {
                    AccentSwatch clicked = sender as AccentSwatch;
                    if (clicked != null) SelectColor(clicked.AccentColor);
                };
                _swatches[i] = swatch;
                Controls.Add(swatch);
            }

            ModernButton custom = MakeButton("自定义颜色…", ButtonVariant.Secondary);
            custom.SetBounds(24, 208, 126, 34);
            custom.Click += delegate { ChooseCustomColor(); };
            Controls.Add(custom);

            _hexLabel = new Label();
            _hexLabel.AutoSize = false;
            _hexLabel.BackColor = Theme.Bg;
            _hexLabel.ForeColor = Theme.TextSecondary;
            _hexLabel.Font = Fonts.Small;
            _hexLabel.TextAlign = ContentAlignment.MiddleLeft;
            _hexLabel.SetBounds(164, 208, 180, 34);
            Controls.Add(_hexLabel);

            Label note = new Label();
            note.AutoSize = false;
            note.BackColor = Theme.Bg;
            note.ForeColor = Theme.TextMuted;
            note.Font = Fonts.Small;
            note.Text = "点击“确定”后立即应用。当前预设、标签和提示词内容不会改变。";
            note.SetBounds(24, 260, 592, 22);
            Controls.Add(note);

            Label contentTitle = new Label();
            contentTitle.AutoSize = false;
            contentTitle.BackColor = Theme.Bg;
            contentTitle.ForeColor = Theme.Text;
            contentTitle.Font = Fonts.BodyBold;
            contentTitle.Text = "内容显示";
            contentTitle.SetBounds(24, 294, 592, 24);
            Controls.Add(contentTitle);

            _adultCheck = new CheckBox();
            _adultCheck.AutoSize = false;
            _adultCheck.BackColor = Theme.Bg;
            _adultCheck.ForeColor = Theme.Text;
            _adultCheck.Font = Fonts.Body;
            _adultCheck.Text = "显示 18+ 内容（默认关闭）";
            _adultCheck.Checked = showAdult;
            _adultCheck.SetBounds(24, 324, 420, 26);
            Controls.Add(_adultCheck);

            Label adultHint = new Label();
            adultHint.AutoSize = false;
            adultHint.BackColor = Theme.Bg;
            adultHint.ForeColor = Theme.TextMuted;
            adultHint.Font = Fonts.Small;
            adultHint.Text = "关闭时隐藏成人向分类及其标签；全部分类、搜索和收藏也会过滤。";
            adultHint.SetBounds(44, 350, 572, 20);
            Controls.Add(adultHint);

            ModernButton reset = MakeButton("恢复默认", ButtonVariant.Ghost);
            reset.SetBounds(24, 392, 92, 32);
            reset.Click += delegate { SelectColor(Theme.DefaultAccent); };
            Controls.Add(reset);

            ModernButton cancel = MakeButton("取消", ButtonVariant.Secondary);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(416, 448, 88, 36);
            Controls.Add(cancel);

            ModernButton ok = MakeButton("确定", ButtonVariant.Primary);
            ok.SetBounds(512, 448, 96, 36);
            ok.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            AcceptButton = ok;
            CancelButton = cancel;
            SelectColor(currentAccent);
        }

        public Color SelectedColor { get; private set; }

        public bool SelectedShowAdult
        {
            get { return _adultCheck.Checked; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Focus();
        }

        private void ChooseCustomColor()
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.FullOpen = true;
                dialog.Color = SelectedColor;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    SelectColor(dialog.Color);
            }
        }

        private void SelectColor(Color color)
        {
            SelectedColor = color;
            bool matched = false;
            for (int i = 0; i < _swatches.Length; i++)
            {
                bool selected = _swatches[i].AccentColor.ToArgb() == color.ToArgb();
                _swatches[i].Selected = selected;
                if (selected) matched = true;
            }
            _hexLabel.Text = "当前：" + string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B)
                + (matched ? string.Empty : "（自定义）");
        }
    }
}
