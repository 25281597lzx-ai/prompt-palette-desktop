using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class MainForm : Form, IMessageFilter
    {
        private readonly TitleBar _titleBar;
        private readonly WindowButton _minimize;
        private readonly WindowButton _maximize;
        private readonly WindowButton _close;
        private readonly TableLayoutPanel _root;
        private readonly Panel _body;
        private readonly HeroPanel _hero;
        private readonly ModernButton _presetButton;
        private readonly ModernButton _importButton;
        private readonly ModernButton _exportButton;
        private readonly ModernButton _settingsButton;
        private readonly ModernButton _editButton;
        private readonly ModernButton _resetButton;
        private readonly ModernButton _favoritesButton;
        private readonly ModernButton _favoriteFirstButton;
        private readonly ComposerPanel _composer;
        private readonly Panel _categoryHost;
        private readonly CategoryBar _categoryBar;
        private readonly CategoryBar _subcategoryBar;
        private readonly Label _allHint;
        private readonly SearchBox _search;
        private readonly TagBrowser _browser;
        private readonly FooterBar _footer;
        private readonly Timer _statusTimer;
        private readonly HashSet<string> _selectedTagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private AppSettings _settings;
        private FavoriteCollection _favorites;
        private bool _favoritesMode;
        private bool _favoriteFirst;
        private PresetCollection _presets;
        private PresetRecord _activePreset;
        private PromptLibrary _library;
        private bool _editMode;
        private bool _showAdult;
        private int _categoryIndex;
        private int _subIndex;
        private readonly HashSet<string> _adultSubcategoryIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            Text = "PromptPalette · 提示词灵感板";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 820);
            MinimumSize = new Size(1040, 780);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Fonts.Body;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            Padding = new Padding(6);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            try
            {
                string home = Environment.GetEnvironmentVariable("PROMPT_PALETTE_HOME");
                string iconPath = string.IsNullOrEmpty(home) ? null : Path.Combine(home, "assets", "app.ico");
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    Icon = new Icon(iconPath);
                else
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // The icon is only a finishing touch.
            }

            _settings = SettingsStore.Load();
            Theme.SetAccent(_settings.Accent);
            _showAdult = _settings.ShowAdult;
            _favoriteFirst = _settings.FavoriteFirst;
            _favorites = FavoritesStore.Load();
            _presets = PresetStore.LoadOrCreate();
            _activePreset = _presets.GetActive();
            if (_activePreset == null)
            {
                _activePreset = new PresetRecord(PresetStore.NewPresetId(), "默认模板", PromptTemplate.Create());
                _presets.Presets.Add(_activePreset);
                _presets.ActivePresetId = _activePreset.Id;
            }
            _library = _activePreset.Library;
            RebuildAdultSubcategoryIds();

            _titleBar = new TitleBar(this);
            _titleBar.Dock = DockStyle.Fill;
            _minimize = new WindowButton(WindowButtonKind.Minimize);
            _maximize = new WindowButton(WindowButtonKind.Maximize);
            _close = new WindowButton(WindowButtonKind.Close);
            _minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            _maximize.Click += delegate { ToggleMaximize(); };
            _close.Click += delegate { Close(); };
            _titleBar.Controls.Add(_minimize);
            _titleBar.Controls.Add(_maximize);
            _titleBar.Controls.Add(_close);
            _titleBar.Resize += delegate { PositionWindowButtons(); };

            _hero = new HeroPanel();
            _hero.Dock = DockStyle.Fill;

            _presetButton = new ModernButton();
            _presetButton.Variant = ButtonVariant.Secondary;
            _presetButton.BackColor = Theme.Bg;
            _presetButton.Size = new Size(190, 32);
            _presetButton.Click += delegate { OpenPresetManager(); };
            _hero.Controls.Add(_presetButton);

            _importButton = new ModernButton();
            _importButton.Text = "导入";
            _importButton.Variant = ButtonVariant.Secondary;
            _importButton.BackColor = Theme.Bg;
            _importButton.Size = new Size(60, 32);
            _importButton.Click += delegate { ImportPreset(); };
            _hero.Controls.Add(_importButton);

            _exportButton = new ModernButton();
            _exportButton.Text = "导出";
            _exportButton.Variant = ButtonVariant.Secondary;
            _exportButton.BackColor = Theme.Bg;
            _exportButton.Size = new Size(60, 32);
            _exportButton.Click += delegate { ExportPreset(); };
            _hero.Controls.Add(_exportButton);

            _settingsButton = new ModernButton();
            _settingsButton.Text = "设置";
            _settingsButton.Variant = ButtonVariant.Secondary;
            _settingsButton.BackColor = Theme.Bg;
            _settingsButton.Size = new Size(60, 32);
            _settingsButton.Click += delegate { OpenSettings(); };
            _hero.Controls.Add(_settingsButton);

            _editButton = new ModernButton();
            _editButton.Text = "编辑模式";
            _editButton.Variant = ButtonVariant.Secondary;
            _editButton.BackColor = Theme.Bg;
            _editButton.Size = new Size(104, 32);
            _editButton.Click += delegate { ToggleEditMode(); };
            _hero.Controls.Add(_editButton);

            _resetButton = new ModernButton();
            _resetButton.Text = "恢复模板";
            _resetButton.Variant = ButtonVariant.Ghost;
            _resetButton.BackColor = Theme.Bg;
            _resetButton.Size = new Size(92, 32);
            _resetButton.Visible = false;
            _resetButton.Click += delegate { ResetTemplate(); };
            _hero.Controls.Add(_resetButton);
            _hero.Resize += delegate { PositionHeroButtons(); };

            _composer = new ComposerPanel();
            _composer.Dock = DockStyle.Fill;

            _categoryHost = new Panel();
            _categoryHost.Dock = DockStyle.Fill;
            _categoryHost.BackColor = Theme.Bg;
            _categoryBar = new CategoryBar();
            _categoryBar.Compact = true;
            _subcategoryBar = new CategoryBar();
            _subcategoryBar.Compact = true;
            _subcategoryBar.Height = 30;
            _subcategoryBar.ShowAdd = false;
            _allHint = new Label();
            _allHint.AutoSize = false;
            _allHint.BackColor = Theme.Bg;
            _allHint.ForeColor = Theme.TextMuted;
            _allHint.Font = Fonts.Small;
            _allHint.Text = "全部分类  ·  所有标签都会出现在这里，并且可以跨分类复用";
            _allHint.TextAlign = ContentAlignment.MiddleLeft;
            _allHint.Visible = false;
            _search = new SearchBox();
            _favoritesButton = new ModernButton();
            _favoritesButton.Text = "☆ 收藏";
            _favoritesButton.Variant = ButtonVariant.Secondary;
            _favoritesButton.BackColor = Theme.Bg;
            _favoritesButton.Size = new Size(72, 38);
            _favoritesButton.Click += delegate { ToggleFavoritesView(); };
            _categoryHost.Controls.Add(_favoritesButton);

            _favoriteFirstButton = new ModernButton();
            _favoriteFirstButton.Text = "☆ 优先显示收藏";
            _favoriteFirstButton.Variant = ButtonVariant.Secondary;
            _favoriteFirstButton.BackColor = Theme.Bg;
            _favoriteFirstButton.Size = new Size(136, 38);
            _favoriteFirstButton.Click += delegate { ToggleFavoriteFirst(); };
            _categoryHost.Controls.Add(_favoriteFirstButton);

            _categoryHost.Controls.Add(_categoryBar);
            _categoryHost.Controls.Add(_subcategoryBar);
            _categoryHost.Controls.Add(_allHint);
            _categoryHost.Controls.Add(_search);
            _categoryHost.Resize += delegate { PositionCategoryControls(); };

            _browser = new TagBrowser();
            _browser.Dock = DockStyle.Fill;
            _browser.IsSelected = delegate(PromptTag tag) { return _selectedTagIds.Contains(tag.Id); };
            _browser.IsFavorite = delegate(PromptTag tag) { return _favorites.IsFavorite(tag); };
            _browser.FavoriteClicked += delegate(PromptTag tag) { ToggleFavorite(tag); };
            _browser.BadgeProvider = delegate(PromptTag tag)
            {
                int count = _library.GetDistinctCategoryCount(tag);
                if (count == 0) return "未归类";
                if (count > 1) return count + " 类";
                return string.Empty;
            };

            _footer = new FooterBar();
            _footer.Dock = DockStyle.Fill;

            _body = new Panel();
            _body.Dock = DockStyle.Fill;
            _body.BackColor = Theme.Bg;
            _body.Padding = new Padding(30, 12, 30, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.BackColor = Theme.Bg;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.Padding = new Padding(0);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 184f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            layout.Controls.Add(_hero, 0, 0);
            layout.Controls.Add(_composer, 0, 1);
            layout.Controls.Add(_categoryHost, 0, 2);
            layout.Controls.Add(_browser, 0, 3);
            layout.Controls.Add(_footer, 0, 4);
            _body.Controls.Add(layout);

            _root = new TableLayoutPanel();
            _root.Dock = DockStyle.Fill;
            _root.BackColor = Theme.Bg;
            _root.ColumnCount = 1;
            _root.RowCount = 2;
            _root.Padding = new Padding(0);
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _root.Controls.Add(_titleBar, 0, 0);
            _root.Controls.Add(_body, 0, 1);
            Controls.Add(_root);

            _categoryBar.SelectedIndexChanged += delegate
            {
                _favoritesMode = false;
                UpdateFavoriteButtons();
                _categoryIndex = _categoryBar.SelectedIndex;
                _subIndex = 0;
                RefreshSubcategoryBar();
                _search.Clear();
                RefreshView();
            };
            _subcategoryBar.SelectedIndexChanged += delegate
            {
                _subIndex = _subcategoryBar.SelectedIndex;
                _search.Clear();
                RefreshView();
            };
            _categoryBar.AddRequested += delegate { AddCategory(); };
            _subcategoryBar.AddRequested += delegate { AddSubcategory(); };
            _search.SearchChanged += delegate { _browser.SetFilter(_search.Query); };
            _browser.TagClicked += delegate(PromptTag tag)
            {
                if (_editMode && !_favoritesMode) EditTag(tag);
                else ToggleTag(tag);
            };
            _browser.AddTagRequested += delegate { AddTag(); };
            _composer.PromptChanged += delegate { RefreshPromptState(); };
            _composer.CopyRequested += delegate { CopyPrompt(); };
            _composer.ClearRequested += delegate { ClearPrompt(); };
            _composer.LanguageChanged += delegate
            {
                string text = _composer.Language == PromptLanguage.English
                    ? "插入语言：English"
                    : "插入语言：中文";
                ShowStatus(text, Theme.AccentLight);
            };

            _statusTimer = new Timer();
            _statusTimer.Interval = 2200;
            _statusTimer.Tick += delegate
            {
                _statusTimer.Stop();
                _footer.ClearStatus();
                _composer.ResetCopyButtonText();
            };

            Resize += delegate
            {
                _maximize.IsMaximized = WindowState == FormWindowState.Maximized;
                _titleBar.Invalidate();
            };

            RefreshCategoryBar(0);
            _subIndex = 0;
            RefreshSubcategoryBar();
            RefreshView();
            RefreshPromptState();
            UpdatePresetButtonText();
            UpdateFavoriteButtons();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.ApplyWindowChrome(Handle);
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PositionWindowButtons();
            PositionHeroButtons();
            PositionCategoryControls();
            UpdateMaximizedBounds();
            _maximize.IsMaximized = WindowState == FormWindowState.Maximized;
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
            if (m.Msg != WM_MOUSEWHEEL || Form.ActiveForm != this || _browser == null) return false;
            long value = m.LParam.ToInt64();
            int screenX = (short)(value & 0xFFFF);
            int screenY = (short)((value >> 16) & 0xFFFF);
            Point point = _browser.PointToClient(new Point(screenX, screenY));
            if (point.X < 0 || point.Y < 58
                || point.X >= _browser.ClientSize.Width || point.Y >= _browser.ClientSize.Height)
                return false;
            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            _browser.ScrollBy(delta);
            return true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Theme.Border, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool control = (keyData & Keys.Control) == Keys.Control;
            Keys key = keyData & Keys.KeyCode;
            if (control && key == Keys.Enter)
            {
                CopyPrompt();
                return true;
            }
            if (control && key == Keys.L)
            {
                ClearPrompt();
                return true;
            }
            if (control && key == Keys.F)
            {
                _search.FocusInput();
                return true;
            }
            if (control && key == Keys.E)
            {
                ToggleEditMode();
                return true;
            }
            if (control && key == Keys.Oemcomma)
            {
                OpenSettings();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private PromptCategory CurrentCategory
        {
            get
            {
                if (_favoritesMode) return null;
                if (_categoryIndex <= 0 || _categoryIndex > _library.Categories.Count) return null;
                return _library.Categories[_categoryIndex - 1];
            }
        }

        private PromptSubcategory CurrentSubcategory
        {
            get
            {
                PromptCategory category = CurrentCategory;
                if (category == null || _subIndex <= 0 || _subIndex > category.Subcategories.Count) return null;
                return category.Subcategories[_subIndex - 1];
            }
        }

        private void PositionWindowButtons()
        {
            if (_titleBar == null || _close == null) return;
            int x = _titleBar.Width - 10 - 46;
            _close.Location = new Point(x, 0);
            _maximize.Location = new Point(x - 42, 0);
            _minimize.Location = new Point(x - 84, 0);
        }

        private void PositionHeroButtons()
        {
            if (_hero == null || _editButton == null) return;
            int y = 18;
            int editX = _hero.Width - 104;
            _editButton.SetBounds(editX, y, 104, 32);

            int nextRight = editX;
            if (_resetButton.Visible)
            {
                _resetButton.SetBounds(nextRight - 8 - 92, y, 92, 32);
                nextRight -= 100;
            }
            else
            {
                _resetButton.SetBounds(nextRight - 8 - 92, y, 92, 32);
            }

            _settingsButton.SetBounds(nextRight - 8 - 60, y, 60, 32);
            _exportButton.SetBounds(nextRight - 8 - 60 - 8 - 60, y, 60, 32);
            _importButton.SetBounds(nextRight - 8 - 60 - 8 - 60 - 8 - 60, y, 60, 32);
            _presetButton.SetBounds(nextRight - 8 - 60 - 8 - 60 - 8 - 60 - 8 - 190, y, 190, 32);
        }

        private void PositionCategoryControls()
        {
            if (_categoryHost == null || _categoryBar == null) return;
            int searchWidth = 232;
            int favoritesWidth = 72;
            int priorityWidth = 136;
            int contentWidth = _categoryHost.Width;
            int searchX = contentWidth - searchWidth;
            int priorityX = searchX - 8 - priorityWidth;

            _favoritesButton.SetBounds(0, 8, favoritesWidth, 38);
            _categoryBar.SetBounds(favoritesWidth + 10, 8, Math.Max(0, contentWidth - favoritesWidth - 10), 38);
            _favoriteFirstButton.SetBounds(priorityX, 52, priorityWidth, 32);
            _search.SetBounds(searchX, 50, searchWidth, 38);
            _subcategoryBar.SetBounds(0, 52, Math.Max(0, priorityX - 10), 30);
            _allHint.SetBounds(0, 51, Math.Max(0, priorityX - 10), 30);
        }

        private void UpdatePresetButtonText()
        {
            if (_presetButton == null) return;
            string name = _activePreset == null ? "未命名" : (_activePreset.Name ?? "未命名").Trim();
            if (name.Length > 10) name = name.Substring(0, 10) + "…";
            _presetButton.Text = "预设：" + name;
            _presetButton.AccessibleName = "当前预设：" + name;
        }

        private void UpdateFavoriteButtons()
        {
            if (_favoritesButton == null || _favoriteFirstButton == null) return;
            _favoritesButton.Text = _favoritesMode ? "★ 收藏" : "☆ 收藏";
            _favoritesButton.Variant = _favoritesMode ? ButtonVariant.Primary : ButtonVariant.Secondary;
            _favoriteFirstButton.Text = _favoriteFirst ? "★ 优先显示收藏" : "☆ 优先显示收藏";
            _favoriteFirstButton.Variant = _favoriteFirst ? ButtonVariant.Primary : ButtonVariant.Secondary;
            _categoryBar.HideSelection = _favoritesMode;
            _favoritesButton.Invalidate();
            _favoriteFirstButton.Invalidate();
        }

        private void ToggleFavoritesView()
        {
            _favoritesMode = !_favoritesMode;
            if (_favoritesMode) _subIndex = 0;
            _search.Clear();
            UpdateFavoriteButtons();
            RefreshSubcategoryBar();
            RefreshView();
        }

        private void ToggleFavoriteFirst()
        {
            _favoriteFirst = !_favoriteFirst;
            _settings.FavoriteFirst = _favoriteFirst;
            bool saved = true;
            try
            {
                SettingsStore.Save(_settings);
            }
            catch
            {
                saved = false;
            }
            UpdateFavoriteButtons();
            string query = _search.Query;
            RefreshView();
            if (query.Length > 0) _browser.SetFilter(query);
            ShowStatus(_favoriteFirst ? "已开启优先显示收藏" : "已关闭优先显示收藏",
                saved ? Theme.AccentLight : Theme.Red);
        }

        private void ToggleFavorite(PromptTag tag)
        {
            if (tag == null) return;
            bool added = _favorites.Toggle(tag);
            bool saved = true;
            try
            {
                FavoritesStore.Save(_favorites);
            }
            catch
            {
                saved = false;
            }
            string query = _search.Query;
            RefreshView();
            if (query.Length > 0) _browser.SetFilter(query);
            ShowStatus((added ? "已收藏「" : "已取消收藏「") + tag.Label + "」"
                + (saved ? string.Empty : "（保存失败）"),
                saved ? (added ? Theme.AccentLight : Theme.TextMuted) : Theme.Red);
        }

        private List<PromptTag> GetFavoriteTags()
        {
            List<PromptTag> result = new List<PromptTag>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _favorites.Tags.Count; i++)
            {
                FavoriteRecord record = _favorites.Tags[i];
                string key = PresetExchangeIO.TagIdentity(record.Chinese, record.English);
                if (!seen.Add(key)) continue;
                PromptTag tag = FindFavoriteTag(record);
                if (tag != null) result.Add(tag);
            }
            return FilterVisibleTags(result);
        }

        private PromptTag FindFavoriteTag(FavoriteRecord record)
        {
            string key = PresetExchangeIO.TagIdentity(record.Chinese, record.English);
            for (int i = 0; i < _library.Tags.Count; i++)
            {
                PromptTag tag = _library.Tags[i];
                if (string.Equals(PresetExchangeIO.TagIdentity(tag.Chinese, tag.English),
                    key, StringComparison.Ordinal)) return tag;
            }
            for (int p = 0; p < _presets.Presets.Count; p++)
            {
                PromptLibrary library = _presets.Presets[p].Library;
                if (library == _library) continue;
                for (int i = 0; i < library.Tags.Count; i++)
                {
                    PromptTag tag = library.Tags[i];
                    if (string.Equals(PresetExchangeIO.TagIdentity(tag.Chinese, tag.English),
                        key, StringComparison.Ordinal)) return tag;
                }
            }
            return null;
        }

        private List<PromptTag> FavoritesFirst(List<PromptTag> tags)
        {
            if (!_favoriteFirst || tags == null || tags.Count == 0) return tags;
            List<PromptTag> result = new List<PromptTag>(tags.Count);
            for (int i = 0; i < tags.Count; i++)
            {
                if (_favorites.IsFavorite(tags[i])) result.Add(tags[i]);
            }
            for (int i = 0; i < tags.Count; i++)
            {
                if (!_favorites.IsFavorite(tags[i])) result.Add(tags[i]);
            }
            return result;
        }

        private void RefreshAfterPresetChange()
        {
            RebuildAdultSubcategoryIds();
            _favoritesMode = false;
            UpdateFavoriteButtons();
            _categoryIndex = 0;
            _subIndex = 0;
            _search.Clear();
            RefreshCategoryBar(0);
            RefreshSubcategoryBar();
            RefreshView();
            RefreshPromptState();
            UpdatePresetButtonText();
            PositionHeroButtons();
        }

        private void OpenPresetManager()
        {
            if (_presets == null) return;
            using (PresetDialog dialog = new PresetDialog(_presets, _activePreset == null ? string.Empty : _activePreset.Id))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                PresetRecord selected = _presets.FindPreset(dialog.SelectedPresetId);
                if (selected == null || selected.Id == _activePreset.Id) return;
                SwitchPreset(selected);
            }
        }

        private void SwitchPreset(PresetRecord preset)
        {
            if (preset == null) return;
            _activePreset = preset;
            _library = preset.Library;
            _presets.ActivePresetId = preset.Id;
            SavePresets();
            RefreshAfterPresetChange();
            ShowStatus("已切换到预设「" + preset.Name + "」", Theme.Green);
        }

        private void ImportPreset()
        {
            string path;
            using (OpenFileDialog open = new OpenFileDialog())
            {
                open.Title = "导入 PromptPalette 预设";
                open.Filter = "PromptPalette 预设 (*.json)|*.json|所有文件 (*.*)|*.*";
                open.CheckFileExists = true;
                if (open.ShowDialog(this) != DialogResult.OK) return;
                path = open.FileName;
            }

            ImportedPreset imported;
            string error;
            if (!PresetExchangeIO.ReadFile(path, out imported, out error))
            {
                MessageBox.Show(this, error, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string activeId = _activePreset == null ? string.Empty : _activePreset.Id;
            using (ImportPresetDialog dialog = new ImportPresetDialog(imported, _presets, activeId))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                if (dialog.Mode == PresetImportMode.CreateNew)
                {
                    PresetRecord preset = new PresetRecord(
                        PresetStore.NewPresetId(), dialog.NewPresetName, imported.Library);
                    _presets.Presets.Add(preset);
                    _presets.ActivePresetId = preset.Id;
                    _activePreset = preset;
                    _library = preset.Library;
                    if (!SavePresets())
                    {
                        ShowStatus("预设已创建，但保存失败", Theme.Red);
                        return;
                    }
                    RefreshAfterPresetChange();
                    ShowStatus("已导入为新预设「" + preset.Name + "」 · 标签 " + imported.TagCount + " 个", Theme.Green);
                }
                else
                {
                    PresetRecord target = _presets.FindPreset(dialog.TargetPresetId);
                    if (target == null) return;
                    MergeStats stats = PresetMerger.Merge(target.Library, imported.Library);
                    target.Library.Normalize();
                    _activePreset = target;
                    _library = target.Library;
                    _presets.ActivePresetId = target.Id;
                    if (!SavePresets())
                    {
                        ShowStatus("预设已合并，但保存失败", Theme.Red);
                        return;
                    }
                    RefreshAfterPresetChange();
                    ShowStatus("已合并到「" + target.Name + "」 · " + stats.Summary(), Theme.Green);
                }
            }
        }

        private void ExportPreset()
        {
            if (_activePreset == null) return;
            string fileName = SanitizeFileName(_activePreset.Name) + ".ppreset.json";
            string path;
            using (SaveFileDialog save = new SaveFileDialog())
            {
                save.Title = "导出当前预设";
                save.Filter = "PromptPalette 预设 (*.json)|*.json|所有文件 (*.*)|*.*";
                save.FileName = fileName;
                save.AddExtension = true;
                save.DefaultExt = "json";
                if (save.ShowDialog(this) != DialogResult.OK) return;
                path = save.FileName;
            }

            try
            {
                PresetExchange exchange = PresetExchangeIO.BuildExchange(_library, _activePreset.Name);
                PresetExchangeIO.WriteFile(path, exchange);
                ShowStatus("已导出预设「" + _activePreset.Name + "」", Theme.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "导出失败：" + ex.Message, "导出失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string SanitizeFileName(string name)
        {
            string safe = string.IsNullOrEmpty(name) ? "preset" : name.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++) safe = safe.Replace(invalid[i], '_');
            if (safe.Length == 0) safe = "preset";
            if (safe.Length > 60) safe = safe.Substring(0, 60);
            return safe;
        }

        private void OpenSettings()
        {
            using (SettingsDialog dialog = new SettingsDialog(Theme.Accent, _showAdult))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Color accent = dialog.SelectedColor;
                bool showAdult = dialog.SelectedShowAdult;
                bool adultChanged = showAdult != _showAdult;
                Theme.SetAccent(accent);
                _settings.AccentArgb = accent.ToArgb();
                _settings.ShowAdult = showAdult;
                _showAdult = showAdult;
                bool saved = true;
                try
                {
                    SettingsStore.Save(_settings);
                }
                catch (Exception ex)
                {
                    saved = false;
                    ShowStatus("设置已应用，但保存失败：" + ex.Message, Theme.Red);
                }
                _search.RefreshTheme();
                _composer.RefreshTheme();
                if (adultChanged) ApplyShowAdultChange();
                Invalidate(true);
                _titleBar.Invalidate();
                if (saved) ShowStatus("设置已更新", Theme.AccentLight);
            }
        }

        private void ApplyShowAdultChange()
        {
            if (!_showAdult && _categoryIndex > 0
                && IsAdultCategory(_library.Categories[_categoryIndex - 1]))
            {
                _categoryIndex = 0;
                _subIndex = 0;
            }
            _favoritesMode = false;
            UpdateFavoriteButtons();
            RefreshCategoryBar(_categoryIndex);
            RefreshSubcategoryBar();
            RefreshView();
            RefreshPromptState();
        }

        private void RefreshCategoryBar(int selectedIndex)
        {
            List<string> names = new List<string>();
            names.Add("全部分类");
            for (int i = 0; i < _library.Categories.Count; i++)
            {
                PromptCategory category = _library.Categories[i];
                if (!_showAdult && IsAdultCategory(category)) continue;
                names.Add(category.Name);
            }
            if (selectedIndex < 0 || selectedIndex >= names.Count) selectedIndex = 0;
            _categoryIndex = selectedIndex;
            _categoryBar.SetItems(names, selectedIndex);
            _categoryBar.ShowAdd = _editMode;
        }

        private static bool IsAdultCategory(PromptCategory category)
        {
            return category != null && string.Equals(category.Name, "成人向", StringComparison.OrdinalIgnoreCase);
        }

        private void RebuildAdultSubcategoryIds()
        {
            _adultSubcategoryIds.Clear();
            for (int i = 0; i < _library.Categories.Count; i++)
            {
                PromptCategory category = _library.Categories[i];
                if (!IsAdultCategory(category)) continue;
                for (int s = 0; s < category.Subcategories.Count; s++)
                    _adultSubcategoryIds.Add(category.Subcategories[s].Id);
            }
        }

        private bool IsAdultTag(PromptTag tag)
        {
            if (tag == null || _adultSubcategoryIds.Count == 0) return false;
            for (int i = 0; i < tag.SubcategoryIds.Count; i++)
            {
                if (_adultSubcategoryIds.Contains(tag.SubcategoryIds[i])) return true;
            }
            return false;
        }

        private List<PromptTag> FilterVisibleTags(List<PromptTag> tags)
        {
            List<PromptTag> result = new List<PromptTag>();
            if (tags == null) return result;
            for (int i = 0; i < tags.Count; i++)
            {
                if (_showAdult || !IsAdultTag(tags[i])) result.Add(tags[i]);
            }
            return result;
        }

        private void RefreshSubcategoryBar()
        {
            if (_favoritesMode)
            {
                _subcategoryBar.Visible = false;
                _allHint.Visible = true;
                _allHint.Text = "收藏  ·  所有预设中收藏过的标签都会汇总在这里，可跨预设查找";
                return;
            }

            PromptCategory category = CurrentCategory;
            if (category == null)
            {
                _subcategoryBar.Visible = false;
                _allHint.Visible = true;
                _allHint.Text = "全部分类  ·  所有标签都会出现在这里，并且可以跨分类复用";
                return;
            }

            List<string> names = new List<string>();
            names.Add("全部");
            for (int i = 0; i < category.Subcategories.Count; i++)
            {
                names.Add(category.Subcategories[i].Name);
            }
            if (_subIndex < 0 || _subIndex >= names.Count) _subIndex = 0;
            _subcategoryBar.Visible = true;
            _allHint.Visible = false;
            _subcategoryBar.SetItems(names, _subIndex);
            _subcategoryBar.ShowAdd = _editMode;
        }

        private void RefreshView()
        {
            PromptCategory category = CurrentCategory;
            List<PromptTag> tags;
            string title;
            string description;
            bool all;

            if (_favoritesMode)
            {
                all = false;
                tags = FilterVisibleTags(GetFavoriteTags());
                title = "收藏";
                description = "跨预设收藏 · " + tags.Count + " 个标签，点卡片即可加入提示词";
            }
            else if (category == null)
            {
                all = true;
                tags = FilterVisibleTags(_library.GetAllTags());
                title = "全部分类";
                description = "共 " + tags.Count + " 个标签，所有分类中的标签都会出现在这里，可跨分类复用";
            }
            else if (_subIndex <= 0)
            {
                all = false;
                tags = FilterVisibleTags(_library.GetTagsForCategory(category));
                title = category.Name;
                description = category.Subcategories.Count == 0
                    ? "还没有小分类，进入编辑模式后点 + 新增小分类"
                    : category.Subcategories.Count + " 个小分类 · 标签可跨分类复用";
            }
            else
            {
                all = false;
                PromptSubcategory sub = CurrentSubcategory;
                tags = sub == null ? new List<PromptTag>()
                    : FilterVisibleTags(_library.GetTagsForSubcategory(sub.Id));
                title = category.Name + " · " + (sub == null ? "全部" : sub.Name);
                description = "该小分类下的标签 · 可同时归入其他分类";
            }

            if (!_favoritesMode) tags = FavoritesFirst(tags);
            _browser.SetView(title, description, tags, _favoritesMode ? false : all);
            _browser.EditMode = _editMode && !_favoritesMode;
            if (_favoritesMode)
            {
                _browser.SetEmptyMessage("还没有收藏的标签",
                    "在任意标签卡片右上角点 ☆；收藏会跨预设汇总到这里");
            }
            else
            {
                _browser.SetEmptyMessage(string.Empty, string.Empty);
            }
            _browser.RefreshSelection();
            _search.SetPlaceholder(_favoritesMode
                ? "搜索收藏标签"
                : (category == null
                    ? "搜索全部标签"
                    : (_subIndex > 0 ? "搜索当前小分类" : "搜索当前分类")));
        }

        private void ToggleEditMode()
        {
            _editMode = !_editMode;
            _editButton.Text = _editMode ? "完成编辑" : "编辑模式";
            _editButton.Variant = _editMode ? ButtonVariant.Primary : ButtonVariant.Secondary;
            _resetButton.Visible = false;
            _categoryBar.ShowAdd = _editMode;
            _subcategoryBar.ShowAdd = _editMode;
            _browser.EditMode = _editMode && !_favoritesMode;
            PositionHeroButtons();
            ShowStatus(_editMode
                ? "编辑模式：上方 + 新增分类/小分类，点击卡片修改标签"
                : "已退出编辑模式", _editMode ? Theme.AccentLight : Theme.TextMuted);
        }

        private void AddCategory()
        {
            if (!_editMode) return;
            TextInputDialog dialog = new TextInputDialog("新增分类", "分类名称", string.Empty);
            dialog.Validator = delegate(string value)
            {
                for (int i = 0; i < _library.Categories.Count; i++)
                {
                    if (string.Equals(_library.Categories[i].Name, value, StringComparison.OrdinalIgnoreCase))
                        return "已经有同名分类了";
                }
                return null;
            };
            DialogResult result = dialog.ShowDialog(this);
            string name = dialog.Value;
            dialog.Dispose();
            if (result != DialogResult.OK) return;

            _library.AddCategory(name);
            SavePresets();
            _favoritesMode = false;
            UpdateFavoriteButtons();
            _categoryIndex = _library.Categories.Count;
            _subIndex = 0;
            RefreshCategoryBar(_categoryIndex);
            RefreshSubcategoryBar();
            _search.Clear();
            RefreshView();
            ShowStatus("已新增分类「" + name + "」，可继续点 + 新增小分类", Theme.Green);
        }

        private void AddSubcategory()
        {
            if (!_editMode) return;
            PromptCategory category = CurrentCategory;
            if (category == null)
            {
                ShowStatus("请先在顶部选择一个分类，再新增小分类", Theme.AccentLight);
                return;
            }
            TextInputDialog dialog = new TextInputDialog("新增小分类", "在「" + category.Name + "」下新增小分类", string.Empty);
            dialog.Validator = delegate(string value)
            {
                for (int i = 0; i < category.Subcategories.Count; i++)
                {
                    if (string.Equals(category.Subcategories[i].Name, value, StringComparison.OrdinalIgnoreCase))
                        return "该分类下已经有同名小分类了";
                }
                return null;
            };
            DialogResult result = dialog.ShowDialog(this);
            string name = dialog.Value;
            dialog.Dispose();
            if (result != DialogResult.OK) return;

            _library.AddSubcategory(category, name);
            SavePresets();
            _subIndex = category.Subcategories.Count;
            RefreshSubcategoryBar();
            _search.Clear();
            RefreshView();
            ShowStatus("已新增小分类「" + name + "」", Theme.Green);
        }

        private void AddTag()
        {
            if (!_editMode) return;
            List<string> preselected = new List<string>();
            PromptSubcategory sub = CurrentSubcategory;
            if (sub != null) preselected.Add(sub.Id);

            TagEditorDialog dialog = new TagEditorDialog(_library, null, preselected);
            DialogResult result = dialog.ShowDialog(this);
            string chinese = dialog.TagChinese;
            string english = dialog.TagEnglish;
            List<string> memberships = dialog.SelectedSubcategoryIds;
            dialog.Dispose();
            if (result != DialogResult.OK) return;

            _library.AddTag(chinese, english, memberships);
            SavePresets();
            _search.Clear();
            RefreshView();
            RefreshPromptState();
            ShowStatus("已新增标签「" + chinese + "」", Theme.Green);
        }

        private void EditTag(PromptTag tag)
        {
            if (tag == null) return;
            TagEditorDialog dialog = new TagEditorDialog(_library, tag, null);
            DialogResult result = dialog.ShowDialog(this);
            string chinese = dialog.TagChinese;
            string english = dialog.TagEnglish;
            List<string> memberships = dialog.SelectedSubcategoryIds;
            bool deleted = dialog.Deleted;
            dialog.Dispose();
            if (result != DialogResult.OK) return;

            if (deleted)
            {
                _library.RemoveTag(tag);
                SavePresets();
                ShowStatus("已删除标签「" + tag.Label + "」", Theme.TextMuted);
            }
            else
            {
                _library.UpdateTag(tag, chinese, english, memberships);
                SavePresets();
                ShowStatus("已保存标签「" + chinese + "」", Theme.Green);
            }
            _search.Clear();
            RefreshView();
            RefreshPromptState();
        }

        private void ResetTemplate()
        {
            ConfirmDialog confirm = new ConfirmDialog("恢复默认模板",
                "会覆盖当前预设的分类、小分类和标签，恢复为内置的 8 个分类模板。\r\n其他预设不受影响。此操作不可撤销，确定继续吗？",
                "恢复模板");
            if (confirm.ShowDialog(this) != DialogResult.OK) return;

            _library = PromptTemplate.Create();
            if (_activePreset != null) _activePreset.Library = _library;
            if (!SavePresets())
            {
                ShowStatus("模板已恢复，但保存失败", Theme.Red);
                return;
            }
            RefreshAfterPresetChange();
            ShowStatus("当前预设已恢复默认模板", Theme.Green);
        }

        private bool SavePresets()
        {
            try
            {
                if (_activePreset != null)
                {
                    _activePreset.Library = _library;
                    _presets.ActivePresetId = _activePreset.Id;
                }
                PresetStore.Save(_presets);
                return true;
            }
            catch (Exception ex)
            {
                ShowStatus("保存失败：" + ex.Message, Theme.Red);
                return false;
            }
        }

        private void ToggleTag(PromptTag tag)
        {
            bool wasSelected = _selectedTagIds.Contains(tag.Id);
            List<string> tokens = ParseTokens(_composer.PromptText);
            if (wasSelected)
            {
                for (int i = tokens.Count - 1; i >= 0; i--)
                {
                    if (SameToken(tokens[i], tag.Chinese)
                        || (!string.IsNullOrEmpty(tag.English) && SameToken(tokens[i], tag.English)))
                    {
                        tokens.RemoveAt(i);
                    }
                }
                _selectedTagIds.Remove(tag.Id);
            }
            else
            {
                string prompt = tag.Prompt(_composer.Language);
                bool alreadyThere = false;
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (SameToken(tokens[i], prompt)) { alreadyThere = true; break; }
                }
                if (!alreadyThere) tokens.Add(prompt);
                _selectedTagIds.Add(tag.Id);
            }

            _composer.PromptText = string.Join(", ", tokens.ToArray());
            _composer.FocusPrompt();
            ShowStatus(wasSelected ? "已移除「" + tag.Label + "」" : "已加入「" + tag.Label + "」",
                wasSelected ? Theme.TextMuted : Theme.Green);
        }

        private static bool SameToken(string left, string right)
        {
            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ParseTokens(string text)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrEmpty(text)) return tokens;
            string[] parts = text.Split(new char[] { ',', '，', ';', '；', '、', '\r', '\n' });
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (token.Length > 0) tokens.Add(token);
            }
            return tokens;
        }

        private void RefreshPromptState()
        {
            string text = _composer.PromptText;
            List<string> tokens = ParseTokens(text);
            HashSet<string> tokenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tokens.Count; i++) tokenSet.Add(tokens[i]);

            _selectedTagIds.Clear();
            HashSet<string> scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _library.Tags.Count; i++)
            {
                PromptTag tag = _library.Tags[i];
                scanned.Add(tag.Id);
                if (!_showAdult && IsAdultTag(tag)) continue;
                if (IsTagLinked(tag, text, tokenSet)) _selectedTagIds.Add(tag.Id);
            }
            if (_favorites.Tags.Count > 0)
            {
                List<PromptTag> favoriteTags = GetFavoriteTags();
                for (int i = 0; i < favoriteTags.Count; i++)
                {
                    PromptTag tag = favoriteTags[i];
                    if (!scanned.Add(tag.Id)) continue;
                    if (IsTagLinked(tag, text, tokenSet)) _selectedTagIds.Add(tag.Id);
                }
            }

            _composer.SetTagCount(_selectedTagIds.Count);
            _browser.RefreshSelection();
        }

        private static bool IsTagLinked(PromptTag tag, string text, HashSet<string> tokenSet)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (tokenSet.Contains(tag.Chinese)) return true;
            if (!string.IsNullOrEmpty(tag.English) && tokenSet.Contains(tag.English)) return true;
            if (!string.IsNullOrEmpty(tag.Chinese)
                && text.IndexOf(tag.Chinese, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (!string.IsNullOrEmpty(tag.English)
                && ContainsEnglishPhrase(text, tag.English))
                return true;
            return false;
        }

        private static bool ContainsEnglishPhrase(string text, string phrase)
        {
            int searchFrom = 0;
            while (searchFrom <= text.Length - phrase.Length)
            {
                int index = text.IndexOf(phrase, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (index < 0) return false;
                int end = index + phrase.Length;
                bool left = index == 0 || !IsWordChar(text[index - 1]);
                bool right = end >= text.Length || !IsWordChar(text[end]);
                if (left && right) return true;
                searchFrom = index + 1;
            }
            return false;
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private void CopyPrompt()
        {
            string text = _composer.PromptText.Trim();
            if (text.Length == 0)
            {
                ShowStatus("提示词还是空的，先点一个标签吧", Theme.AccentLight);
                _composer.FocusPrompt();
                return;
            }

            try
            {
                Clipboard.SetText(text);
                _composer.SetCopyButtonText("已复制 ✓");
                ShowStatus("已复制到剪贴板 · " + _selectedTagIds.Count + " 个标签", Theme.Green);
            }
            catch (ExternalException)
            {
                ShowStatus("复制失败，剪贴板正被其他程序占用", Theme.Red);
            }
        }

        private void ClearPrompt()
        {
            if (_composer.PromptTextLength == 0)
            {
                _composer.FocusPrompt();
                return;
            }
            _composer.ClearPrompt();
            ShowStatus("已清空，可重新选择标签", Theme.TextMuted);
        }

        private void ShowStatus(string text, Color color)
        {
            _footer.ShowStatus(text, color);
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void ToggleMaximize()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                UpdateMaximizedBounds();
                WindowState = FormWindowState.Maximized;
            }
            _maximize.IsMaximized = WindowState == FormWindowState.Maximized;
        }

        private void UpdateMaximizedBounds()
        {
            Screen screen = Screen.FromControl(this);
            MaximizedBounds = screen.WorkingArea;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                if (m.Result.ToInt32() == 1)
                {
                    long value = m.LParam.ToInt64();
                    int screenX = (short)(value & 0xFFFF);
                    int screenY = (short)((value >> 16) & 0xFFFF);
                    Point point = PointToClient(new Point(screenX, screenY));
                    const int edge = 6;
                    bool left = point.X >= 0 && point.X < edge;
                    bool right = point.X <= ClientSize.Width && point.X > ClientSize.Width - edge;
                    bool top = point.Y >= 0 && point.Y < edge;
                    bool bottom = point.Y <= ClientSize.Height && point.Y > ClientSize.Height - edge;

                    int hit = -1;
                    if (top && left) hit = NativeMethods.HTTOPLEFT;
                    else if (top && right) hit = NativeMethods.HTTOPRIGHT;
                    else if (bottom && left) hit = NativeMethods.HTBOTTOMLEFT;
                    else if (bottom && right) hit = NativeMethods.HTBOTTOMRIGHT;
                    else if (left) hit = NativeMethods.HTLEFT;
                    else if (right) hit = NativeMethods.HTRIGHT;
                    else if (top) hit = NativeMethods.HTTOP;
                    else if (bottom) hit = NativeMethods.HTBOTTOM;

                    if (hit >= 0)
                    {
                        m.Result = (IntPtr)hit;
                        return;
                    }
                }
                return;
            }
            base.WndProc(ref m);
        }
    }
}

