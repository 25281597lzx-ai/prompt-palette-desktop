using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class ComposerPanel : RoundedPanel
    {
        private readonly TextBox _prompt;
        private readonly Label _placeholder;
        private readonly Label _tagCount;
        private readonly Label _langLabel;
        private readonly LanguageSwitch _language;
        private readonly RoundedPanel _inputHost;
        private readonly ModernButton _copy;
        private readonly ModernButton _clear;
        private readonly Label _editHint;

        public ComposerPanel()
        {
            Radius = 16;
            FillColor = Theme.Surface;
            BorderColor = Theme.Border;
            BackColor = Theme.Bg;

            Label title = new Label();
            title.AutoSize = false;
            title.BackColor = Theme.Surface;
            title.ForeColor = Theme.Text;
            title.Font = Fonts.BodyBold;
            title.Text = "当前提示词";
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.SetBounds(20, 13, 90, 26);

            _tagCount = new Label();
            _tagCount.AutoSize = false;
            _tagCount.BackColor = Theme.Surface;
            _tagCount.ForeColor = Theme.TextMuted;
            _tagCount.Font = Fonts.Small;
            _tagCount.Text = "0 个标签";
            _tagCount.TextAlign = ContentAlignment.MiddleLeft;
            _tagCount.SetBounds(108, 13, 78, 26);

            _langLabel = new Label();
            _langLabel.AutoSize = false;
            _langLabel.BackColor = Theme.Surface;
            _langLabel.ForeColor = Theme.TextFaint;
            _langLabel.Font = Fonts.Small;
            _langLabel.Text = "插入";
            _langLabel.TextAlign = ContentAlignment.MiddleLeft;
            _langLabel.SetBounds(190, 13, 30, 26);

            _language = new LanguageSwitch();
            _language.BackColor = Theme.Surface;
            _language.SetBounds(216, 13, 94, 26);
            _language.LanguageChanged += delegate
            {
                if (LanguageChanged != null) LanguageChanged(this, EventArgs.Empty);
            };

            _inputHost = new RoundedPanel();
            _inputHost.Radius = 10;
            _inputHost.FillColor = Theme.SurfaceDeep;
            _inputHost.BorderColor = Theme.BorderSoft;
            _inputHost.BackColor = Theme.Surface;

            _prompt = new TextBox();
            _prompt.Multiline = true;
            _prompt.AcceptsReturn = true;
            _prompt.AcceptsTab = false;
            _prompt.WordWrap = true;
            _prompt.ScrollBars = ScrollBars.None;
            _prompt.BorderStyle = BorderStyle.None;
            _prompt.BackColor = Theme.SurfaceDeep;
            _prompt.ForeColor = Theme.Text;
            _prompt.Font = Fonts.Prompt;
            _prompt.ShortcutsEnabled = true;
            _prompt.TextChanged += delegate
            {
                UpdatePlaceholder();
                UpdateMetrics();
                if (PromptChanged != null) PromptChanged(this, EventArgs.Empty);
            };
            _prompt.GotFocus += delegate
            {
                _inputHost.BorderColor = Theme.AccentBorder;
                UpdatePlaceholder();
                _inputHost.Invalidate();
            };
            _prompt.LostFocus += delegate
            {
                _inputHost.BorderColor = Theme.BorderSoft;
                UpdatePlaceholder();
                _inputHost.Invalidate();
            };

            _placeholder = new Label();
            _placeholder.AutoSize = false;
            _placeholder.BackColor = Theme.SurfaceDeep;
            _placeholder.ForeColor = Theme.TextMuted;
            _placeholder.Font = Fonts.Prompt;
            _placeholder.Text = "点击下方标签，开始组合你的提示词…";
            _placeholder.Cursor = Cursors.IBeam;
            _placeholder.Click += delegate { _prompt.Focus(); };

            _inputHost.Controls.Add(_prompt);
            _inputHost.Controls.Add(_placeholder);
            _placeholder.BringToFront();

            _copy = new ModernButton();
            _copy.Text = "复制提示词";
            _copy.Variant = ButtonVariant.Primary;
            _copy.ShowCopyIcon = true;
            _copy.BackColor = Theme.Surface;
            _copy.Enabled = false;
            _copy.Click += delegate
            {
                if (CopyRequested != null) CopyRequested(this, EventArgs.Empty);
            };

            _clear = new ModernButton();
            _clear.Text = "清空全部";
            _clear.Variant = ButtonVariant.Secondary;
            _clear.BackColor = Theme.Surface;
            _clear.Enabled = false;
            _clear.Click += delegate
            {
                if (ClearRequested != null) ClearRequested(this, EventArgs.Empty);
            };

            _editHint = new Label();
            _editHint.AutoSize = false;
            _editHint.BackColor = Theme.Surface;
            _editHint.ForeColor = Theme.TextMuted;
            _editHint.Font = Fonts.Tiny;
            _editHint.Text = "可直接编辑 · 用逗号分隔";
            _editHint.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(title);
            Controls.Add(_tagCount);
            Controls.Add(_langLabel);
            Controls.Add(_language);
            Controls.Add(_inputHost);
            Controls.Add(_copy);
            Controls.Add(_clear);
            Controls.Add(_editHint);

            LayoutChildren();
            UpdateMetrics();
        }

        public event EventHandler PromptChanged;
        public event EventHandler CopyRequested;
        public event EventHandler ClearRequested;
        public event EventHandler LanguageChanged;

        public PromptLanguage Language
        {
            get { return _language.Language; }
            set { _language.Language = value; }
        }

        public string PromptText
        {
            get { return _prompt.Text; }
            set
            {
                _prompt.Text = value ?? string.Empty;
                _prompt.SelectionStart = _prompt.TextLength;
                _prompt.SelectionLength = 0;
            }
        }

        public int PromptTextLength
        {
            get { return _prompt.TextLength; }
        }

        public void SetTagCount(int count)
        {
            _tagCount.Text = count + " 个标签";
        }

        public void SetCopyButtonText(string text)
        {
            _copy.Text = text;
        }

        public void ResetCopyButtonText()
        {
            _copy.Text = "复制提示词";
        }

        public void FocusPrompt()
        {
            _prompt.Focus();
        }

        public void RefreshTheme()
        {
            _inputHost.BorderColor = _prompt.Focused ? Theme.AccentBorder : Theme.BorderSoft;
            Invalidate(true);
        }

        public void ClearPrompt()
        {
            _prompt.Clear();
            _prompt.Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_prompt != null) LayoutChildren();
        }

        private void LayoutChildren()
        {
            int rightX = Width - 20 - 128;
            int promptWidth = rightX - 16 - 20;
            if (promptWidth < 200) promptWidth = 200;

            _inputHost.SetBounds(20, 48, promptWidth, 112);
            _prompt.SetBounds(14, 11, promptWidth - 28, 90);
            _placeholder.SetBounds(15, 10, promptWidth - 30, 26);

            _copy.SetBounds(rightX, 48, 128, 42);
            _clear.SetBounds(rightX, 98, 128, 32);
            _editHint.SetBounds(rightX, 136, 128, 18);
        }

        private void UpdatePlaceholder()
        {
            if (_placeholder == null) return;
            _placeholder.Visible = _prompt.TextLength == 0 && !_prompt.Focused;
        }

        private void UpdateMetrics()
        {
            bool hasText = _prompt.TextLength > 0;
            _copy.Enabled = hasText;
            _clear.Enabled = hasText;
        }
    }
}

