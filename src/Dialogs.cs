using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class DialogTitleBar : Control
    {
        private readonly Form _form;
        private readonly string _title;
        private bool _dragging;
        private Point _dragCursor;
        private Point _dragForm;

        public DialogTitleBar(Form form, string title)
        {
            _form = form;
            _title = title;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 46;
            Dock = DockStyle.Top;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }
            using (SolidBrush dot = new SolidBrush(Theme.Accent))
            {
                g.FillEllipse(dot, 24, 19, 6, 6);
            }
            TextRenderer.DrawText(g, _title, Fonts.BodyBold,
                new Rectangle(38, 0, Width - 90, Height), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
            using (Pen line = new Pen(Theme.BorderSoft, 1f))
            {
                g.DrawLine(line, 18, Height - 1, Width - 18, Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
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
    }

    internal abstract class DialogForm : Form
    {
        protected readonly DialogTitleBar DialogBar;
        protected readonly WindowButton CloseButton;

        protected DialogForm(string title, Size clientSize)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Fonts.Body;
            ClientSize = clientSize;
            Padding = new Padding(1);
            KeyPreview = true;
            DoubleBuffered = true;

            DialogBar = new DialogTitleBar(this, title);
            CloseButton = new WindowButton(WindowButtonKind.Close);
            CloseButton.Size = new Size(40, 36);
            CloseButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            DialogBar.Controls.Add(CloseButton);
            DialogBar.Resize += delegate
            {
                CloseButton.Location = new Point(DialogBar.Width - 44, 5);
            };
            Controls.Add(DialogBar);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.ApplyWindowChrome(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CloseButton.Location = new Point(DialogBar.Width - 44, 5);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Theme.Border, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected TextBox MakeTextBox()
        {
            TextBox box = new TextBox();
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.SurfaceDeep;
            box.ForeColor = Theme.Text;
            box.Font = Fonts.Prompt;
            box.ShortcutsEnabled = true;
            return box;
        }

        protected RoundedPanel MakeInputHost(TextBox box)
        {
            RoundedPanel host = new RoundedPanel();
            host.Radius = 9;
            host.FillColor = Theme.SurfaceDeep;
            host.BorderColor = Theme.Border;
            host.BackColor = Theme.Bg;
            box.BackColor = Theme.SurfaceDeep;
            box.SetBounds(12, 10, 100, 22);
            host.Controls.Add(box);
            host.Resize += delegate
            {
                box.SetBounds(12, 10, Math.Max(20, host.Width - 24), 22);
            };
            return host;
        }

        protected ModernButton MakeButton(string text, ButtonVariant variant)
        {
            ModernButton button = new ModernButton();
            button.Text = text;
            button.Variant = variant;
            button.BackColor = Theme.Bg;
            button.Size = new Size(92, 36);
            return button;
        }
    }

    internal class TextInputDialog : DialogForm
    {
        private readonly TextBox _input;
        private readonly Label _error;
        private readonly ModernButton _ok;
        public Func<string, string> Validator;

        public TextInputDialog(string title, string caption, string initialValue)
            : base(title, new Size(520, 250))
        {
            Label label = new Label();
            label.AutoSize = false;
            label.BackColor = Theme.Bg;
            label.ForeColor = Theme.TextSecondary;
            label.Font = Fonts.Body;
            label.Text = caption;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.SetBounds(24, 54, 472, 22);
            Controls.Add(label);

            _input = MakeTextBox();
            _input.Text = initialValue ?? string.Empty;
            RoundedPanel host = MakeInputHost(_input);
            host.SetBounds(24, 78, 472, 42);
            Controls.Add(host);

            _error = new Label();
            _error.AutoSize = false;
            _error.BackColor = Theme.Bg;
            _error.ForeColor = Theme.Red;
            _error.Font = Fonts.Small;
            _error.TextAlign = ContentAlignment.MiddleLeft;
            _error.SetBounds(24, 122, 472, 20);
            Controls.Add(_error);

            ModernButton cancel = MakeButton("取消", ButtonVariant.Secondary);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(316, 192, 88, 36);
            Controls.Add(cancel);

            _ok = MakeButton("确定", ButtonVariant.Primary);
            _ok.SetBounds(412, 192, 88, 36);
            _ok.Click += delegate { Accept(); };
            Controls.Add(_ok);

            AcceptButton = _ok;
            CancelButton = cancel;
        }

        public string Value
        {
            get { return _input.Text.Trim(); }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _input.Focus();
            _input.SelectAll();
        }

        private void Accept()
        {
            string value = Value;
            if (value.Length == 0)
            {
                _error.Text = "名称不能为空";
                _input.Focus();
                return;
            }
            if (Validator != null)
            {
                string error = Validator(value);
                if (!string.IsNullOrEmpty(error))
                {
                    _error.Text = error;
                    _input.Focus();
                    return;
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal class ConfirmDialog : DialogForm
    {
        public ConfirmDialog(string title, string message, string confirmText)
            : base(title, new Size(440, 218))
        {
            Label label = new Label();
            label.AutoSize = false;
            label.BackColor = Theme.Bg;
            label.ForeColor = Theme.TextSecondary;
            label.Font = Fonts.Body;
            label.Text = message;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.SetBounds(24, 58, 392, 76);
            Controls.Add(label);

            ModernButton cancel = MakeButton("取消", ButtonVariant.Secondary);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(232, 162, 88, 36);
            Controls.Add(cancel);

            ModernButton confirm = MakeButton(confirmText, ButtonVariant.Danger);
            confirm.DialogResult = DialogResult.OK;
            confirm.SetBounds(328, 162, 88, 36);
            Controls.Add(confirm);

            AcceptButton = confirm;
            CancelButton = cancel;
        }
    }

    internal class CheckRow : Control
    {
        private bool _checked;
        private bool _hover;

        public CheckRow(string subcategoryId, string text)
        {
            SubcategoryId = subcategoryId;
            Text = text;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.SurfaceDeep;
            Height = 32;
            Width = 560;
            Cursor = Cursors.Hand;
            TabStop = false;
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleName = text;
        }

        public string SubcategoryId { get; private set; }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler CheckedChanged;

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

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
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
                Theme.FillRounded(g, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 7f,
                    Color.FromArgb(245, 242, 251));
            }

            RectangleF box = new RectangleF(10f, 7f, 18f, 18f);
            if (_checked)
            {
                Theme.FillRounded(g, box, 5f, Theme.Accent);
                using (Pen check = new Pen(Color.White, 1.5f))
                {
                    check.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    check.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLines(check, new PointF[]
                    {
                        new PointF(14.5f, 16f),
                        new PointF(18f, 19.5f),
                        new PointF(24f, 12.5f)
                    });
                }
            }
            else
            {
                Theme.FillRounded(g, box, 5f, Color.White);
                Theme.DrawRounded(g, box, 5f, Theme.BorderHover, 1f);
            }

            TextRenderer.DrawText(g, Text, Fonts.Body,
                new Rectangle(38, 0, Width - 48, Height),
                _checked ? Theme.Text : Theme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
        }
    }

    internal class TagEditorDialog : DialogForm, IMessageFilter
    {
        private readonly PromptLibrary _library;
        private readonly PromptTag _tag;
        private readonly TextBox _chinese;
        private readonly TextBox _english;
        private readonly Panel _membershipContent;
        private readonly RoundedPanel _membershipBox;
        private readonly ScrollOverlay _membershipScroll;
        private readonly List<CheckRow> _rows = new List<CheckRow>();
        private int _membershipContentHeight = 344;
        private readonly Label _selectedCount;
        private readonly Label _error;

        public TagEditorDialog(PromptLibrary library, PromptTag tag, IEnumerable<string> preselected)
            : base(tag == null ? "新建标签" : "编辑标签", new Size(680, 640))
        {
            _library = library;
            _tag = tag;
            HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
            if (preselected != null)
            {
                foreach (string id in preselected)
                {
                    if (!string.IsNullOrEmpty(id)) selected.Add(id);
                }
            }
            if (tag != null)
            {
                for (int i = 0; i < tag.SubcategoryIds.Count; i++) selected.Add(tag.SubcategoryIds[i]);
            }

            Label chineseLabel = FieldLabel("中文名称");
            chineseLabel.SetBounds(24, 58, 296, 20);
            Controls.Add(chineseLabel);

            Label englishLabel = FieldLabel("English（可选）");
            englishLabel.SetBounds(332, 58, 324, 20);
            Controls.Add(englishLabel);

            _chinese = MakeTextBox();
            _chinese.Text = tag == null ? string.Empty : tag.Chinese;
            RoundedPanel chineseHost = MakeInputHost(_chinese);
            chineseHost.SetBounds(24, 79, 296, 40);
            Controls.Add(chineseHost);

            _english = MakeTextBox();
            _english.Text = tag == null ? string.Empty : tag.English;
            RoundedPanel englishHost = MakeInputHost(_english);
            englishHost.SetBounds(332, 79, 324, 40);
            Controls.Add(englishHost);

            Label membershipTitle = FieldLabel("归属小分类（可多选，标签可同时出现在多个分类）");
            membershipTitle.Font = Fonts.BodyBold;
            membershipTitle.SetBounds(24, 128, 500, 20);
            Controls.Add(membershipTitle);

            _selectedCount = new Label();
            _selectedCount.AutoSize = false;
            _selectedCount.BackColor = Theme.Bg;
            _selectedCount.ForeColor = Theme.AccentLight;
            _selectedCount.Font = Fonts.Small;
            _selectedCount.TextAlign = ContentAlignment.MiddleRight;
            _selectedCount.SetBounds(526, 128, 130, 20);
            Controls.Add(_selectedCount);

            _membershipBox = new RoundedPanel();
            _membershipBox.Radius = 10;
            _membershipBox.FillColor = Theme.SurfaceDeep;
            _membershipBox.BorderColor = Theme.Border;
            _membershipBox.BackColor = Theme.Bg;
            _membershipBox.SetBounds(24, 154, 632, 366);
            Controls.Add(_membershipBox);

            _membershipContent = new Panel();
            _membershipContent.BackColor = Theme.SurfaceDeep;
            _membershipContent.SetBounds(10, 10, 610, 344);
            _membershipContent.MouseWheel += delegate(object sender, MouseEventArgs e) { MembershipWheel(e); };
            _membershipBox.Controls.Add(_membershipContent);

            _membershipScroll = new ScrollOverlay(null);
            _membershipScroll.BackColor = Theme.SurfaceDeep;
            _membershipScroll.SetBounds(10 + 610 - SystemInformation.VerticalScrollBarWidth, 10,
                SystemInformation.VerticalScrollBarWidth, 344);
            _membershipScroll.ValueChanged += delegate(int value)
            {
                _membershipContent.Top = 10 - value;
            };
            _membershipBox.Controls.Add(_membershipScroll);
            _membershipScroll.BringToFront();

            BuildMembershipRows(selected);

            _error = new Label();
            _error.AutoSize = false;
            _error.BackColor = Theme.Bg;
            _error.ForeColor = Theme.Red;
            _error.Font = Fonts.Small;
            _error.TextAlign = ContentAlignment.MiddleLeft;
            _error.SetBounds(24, 530, 632, 20);
            Controls.Add(_error);

            if (tag != null)
            {
                ModernButton delete = MakeButton("删除标签", ButtonVariant.Danger);
                delete.SetBounds(24, 570, 104, 36);
                delete.Click += delegate { DeleteTag(); };
                Controls.Add(delete);
            }

            ModernButton cancel = MakeButton("取消", ButtonVariant.Secondary);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(462, 570, 88, 36);
            Controls.Add(cancel);

            ModernButton save = MakeButton(tag == null ? "添加标签" : "保存修改", ButtonVariant.Primary);
            save.SetBounds(558, 570, 98, 36);
            save.Click += delegate { Accept(); };
            Controls.Add(save);

            AcceptButton = save;
            CancelButton = cancel;
            UpdateSelectedCount();
        }

        public string TagChinese
        {
            get { return _chinese.Text.Trim(); }
        }

        public string TagEnglish
        {
            get { return _english.Text.Trim(); }
        }

        public bool Deleted { get; private set; }

        public List<string> SelectedSubcategoryIds
        {
            get
            {
                List<string> ids = new List<string>();
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Checked) ids.Add(_rows[i].SubcategoryId);
                }
                return ids;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SyncMembershipScroll();
            ScrollFirstCheckIntoView();
            _chinese.Focus();
            _chinese.SelectAll();
            Application.AddMessageFilter(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            base.OnFormClosed(e);
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL || Form.ActiveForm != this) return false;
            long value = m.LParam.ToInt64();
            int screenX = (short)(value & 0xFFFF);
            int screenY = (short)((value >> 16) & 0xFFFF);
            Point point = _membershipBox.PointToClient(new Point(screenX, screenY));
            if (point.X < 0 || point.Y < 0
                || point.X >= _membershipBox.ClientSize.Width || point.Y >= _membershipBox.ClientSize.Height)
                return false;
            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            _membershipScroll.ScrollBy(delta);
            return true;
        }

        private void MembershipWheel(MouseEventArgs e)
        {
            if (!_membershipScroll.Visible) return;
            _membershipScroll.ScrollBy(e.Delta);
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null) handled.Handled = true;
        }

        private void SyncMembershipScroll()
        {
            if (_membershipScroll == null) return;
            int max = Math.Max(0, _membershipContentHeight - 344);
            int value = Math.Max(0, 10 - _membershipContent.Top);
            _membershipScroll.SetMetrics(max, 344, value);
        }

        private void ScrollFirstCheckIntoView()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (!_rows[i].Checked) continue;
                int target = Math.Max(0, _rows[i].Top - 70);
                if (target > 0) _membershipScroll.SetValue(target);
                return;
            }
        }

        private Label FieldLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.BackColor = Theme.Bg;
            label.ForeColor = Theme.TextSecondary;
            label.Font = Fonts.Small;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private void BuildMembershipRows(HashSet<string> selected)
        {
            _membershipContent.SuspendLayout();
            int y = 8;
            for (int c = 0; c < _library.Categories.Count; c++)
            {
                PromptCategory category = _library.Categories[c];
                Label group = new Label();
                group.AutoSize = false;
                group.BackColor = Theme.SurfaceDeep;
                group.ForeColor = Theme.AccentLight;
                group.Font = Fonts.SmallBold;
                group.Text = category.Name;
                group.TextAlign = ContentAlignment.MiddleLeft;
                group.Padding = new Padding(4, 0, 0, 0);
                group.SetBounds(8, y, 580, 26);
                group.MouseWheel += delegate(object sender, MouseEventArgs e) { MembershipWheel(e); };
                _membershipContent.Controls.Add(group);
                y += 30;

                for (int s = 0; s < category.Subcategories.Count; s++)
                {
                    PromptSubcategory sub = category.Subcategories[s];
                    CheckRow row = new CheckRow(sub.Id, sub.Name);
                    row.Checked = selected.Contains(sub.Id);
                    row.CheckedChanged += delegate { UpdateSelectedCount(); };
                    row.MouseWheel += delegate(object sender, MouseEventArgs e) { MembershipWheel(e); };
                    row.SetBounds(14, y, 574, 32);
                    _rows.Add(row);
                    _membershipContent.Controls.Add(row);
                    y += 34;
                }
                y += 6;
            }

            if (_rows.Count == 0)
            {
                Label empty = new Label();
                empty.AutoSize = false;
                empty.BackColor = Theme.SurfaceDeep;
                empty.ForeColor = Theme.TextMuted;
                empty.Font = Fonts.Body;
                empty.Text = "还没有小分类，标签会先出现在「全部分类」中。";
                empty.TextAlign = ContentAlignment.MiddleLeft;
                empty.SetBounds(8, y, 580, 40);
                empty.MouseWheel += delegate(object sender, MouseEventArgs e) { MembershipWheel(e); };
                _membershipContent.Controls.Add(empty);
                y += 48;
            }

            _membershipContentHeight = Math.Max(344, y + 8);
            _membershipContent.Height = _membershipContentHeight;
            _membershipContent.ResumeLayout();
            SyncMembershipScroll();
        }

        private void UpdateSelectedCount()
        {
            int count = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Checked) count++;
            }
            _selectedCount.Text = "已选 " + count + " 个";
        }

        private void Accept()
        {
            if (TagChinese.Length == 0)
            {
                _error.Text = "中文名称不能为空";
                _chinese.Focus();
                return;
            }
            for (int i = 0; i < _library.Tags.Count; i++)
            {
                PromptTag other = _library.Tags[i];
                if (other == _tag) continue;
                if (string.Equals(other.Chinese, TagChinese, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(other.English, TagEnglish, StringComparison.OrdinalIgnoreCase))
                {
                    _error.Text = "已经有完全相同的中文 + English 标签了";
                    _chinese.Focus();
                    return;
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DeleteTag()
        {
            ConfirmDialog confirm = new ConfirmDialog("删除标签",
                "确定要删除「" + _tag.Label + "」吗？\r\n删除后，它会从所有分类和「全部分类」中移除。",
                "删除");
            if (confirm.ShowDialog(this) == DialogResult.OK)
            {
                Deleted = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}

