using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal sealed class PresetListBox : ListBox
    {
        private readonly string _activeId;

        public PresetListBox(string activeId)
        {
            _activeId = activeId;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 62;
            BorderStyle = BorderStyle.None;
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            Font = Fonts.Body;
            IntegralHeight = false;
            SelectionMode = SelectionMode.One;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            PresetRecord preset = Items[e.Index] as PresetRecord;
            if (preset == null) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush back = new SolidBrush(selected ? Theme.AccentSoft : Theme.Surface))
            {
                g.FillRectangle(back, e.Bounds);
            }
            if (selected)
            {
                using (SolidBrush accent = new SolidBrush(Theme.Accent))
                {
                    g.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 8, 3, e.Bounds.Height - 16);
                }
            }

            TextRenderer.DrawText(g, preset.Name, Fonts.BodyBold,
                new Rectangle(e.Bounds.X + 16, e.Bounds.Y + 8, e.Bounds.Width - 110, 22),
                selected ? Theme.AccentLight : Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
            string meta = preset.Library.Categories.Count + " 个分类 · "
                + preset.Library.Tags.Count + " 个标签";
            TextRenderer.DrawText(g, meta, Fonts.Small,
                new Rectangle(e.Bounds.X + 16, e.Bounds.Y + 32, e.Bounds.Width - 40, 18),
                Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (preset.Id == _activeId)
            {
                Rectangle badge = new Rectangle(e.Bounds.Right - 64, e.Bounds.Y + 18, 46, 20);
                Theme.FillRounded(g, badge, 9f, Theme.AccentSoft);
                Theme.DrawRounded(g, badge, 9f, Theme.AccentBorder, 1f);
                TextRenderer.DrawText(g, "当前", Fonts.Tiny, badge, Theme.AccentLight,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            using (Pen line = new Pen(Theme.BorderSoft, 1f))
            {
                g.DrawLine(line, e.Bounds.X + 12, e.Bounds.Bottom - 1,
                    e.Bounds.Right - 12, e.Bounds.Bottom - 1);
            }
        }
    }

    internal class PresetDialog : DialogForm
    {
        private readonly PresetListBox _list;
        private readonly string _initialId;

        public PresetDialog(PresetCollection presets, string activeId)
            : base("预设管理", new Size(620, 520))
        {
            _initialId = activeId;

            Label caption = new Label();
            caption.AutoSize = false;
            caption.BackColor = Theme.Bg;
            caption.ForeColor = Theme.TextSecondary;
            caption.Font = Fonts.Body;
            caption.Text = "选择要使用的预设；每个预设都有独立的分类、小分类和标签。";
            caption.SetBounds(24, 56, 572, 24);
            Controls.Add(caption);

            _list = new PresetListBox(activeId);
            _list.SetBounds(24, 86, 572, 326);
            for (int i = 0; i < presets.Presets.Count; i++)
            {
                _list.Items.Add(presets.Presets[i]);
                if (presets.Presets[i].Id == activeId) _list.SelectedIndex = i;
            }
            _list.DoubleClick += delegate { AcceptSelection(); };
            Controls.Add(_list);

            ModernButton cancel = MakeButton("取消", ButtonVariant.Secondary);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(392, 440, 88, 36);
            Controls.Add(cancel);

            ModernButton use = MakeButton("使用此预设", ButtonVariant.Primary);
            use.SetBounds(488, 440, 108, 36);
            use.Click += delegate { AcceptSelection(); };
            Controls.Add(use);

            AcceptButton = use;
            CancelButton = cancel;
        }

        public string SelectedPresetId { get; private set; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_list.SelectedIndex < 0 && _list.Items.Count > 0) _list.SelectedIndex = 0;
            _list.Focus();
        }

        private void AcceptSelection()
        {
            PresetRecord preset = _list.SelectedItem as PresetRecord;
            if (preset == null) return;
            SelectedPresetId = preset.Id;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal enum PresetImportMode
    {
        CreateNew,
        MergeExisting
    }

    internal class ImportPresetDialog : DialogForm
    {
        private readonly PresetCollection _presets;
        private readonly RadioButton _createRadio;
        private readonly RadioButton _mergeRadio;
        private readonly TextBox _nameBox;
        private readonly ComboBox _mergeCombo;
        private readonly Label _error;
        private readonly ModernButton _importButton;

        public ImportPresetDialog(ImportedPreset imported, PresetCollection presets, string activeId)
            : base("导入预设", new Size(680, 540))
        {
            _presets = presets;

            Label source = new Label();
            source.AutoSize = false;
            source.BackColor = Theme.Bg;
            source.ForeColor = Theme.TextSecondary;
            source.Font = Fonts.Body;
            source.Text = "来源：" + imported.Name + "  ·  " + imported.CategoryCount
                + " 个分类 / " + imported.SubcategoryCount + " 个小分类 / "
                + imported.TagCount + " 个标签";
            source.SetBounds(24, 56, 632, 24);
            Controls.Add(source);

            _createRadio = MakeRadio("创建为新预设", 24, 92);
            _mergeRadio = MakeRadio("合并到已有预设", 24, 184);
            _createRadio.Checked = true;
            Controls.Add(_createRadio);
            Controls.Add(_mergeRadio);

            _nameBox = MakeTextBox();
            _nameBox.Text = presets.MakeUniqueName(imported.Name);
            RoundedPanel nameHost = MakeInputHost(_nameBox);
            nameHost.SetBounds(44, 122, 600, 40);
            Controls.Add(nameHost);

            _mergeCombo = new ComboBox();
            _mergeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _mergeCombo.FlatStyle = FlatStyle.Flat;
            _mergeCombo.BackColor = Theme.Surface;
            _mergeCombo.ForeColor = Theme.Text;
            _mergeCombo.Font = Fonts.Body;
            _mergeCombo.SetBounds(44, 214, 600, 28);
            int activeIndex = 0;
            for (int i = 0; i < presets.Presets.Count; i++)
            {
                _mergeCombo.Items.Add(presets.Presets[i]);
                if (presets.Presets[i].Id == activeId) activeIndex = i;
            }
            if (_mergeCombo.Items.Count > 0) _mergeCombo.SelectedIndex = activeIndex;
            Controls.Add(_mergeCombo);

            Label hint = new Label();
            hint.AutoSize = false;
            hint.BackColor = Theme.Bg;
            hint.ForeColor = Theme.TextMuted;
            hint.Font = Fonts.Small;
            hint.Text = "合并规则：分类和小分类按名称复用；标签按「中文 + English」识别。"
                + "中英文都相同的标签会合并归属；中文相同但 English 不同，或 English 相同但中文不同，会保留为两个不同标签。";
            hint.SetBounds(24, 256, 632, 58);
            Controls.Add(hint);

            _error = new Label();
            _error.AutoSize = false;
            _error.BackColor = Theme.Bg;
            _error.ForeColor = Theme.Red;
            _error.Font = Fonts.Small;
            _error.SetBounds(24, 326, 632, 22);
            Controls.Add(_error);

            ModernButton cancel = MakeButton("取消", ButtonVariant.Secondary);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(464, 454, 88, 36);
            Controls.Add(cancel);

            _importButton = MakeButton("导入", ButtonVariant.Primary);
            _importButton.SetBounds(560, 454, 96, 36);
            _importButton.Click += delegate { AcceptImport(); };
            Controls.Add(_importButton);

            AcceptButton = _importButton;
            CancelButton = cancel;

            _createRadio.CheckedChanged += delegate { UpdateMode(); };
            _mergeRadio.CheckedChanged += delegate { UpdateMode(); };
            UpdateMode();
        }

        public PresetImportMode Mode { get; private set; }

        public string NewPresetName
        {
            get { return _nameBox.Text.Trim(); }
        }

        public string TargetPresetId
        {
            get
            {
                PresetRecord preset = _mergeCombo.SelectedItem as PresetRecord;
                return preset == null ? string.Empty : preset.Id;
            }
        }

        private RadioButton MakeRadio(string text, int x, int y)
        {
            RadioButton radio = new RadioButton();
            radio.AutoSize = true;
            radio.BackColor = Theme.Bg;
            radio.ForeColor = Theme.Text;
            radio.Font = Fonts.BodyBold;
            radio.Text = text;
            radio.Location = new Point(x, y);
            return radio;
        }

        private void UpdateMode()
        {
            bool create = _createRadio.Checked;
            _nameBox.Enabled = create;
            _mergeCombo.Enabled = !create;
            _error.Text = string.Empty;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_createRadio.Checked)
            {
                _nameBox.Focus();
                _nameBox.SelectAll();
            }
        }

        private void AcceptImport()
        {
            if (_createRadio.Checked)
            {
                string name = NewPresetName;
                if (name.Length == 0)
                {
                    _error.Text = "新预设名称不能为空";
                    _nameBox.Focus();
                    return;
                }
                if (name.Length > 80)
                {
                    _error.Text = "新预设名称不能超过 80 个字符";
                    _nameBox.Focus();
                    return;
                }
                if (_presets.FindPresetByName(name, null) != null)
                {
                    _error.Text = "已有同名预设，请换一个名称";
                    _nameBox.Focus();
                    return;
                }
                Mode = PresetImportMode.CreateNew;
            }
            else
            {
                if (_mergeCombo.SelectedItem == null)
                {
                    _error.Text = "请选择要合并到的预设";
                    return;
                }
                Mode = PresetImportMode.MergeExisting;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
