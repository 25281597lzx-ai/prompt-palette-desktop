using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PromptPalette
{
    internal class ScrollOverlay : Control
    {
        private readonly ScrollableControl _target;
        private int _max;
        private int _viewport;
        private int _value;
        private bool _dragging;
        private int _dragStartY;
        private int _dragStartValue;

        public ScrollOverlay(ScrollableControl target)
        {
            _target = target;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.TagArea;
            Width = SystemInformation.VerticalScrollBarWidth;
            Cursor = Cursors.Hand;
            Visible = false;
        }

        public event Action<int> ValueChanged;

        public int Value
        {
            get { return _value; }
        }

        public void SetMetrics(int max, int viewport, int value)
        {
            _max = Math.Max(0, max);
            _viewport = Math.Max(0, viewport);
            _value = Math.Max(0, Math.Min(_max, value));
            Visible = _max > 0;
            Invalidate();
        }

        public void SetValue(int value)
        {
            if (_max <= 0) return;
            _value = Math.Max(0, Math.Min(_max, value));
            if (_target != null) _target.AutoScrollPosition = new Point(0, _value);
            if (ValueChanged != null) ValueChanged(_value);
            Invalidate();
        }

        public void ScrollBy(int delta)
        {
            SetValue(_value - delta / 2);
        }

        private int ThumbHeight()
        {
            int track = Math.Max(1, Height - 8);
            if (_max <= 0) return track;
            int visible = Math.Max(24, (int)(track * (_viewport / (double)(_viewport + _max))));
            return Math.Min(track, visible);
        }

        private int ThumbTop()
        {
            int track = Math.Max(1, Height - 8);
            int thumb = ThumbHeight();
            if (_max <= 0) return 4;
            return 4 + (int)((track - thumb) * (_value / (double)_max));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }
            if (_max <= 0) return;

            int cx = Width / 2;
            Theme.FillRounded(g, new RectangleF(cx - 2f, 4f, 4f, Height - 8), 2f,
                Theme.AccentSoft);
            int thumbTop = ThumbTop();
            int thumbHeight = ThumbHeight();
            Theme.FillRounded(g, new RectangleF(cx - 3f, thumbTop, 6f, thumbHeight), 3f,
                _dragging ? Theme.Accent : Theme.AccentThumb);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _max <= 0) return;
            int thumbTop = ThumbTop();
            int thumbHeight = ThumbHeight();
            if (e.Y >= thumbTop && e.Y <= thumbTop + thumbHeight)
            {
                _dragging = true;
                _dragStartY = e.Y;
                _dragStartValue = _value;
                Capture = true;
            }
            else if (e.Y < thumbTop)
            {
                SetValue(_value - _viewport);
            }
            else
            {
                SetValue(_value + _viewport);
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            int track = Math.Max(1, Height - 8);
            int thumb = ThumbHeight();
            int range = Math.Max(1, track - thumb);
            int delta = e.Y - _dragStartY;
            int value = _dragStartValue + (int)(_max * (delta / (double)range));
            SetValue(value);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            Capture = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollBy(e.Delta);
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null) handled.Handled = true;
        }
    }

    internal class TagGrid : Control
    {
        private const int Gap = 8;
        private const int CardHeight = 58;
        private const int MinCardWidth = 118;
        private const int MaxColumns = 12;

        private List<PromptTag> _tags = new List<PromptTag>();
        private Predicate<PromptTag> _isSelected = delegate { return false; };
        private Predicate<PromptTag> _isFavorite = delegate { return false; };
        private Func<PromptTag, string> _badgeProvider;
        private bool _showBadges;
        private bool _editMode;
        private int _scrollOffset;
        private int _hoverIndex = -1;
        private int _columns = 1;
        private int _cardWidth = 128;
        private int _contentHeight;

        public TagGrid()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.TagArea;
            TabStop = false;
        }

        public event Action<PromptTag> TagClicked;
        public event Action<PromptTag> FavoriteClicked;
        public event EventHandler ScrollMetricsChanged;

        public int ContentHeight
        {
            get { return _contentHeight; }
        }

        public int ScrollOffset
        {
            get { return _scrollOffset; }
        }

        public void SetTags(List<PromptTag> tags)
        {
            _tags = tags ?? new List<PromptTag>();
            _hoverIndex = -1;
            Relayout();
            Invalidate();
        }

        public void SetSelection(Predicate<PromptTag> isSelected)
        {
            _isSelected = isSelected ?? delegate { return false; };
            Invalidate();
        }

        public void SetFavoriteProvider(Predicate<PromptTag> isFavorite)
        {
            _isFavorite = isFavorite ?? delegate { return false; };
            Invalidate();
        }

        public void SetBadgeProvider(Func<PromptTag, string> provider, bool showBadges)
        {
            _badgeProvider = provider;
            _showBadges = showBadges;
            Invalidate();
        }

        public void SetEditMode(bool editMode)
        {
            _editMode = editMode;
            Invalidate();
        }

        public void RefreshSelection()
        {
            Invalidate();
        }

        public void RefreshBadges()
        {
            Invalidate();
        }

        public void SetScrollOffset(int value)
        {
            int max = Math.Max(0, _contentHeight - Height);
            int next = Math.Max(0, Math.Min(max, value));
            if (next == _scrollOffset) return;
            _scrollOffset = next;
            if (ScrollMetricsChanged != null) ScrollMetricsChanged(this, EventArgs.Empty);
            Invalidate();
        }

        public void ScrollBy(int delta)
        {
            SetScrollOffset(_scrollOffset - delta / 2);
        }

        private void Relayout()
        {
            if (ClientSize.Width <= 0) return;
            int available = ClientSize.Width;
            int columns = (available + Gap) / (MinCardWidth + Gap);
            if (columns < 1) columns = 1;
            if (columns > MaxColumns) columns = MaxColumns;
            int cardWidth = (available - Gap) / columns - Gap;
            if (cardWidth < 96) cardWidth = Math.Max(96, available - Gap);
            _columns = columns;
            _cardWidth = cardWidth;
            int rows = _tags.Count == 0 ? 0 : (_tags.Count + columns - 1) / columns;
            _contentHeight = rows == 0 ? 0 : rows * (CardHeight + Gap) - Gap + 2;
            int max = Math.Max(0, _contentHeight - Height);
            if (_scrollOffset > max) _scrollOffset = max;
            if (ScrollMetricsChanged != null) ScrollMetricsChanged(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
            Invalidate();
        }

        private Rectangle CardRect(int index)
        {
            int row = index / _columns;
            int column = index % _columns;
            return new Rectangle(
                column * (_cardWidth + Gap),
                row * (CardHeight + Gap) - _scrollOffset,
                _cardWidth,
                CardHeight);
        }

        private Rectangle StarRect(int index)
        {
            Rectangle card = CardRect(index);
            return new Rectangle(card.Right - 23, card.Y + 3, 21, 21);
        }

        private static PointF[] StarPoints(PointF center, float outer, float inner, int points)
        {
            PointF[] result = new PointF[points * 2];
            double step = Math.PI / points;
            for (int i = 0; i < result.Length; i++)
            {
                double angle = -Math.PI / 2 + i * step;
                float radius = (i % 2 == 0) ? outer : inner;
                result[i] = new PointF(
                    center.X + (float)Math.Cos(angle) * radius,
                    center.Y + (float)Math.Sin(angle) * radius);
            }
            return result;
        }

        private int IndexAt(Point point)
        {
            if (point.X < 0 || point.Y < 0) return -1;
            int stride = CardHeight + Gap;
            int row = (point.Y + _scrollOffset) / stride;
            int col = point.X / (_cardWidth + Gap);
            if (col < 0 || col >= _columns) return -1;
            int index = row * _columns + col;
            if (index < 0 || index >= _tags.Count) return -1;
            Rectangle rect = CardRect(index);
            return rect.Contains(point) ? index : -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Prepare(g);
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            if (_tags.Count == 0) return;
            int stride = CardHeight + Gap;
            int firstRow = Math.Max(0, _scrollOffset / stride);
            int lastRow = Math.Min((_tags.Count + _columns - 1) / _columns - 1,
                (_scrollOffset + Height) / stride);

            for (int row = firstRow; row <= lastRow; row++)
            {
                int y = row * stride - _scrollOffset;
                for (int col = 0; col < _columns; col++)
                {
                    int index = row * _columns + col;
                    if (index >= _tags.Count) break;
                    Rectangle rect = new Rectangle(col * (_cardWidth + Gap), y, _cardWidth, CardHeight);
                    if (rect.Bottom < 0 || rect.Top > Height) continue;
                    DrawCard(g, _tags[index], rect, index == _hoverIndex);
                }
            }
        }

        private void DrawCard(Graphics g, PromptTag tag, Rectangle rect, bool hover)
        {
            bool selected = _isSelected(tag);
            RectangleF rf = new RectangleF(rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1f, rect.Height - 1f);
            Color fill = selected ? Theme.CardSelected : (hover ? Theme.CardHover : Theme.Card);
            Color border = selected ? Theme.AccentBorder : (hover ? Theme.BorderHover : Theme.BorderSoft);
            Theme.FillRounded(g, rf, 8f, fill);
            Theme.DrawRounded(g, rf, 8f, border, 1f);

            int cy = rect.Y + rect.Height / 2;
            RectangleF circle = new RectangleF(rect.X + 8f, cy - 9f, 18f, 18f);
            Color circleFill = selected ? Theme.Accent
                : (_editMode ? Theme.AccentSoft : Color.FromArgb(241, 238, 248));
            Theme.FillRounded(g, circle, 9f, circleFill);

            if (_editMode)
            {
                TextRenderer.DrawText(g, "改", Fonts.Tiny, Rectangle.Round(circle), Theme.AccentLight,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else
            {
                using (Pen pen = new Pen(selected ? Color.White : Theme.TextMuted, 1.25f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    if (selected)
                    {
                        g.DrawLines(pen, new PointF[]
                        {
                            new PointF(rect.X + 14f, cy - 0.5f),
                            new PointF(rect.X + 17f, cy + 2.5f),
                            new PointF(rect.X + 23f, cy - 3.5f)
                        });
                    }
                    else
                    {
                        g.DrawLine(pen, rect.X + 19f, cy - 4f, rect.X + 19f, cy + 4f);
                        g.DrawLine(pen, rect.X + 15f, cy, rect.X + 23f, cy);
                    }
                }
            }

            int badgeWidth = 0;
            string badge = _showBadges && _badgeProvider != null ? _badgeProvider(tag) : string.Empty;
            if (!string.IsNullOrEmpty(badge))
            {
                badgeWidth = Math.Max(30, Theme.Measure(badge, Fonts.Tiny) + 10);
                RectangleF badgeRect = new RectangleF(rect.Right - badgeWidth - 6, rect.Y + 36, badgeWidth, 14);
                bool unassigned = badge == "未归类";
                Theme.FillRounded(g, badgeRect, 7f,
                    unassigned ? Color.FromArgb(246, 244, 250) : Theme.AccentSoft);
                Theme.DrawRounded(g, badgeRect, 7f,
                    unassigned ? Theme.BorderSoft : Theme.AccentBorder, 1f);
                TextRenderer.DrawText(g, badge, Fonts.Tiny, Rectangle.Round(badgeRect),
                    unassigned ? Theme.TextMuted : Theme.AccentLight,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            bool favorite = _isFavorite(tag);
            PointF starCenter = new PointF(rect.Right - 12f, rect.Y + 12f);
            PointF[] star = StarPoints(starCenter, 5.6f, 2.4f, 5);
            if (favorite)
            {
                using (SolidBrush starBrush = new SolidBrush(Theme.Accent))
                {
                    g.FillPolygon(starBrush, star);
                }
            }
            else
            {
                using (Pen starPen = new Pen(hover ? Theme.AccentLight : Theme.TextMuted, 1.2f))
                {
                    starPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    g.DrawPolygon(starPen, star);
                }
            }

            int textX = rect.X + 31;
            int titleWidth = rect.Width - 51;
            Font titleFont = Fonts.CardTitle;
            if (Theme.Measure(tag.Label, titleFont) > titleWidth)
                titleFont = Fonts.CardTitleSmall;
            TextRenderer.DrawText(g, tag.Label, titleFont,
                new Rectangle(textX, rect.Y + 8, titleWidth, 21),
                selected ? Theme.AccentLight : Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            int metaWidth = rect.Width - 38 - (badgeWidth > 0 ? badgeWidth + 4 : 0);
            if (metaWidth < 30) metaWidth = rect.Width - 42;
            TextRenderer.DrawText(g, tag.English, Fonts.CardMeta,
                new Rectangle(textX, rect.Y + 29, metaWidth, 16),
                selected ? Color.FromArgb(116, 94, 174) : Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int index = IndexAt(e.Location);
            if (index < 0) return;
            if (StarRect(index).Contains(e.Location))
            {
                if (FavoriteClicked != null) FavoriteClicked(_tags[index]);
                return;
            }
            if (TagClicked != null) TagClicked(_tags[index]);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollBy(e.Delta);
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null) handled.Handled = true;
        }
    }

    internal class TagBrowser : RoundedPanel
    {
        private readonly TagGrid _grid;
        private readonly EmptyState _empty;
        private readonly ModernButton _addTag;
        private readonly ScrollOverlay _scrollOverlay;
        private List<PromptTag> _allTags = new List<PromptTag>();
        private List<PromptTag> _filtered = new List<PromptTag>();
        private string _query = string.Empty;
        private string _title = "全部标签";
        private string _description = "所有分类中的标签都会出现在这里";
        private string _emptyTitle = string.Empty;
        private string _emptyDescription = string.Empty;
        private bool _showCategoryBadges;
        private bool _editMode;
        private Predicate<PromptTag> _isSelected = delegate { return false; };
        private Predicate<PromptTag> _isFavorite = delegate { return false; };
        private Func<PromptTag, string> _badgeProvider;

        public TagBrowser()
        {
            Radius = 14;
            FillColor = Theme.TagArea;
            BorderColor = Theme.Border;
            BackColor = Theme.Bg;

            _grid = new TagGrid();
            _grid.TagClicked += delegate(PromptTag tag)
            {
                if (TagClicked != null) TagClicked(tag);
            };
            _grid.FavoriteClicked += delegate(PromptTag tag)
            {
                if (FavoriteClicked != null) FavoriteClicked(tag);
            };
            _grid.ScrollMetricsChanged += delegate { UpdateScrollMetrics(); };

            _empty = new EmptyState();
            _empty.BackColor = Theme.TagArea;

            _addTag = new ModernButton();
            _addTag.Text = "＋ 新建标签";
            _addTag.Variant = ButtonVariant.Secondary;
            _addTag.BackColor = Theme.TagArea;
            _addTag.Size = new Size(112, 30);
            _addTag.Visible = false;
            _addTag.Click += delegate
            {
                if (AddTagRequested != null) AddTagRequested(this, EventArgs.Empty);
            };

            _scrollOverlay = new ScrollOverlay(null);
            _scrollOverlay.BackColor = Theme.TagArea;
            _scrollOverlay.ValueChanged += delegate(int value) { _grid.SetScrollOffset(value); };

            Controls.Add(_grid);
            Controls.Add(_empty);
            Controls.Add(_addTag);
            Controls.Add(_scrollOverlay);
            _scrollOverlay.BringToFront();
        }

        public event Action<PromptTag> TagClicked;
        public event Action<PromptTag> FavoriteClicked;
        public event EventHandler AddTagRequested;

        public Predicate<PromptTag> IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value ?? delegate { return false; };
                _grid.SetSelection(_isSelected);
            }
        }

        public Predicate<PromptTag> IsFavorite
        {
            get { return _isFavorite; }
            set
            {
                _isFavorite = value ?? delegate { return false; };
                _grid.SetFavoriteProvider(_isFavorite);
            }
        }

        public Func<PromptTag, string> BadgeProvider
        {
            get { return _badgeProvider; }
            set
            {
                _badgeProvider = value;
                _grid.SetBadgeProvider(_badgeProvider, _showCategoryBadges);
            }
        }

        public bool EditMode
        {
            get { return _editMode; }
            set
            {
                if (_editMode == value) return;
                _editMode = value;
                _addTag.Visible = _editMode;
                _grid.SetEditMode(_editMode);
                if (_allTags.Count == 0 && _query.Length == 0)
                {
                    _empty.SetMessage("这里还没有标签",
                        _editMode ? "点上方「＋ 新建标签」添加第一个标签" : "切换到编辑模式后添加小分类和标签");
                }
                Invalidate();
            }
        }

        public int VisibleCount
        {
            get { return _filtered.Count; }
        }

        public void SetView(string title, string description, List<PromptTag> tags, bool showCategoryBadges)
        {
            _title = title ?? string.Empty;
            _description = description ?? string.Empty;
            _allTags = tags ?? new List<PromptTag>();
            _showCategoryBadges = showCategoryBadges;
            _query = string.Empty;
            ApplyFilter();
            _grid.SetScrollOffset(0);
        }

        public void SetEmptyMessage(string title, string description)
        {
            _emptyTitle = title ?? string.Empty;
            _emptyDescription = description ?? string.Empty;
            if (_allTags.Count == 0 && _query.Length == 0 && _grid != null)
            {
                if (string.IsNullOrEmpty(_emptyTitle))
                {
                    _empty.SetMessage("这里还没有标签",
                        _editMode ? "点上方「＋ 新建标签」添加第一个标签" : "切换到编辑模式后添加小分类和标签");
                }
                else
                {
                    _empty.SetMessage(_emptyTitle, _emptyDescription);
                }
                _empty.Visible = true;
                _empty.BringToFront();
            }
        }

        public void SetFilter(string query)
        {
            string value = (query ?? string.Empty).Trim();
            if (string.Equals(value, _query, StringComparison.Ordinal)) return;
            _query = value;
            ApplyFilter();
            _grid.SetScrollOffset(0);
        }

        public void RefreshSelection()
        {
            _grid.RefreshSelection();
        }

        public void RefreshBadges()
        {
            _grid.SetBadgeProvider(_badgeProvider, _showCategoryBadges);
        }

        public void ScrollBy(int delta)
        {
            if (_scrollOverlay.Visible) _scrollOverlay.ScrollBy(delta);
        }

        private void ApplyFilter()
        {
            _filtered = new List<PromptTag>();
            for (int i = 0; i < _allTags.Count; i++)
            {
                if (_allTags[i].Matches(_query)) _filtered.Add(_allTags[i]);
            }
            if (_allTags.Count == 0 && _query.Length == 0)
            {
                if (!string.IsNullOrEmpty(_emptyTitle))
                {
                    _empty.SetMessage(_emptyTitle, _emptyDescription);
                }
                else
                {
                    _empty.SetMessage("这里还没有标签",
                        _editMode ? "点上方「＋ 新建标签」添加第一个标签" : "切换到编辑模式后添加小分类和标签");
                }
            }
            else
            {
                _empty.SetMessage("没有找到相关提示词", "试试其他关键词，或切换一个分类");
            }
            _grid.SetBadgeProvider(_badgeProvider, _showCategoryBadges);
            _grid.SetTags(_filtered);
            _grid.SetSelection(_isSelected);
            _grid.SetEditMode(_editMode);
            _empty.Visible = _filtered.Count == 0;
            if (_empty.Visible) _empty.BringToFront();
            UpdateScrollMetrics();
            Invalidate();
        }

        private void UpdateScrollMetrics()
        {
            if (_grid == null || _scrollOverlay == null) return;
            int max = Math.Max(0, _grid.ContentHeight - _grid.Height);
            _scrollOverlay.SetMetrics(max, _grid.Height, _grid.ScrollOffset);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChrome();
        }

        private void LayoutChrome()
        {
            if (_grid == null) return;
            int flowX = 16;
            int flowY = 58;
            int scrollWidth = SystemInformation.VerticalScrollBarWidth;
            int flowW = Width - flowX - 14 - scrollWidth;
            int flowH = Height - flowY - 12;
            if (flowW < 0) flowW = 0;
            if (flowH < 0) flowH = 0;
            _grid.SetBounds(flowX, flowY, flowW, flowH);
            _empty.SetBounds(flowX, flowY, Math.Max(0, flowW + scrollWidth), flowH);
            _addTag.SetBounds(Width - 130, 14, 112, 30);
            _scrollOverlay.SetBounds(flowX + flowW + 2, flowY, scrollWidth, flowH);
            _scrollOverlay.BringToFront();
            UpdateScrollMetrics();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme.Prepare(g);

            TextRenderer.DrawText(g, _title, Fonts.Section,
                new Rectangle(22, 9, 280, 28), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);
            int titleWidth = Math.Min(280, Theme.Measure(_title, Fonts.Section));

            string countText = _query.Length == 0
                ? _allTags.Count + " 个标签"
                : _filtered.Count + " / " + _allTags.Count + " 个标签";
            TextRenderer.DrawText(g, countText, Fonts.Small,
                new Rectangle(22 + titleWidth + 11, 13, 200, 22), Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            TextRenderer.DrawText(g, _description, Fonts.Small,
                new Rectangle(23, 34, Width - (_editMode ? 180 : 350), 18), Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis);

            if (!_editMode)
            {
                TextRenderer.DrawText(g, "点击加入  ·  再次点击移除", Fonts.Small,
                    new Rectangle(Width - 280, 9, 258, 28), Theme.TextMuted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            using (Pen line = new Pen(Theme.BorderSoft, 1f))
            {
                g.DrawLine(line, 20, 55, Width - 20, 55);
            }
        }
    }
}



