using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace hahahalib
{
    namespace ui
    {

        /// <summary>
        /// 自訂 TabControl（Top/Bottom）：
        /// - 圓角、Hover/Selected、可切換：關閉×/外框線
        /// - 右上「＋」新增分頁
        /// - 修正 FlatButtons 文字被裁切
        /// - 隱藏內建捲動箭頭(TabControlUpDownButton)
        /// - 關掉系統灰色外框（WndProc/TCM_ADJUSTRECT）
        /// </summary>
        public class ModernTabControl : TabControl
        {
            // ===== 外觀設定 =====
            private int _cornerRadius = 10;
            private Padding _tabPadding = new Padding(12, 4, 22, 4); // 右側預留關閉鈕
            private int _closeSize = 12;

            private Color _tabBackColor = Color.FromArgb(245, 246, 248);
            private Color _tabHoverColor = Color.FromArgb(235, 238, 242);
            private Color _tabSelectedColor = Color.White;
            private Color _borderColor = Color.FromArgb(210, 214, 220);
            private Color _selectedBorderColor = Color.FromArgb(60, 120, 255);
            private Color _closeHoverBack = Color.FromArgb(230, 80, 80);
            private Color _closeHoverFore = Color.White;

            // Add（＋）按鈕
            private bool _showAddButton = true;
            private int _addButtonSize = 18;
            private int _addButtonMargin = 8;
            private Color _addButtonBack = Color.FromArgb(246, 248, 251);
            private Color _addButtonHoverBack = Color.FromArgb(224, 236, 255);
            private Color _addButtonFore = Color.FromArgb(60, 120, 255);
            private Rectangle _addRect = Rectangle.Empty;
            private bool _hoverAdd = false;
            private int _newCounter = 1;

            // 互動狀態
            private int _hoverIndex = -1;
            private int _hoverCloseIndex = -1;

            // DPI
            private float DpiScale => DeviceDpi / 96f;
            private int ScaleI(int v) => (int)Math.Round(v * DpiScale);

            // ===== 可控屬性 =====
            [Category("Behavior"), Description("是否顯示每個分頁的關閉（×）按鈕")]
            public bool ShowCloseButtons { get; set; } = false;

            [Category("Appearance"), Description("是否繪製分頁外框線")]
            public bool ShowTabBorder { get; set; } = false;

            // ===== 事件 / 工廠 =====
            public class TabClosingEventArgs : CancelEventArgs
            {
                public TabPage Page { get; }
                public int Index { get; }
                public TabClosingEventArgs(TabPage page, int index) { Page = page; Index = index; }
            }
            public class TabClosedEventArgs : EventArgs
            {
                public TabPage Page { get; }
                public int Index { get; }
                public TabClosedEventArgs(TabPage page, int index) { Page = page; Index = index; }
            }
            public class AddTabRequestedEventArgs : EventArgs
            {
                public bool Cancel { get; set; }
                public TabPage NewPage { get; set; }
            }

            [Category("Action")] public event EventHandler<TabClosingEventArgs> TabClosing;
            [Category("Action")] public event EventHandler<TabClosedEventArgs> TabClosed;
            [Category("Action")] public event EventHandler<AddTabRequestedEventArgs> AddTabRequested;
            [Browsable(false)] public Func<TabPage> TabFactory { get; set; }

            public ModernTabControl()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

                DrawMode = TabDrawMode.OwnerDrawFixed;
                SizeMode = TabSizeMode.Fixed;
                ItemSize = new Size(140, 42);     // 高一點，避免切字（原 36 有風險）
                Alignment = TabAlignment.Top;
                Appearance = TabAppearance.Normal;  // ← 改成 Normal，避免「按下的眶」
                Padding = new Point(12, 4);
                Multiline = false;

                if (Font == DefaultFont) Font = new Font("Segoe UI", 9f);
                EnsureTabStripVisible();
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                EnsureTabStripVisible();
                HideUpDownButton();
            }

            protected override void OnCreateControl()
            {
                base.OnCreateControl();
                HideUpDownButton();
            }

            // ===== 其他公開屬性 =====
            [Category("Appearance")]
            public int CornerRadius { get => _cornerRadius; set { _cornerRadius = Math.Max(0, value); Invalidate(); } }
            [Category("Appearance")]
            public Padding TabInnerPadding { get => _tabPadding; set { _tabPadding = value; Invalidate(); } }
            [Category("Appearance")]
            public int CloseButtonSize { get => _closeSize; set { _closeSize = Math.Max(10, value); Invalidate(); } }
            [Category("Appearance")]
            public Color TabBackColor { get => _tabBackColor; set { _tabBackColor = value; Invalidate(); } }
            [Category("Appearance")]
            public Color TabHoverColor { get => _tabHoverColor; set { _tabHoverColor = value; Invalidate(); } }
            [Category("Appearance")]
            public Color TabSelectedColor { get => _tabSelectedColor; set { _tabSelectedColor = value; Invalidate(); } }
            [Category("Appearance")]
            public Color BorderColor { get => _borderColor; set { _borderColor = value; Invalidate(); } }
            [Category("Appearance")]
            public Color SelectedBorderColor { get => _selectedBorderColor; set { _selectedBorderColor = value; Invalidate(); } }

            [Category("Behavior")]
            public bool ShowAddButton { get => _showAddButton; set { _showAddButton = value; Invalidate(); } }
            [Category("Appearance")]
            public int AddButtonSize { get => _addButtonSize; set { _addButtonSize = Math.Max(12, value); Invalidate(); } }
            [Category("Appearance")]
            public int AddButtonMargin { get => _addButtonMargin; set { _addButtonMargin = Math.Max(0, value); Invalidate(); } }
            [Category("Appearance")]
            public Color AddButtonBack { get => _addButtonBack; set { _addButtonBack = value; Invalidate(); } }
            [Category("Appearance")]
            public Color AddButtonHoverBack { get => _addButtonHoverBack; set { _addButtonHoverBack = value; Invalidate(); } }
            [Category("Appearance")]
            public Color AddButtonFore { get => _addButtonFore; set { _addButtonFore = value; Invalidate(); } }

            // ===== 繪製分頁鈕 =====
            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                try
                {
                    if (TabCount == 0) return;

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    int index = e.Index;
                    var page = TabPages[index];
                    var rect = GetTabRect(index);

                    // 這裡只微縮，不要上下移位，避免切字
                    rect.Inflate(-2, -2);

                    bool selected = (SelectedIndex == index);
                    bool hover = (_hoverIndex == index);

                    Color back = selected ? _tabSelectedColor : (hover ? _tabHoverColor : _tabBackColor);
                    Color border = selected ? _selectedBorderColor : _borderColor;

                    var inset = Rectangle.Inflate(rect, -ScaleI(4), -ScaleI(4));

                    using (var path = RoundRect(inset, ScaleI(_cornerRadius)))
                    using (var bg = new SolidBrush(back))
                    {
                        e.Graphics.FillPath(bg, path);
                        if (ShowTabBorder)
                            using (var pen = new Pen(border)) e.Graphics.DrawPath(pen, path);
                    }

                    // 內容區：上下各留 2px 緩衝（避免字底被吃）
                    var content = Rectangle.Inflate(inset, -ScaleI(_tabPadding.Left), -ScaleI(_tabPadding.Top));
                    content.Width -= ScaleI(_tabPadding.Right);
                    content.Height -= ScaleI(_tabPadding.Bottom);
                    content.Y += 1;                         // 微調 1px
                    content.Height = Math.Max(0, content.Height - 2);

                    // 關閉鈕（可關）
                    Rectangle closeRc = Rectangle.Empty;
                    if (ShowCloseButtons)
                    {
                        int s = ScaleI(_closeSize);
                        closeRc = new Rectangle(inset.Right - ScaleI(8) - s, inset.Top + (inset.Height - s) / 2, s, s);
                        content.Width -= (s + ScaleI(10));
                    }

                    // 圖示
                    int iconGap = 0;
                    if (ImageList != null)
                    {
                        int imgIndex = page.ImageIndex;
                        if (imgIndex >= 0 && imgIndex < ImageList.Images.Count)
                        {
                            var img = ImageList.Images[imgIndex];
                            int iy = content.Top + (content.Height - img.Height) / 2;
                            e.Graphics.DrawImage(img, content.Left, iy, img.Width, img.Height);
                            iconGap = img.Width + ScaleI(6);
                        }
                    }

                    // 文字：不畫任何 Focus/按下框線
                    var textRc = new Rectangle(content.Left + iconGap, content.Top, content.Width - iconGap, content.Height);
                    var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
                                 TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;   // ← 避免多餘邊距/底線
                    TextRenderer.DrawText(e.Graphics, page.Text, this.Font, textRc, this.ForeColor, flags);

                    if (ShowCloseButtons)
                    {
                        bool onClose = (_hoverCloseIndex == index);
                        DrawCloseGlyph(e.Graphics, closeRc, onClose);
                    }
                }
                catch
                {
                    var r = GetTabRect(e.Index);
                    using (var pen = new Pen(Color.DarkGray)) e.Graphics.DrawRectangle(pen, r);
                }

                base.OnDrawItem(e);
            }

            // ===== 畫 Add（＋）按鈕 =====
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (!_showAddButton) return;

                _addRect = GetAddButtonRect();
                if (_addRect.Width <= 0 || _addRect.Height <= 0) return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var bg = new SolidBrush(_hoverAdd ? _addButtonHoverBack : _addButtonBack))
                using (var pen = new Pen(_addButtonFore, Math.Max(1.5f, _addRect.Width / 10f)))
                {
                    var r = Rectangle.Inflate(_addRect, -1, -1);
                    e.Graphics.FillEllipse(bg, r);

                    int pad = Math.Max(2, r.Width / 4);
                    int cx = r.Left + r.Width / 2;
                    int cy = r.Top + r.Height / 2;
                    e.Graphics.DrawLine(pen, new Point(cx - pad, cy), new Point(cx + pad, cy));
                    e.Graphics.DrawLine(pen, new Point(cx, cy - pad), new Point(cx, cy + pad));
                }
            }

            // ===== 滑鼠互動 =====
            protected override void OnMouseMove(MouseEventArgs e)
            {
                int oldHover = _hoverIndex;
                int oldClose = _hoverCloseIndex;
                bool oldAdd = _hoverAdd;

                _hoverIndex = HitTestTab(e.Location);
                _hoverCloseIndex = -1;
                _hoverAdd = _showAddButton && GetAddButtonRect().Contains(e.Location);

                if (_hoverIndex >= 0 && ShowCloseButtons)
                {
                    var rc = GetCloseRect(_hoverIndex);
                    if (rc.Contains(e.Location))
                        _hoverCloseIndex = _hoverIndex;
                }

                if (oldHover != _hoverIndex || oldClose != _hoverCloseIndex || oldAdd != _hoverAdd)
                    Invalidate();

                base.OnMouseMove(e);
            }
            protected override void OnMouseLeave(EventArgs e)
            {
                _hoverIndex = -1;
                _hoverCloseIndex = -1;
                _hoverAdd = false;
                Invalidate();
                base.OnMouseLeave(e);
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    // 先檢查 Add（＋）
                    if (_showAddButton && GetAddButtonRect().Contains(e.Location))
                    {
                        CreateNewTab();
                        return;
                    }

                    int idx = HitTestTab(e.Location);
                    if (idx >= 0)
                    {
                        if (ShowCloseButtons)
                        {
                            var rc = GetCloseRect(idx);
                            if (rc.Contains(e.Location))
                            {
                                var page = TabPages[idx];
                                var closing = new TabClosingEventArgs(page, idx);
                                TabClosing?.Invoke(this, closing);
                                if (!closing.Cancel)
                                {
                                    TabPages.RemoveAt(idx);
                                    TabClosed?.Invoke(this, new TabClosedEventArgs(page, idx));
                                    page.Dispose();
                                }
                                return;
                            }
                        }

                        SelectedIndex = idx;
                    }
                }

                // 中鍵關閉（僅當顯示關閉鈕時）
                if (e.Button == MouseButtons.Middle && ShowCloseButtons)
                {
                    int idx = HitTestTab(e.Location);
                    if (idx >= 0)
                    {
                        var page = TabPages[idx];
                        var closing = new TabClosingEventArgs(page, idx);
                        TabClosing?.Invoke(this, closing);
                        if (!closing.Cancel)
                        {
                            TabPages.RemoveAt(idx);
                            TabClosed?.Invoke(this, new TabClosedEventArgs(page, idx));
                            page.Dispose();
                        }
                    }
                    return;
                }

                base.OnMouseDown(e);
            }

            protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); HideUpDownButton(); }
            protected override void OnControlAdded(ControlEventArgs e) { base.OnControlAdded(e); Invalidate(); HideUpDownButton(); }
            protected override void OnControlRemoved(ControlEventArgs e) { base.OnControlRemoved(e); Invalidate(); HideUpDownButton(); }

            // ===== 背景：整體刷底，再刷內容區，避免 TabStrip 發黑 =====
            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                using (var b = new SolidBrush(Parent?.BackColor ?? SystemColors.Control))
                    pevent.Graphics.FillRectangle(b, this.ClientRectangle);

                var pageArea = this.DisplayRectangle; // 內容區（不含標籤列）
                using (var b = new SolidBrush(SystemColors.Window))
                    pevent.Graphics.FillRectangle(b, pageArea);
            }

            // ===== 幫手 =====
            private int HitTestTab(Point p)
            {
                for (int i = 0; i < TabCount; i++)
                    if (GetTabRect(i).Contains(p)) return i;
                return -1;
            }

            private Rectangle GetCloseRect(int index)
            {
                var rect = GetTabRect(index);
                var inset = Rectangle.Inflate(rect, -ScaleI(4), -ScaleI(4));
                int s = ScaleI(_closeSize);
                return new Rectangle(inset.Right - ScaleI(8) - s, inset.Top + (inset.Height - s) / 2, s, s);
            }

            private Rectangle GetAddButtonRect()
            {
                if (!_showAddButton) return Rectangle.Empty;
                var strip = GetTabStripBounds();
                if (strip.Width <= 0 || strip.Height <= 0) return Rectangle.Empty;

                int s = ScaleI(_addButtonSize);
                int m = ScaleI(_addButtonMargin);

                var rc = new Rectangle(strip.Right - m - s, strip.Top + (strip.Height - s) / 2, s, s);

                if (TabCount > 0)
                {
                    var last = GetTabRect(TabCount - 1);
                    if (rc.Left < last.Right + ScaleI(6))
                        rc.X = Math.Max(ScaleI(4), last.Right + ScaleI(6));
                }
                return rc;
            }

            private Rectangle GetTabStripBounds()
            {
                var pageArea = this.DisplayRectangle; // 內容區（已扣掉分頁列）
                if (Alignment == TabAlignment.Top)
                    return new Rectangle(0, 0, Width, Math.Max(0, pageArea.Top));
                if (Alignment == TabAlignment.Bottom)
                    return new Rectangle(0, pageArea.Bottom, Width, Math.Max(0, Height - pageArea.Bottom));
                return Rectangle.Empty;
            }

            private static GraphicsPath RoundRect(Rectangle r, int d)
            {
                var path = new GraphicsPath();
                if (d <= 0) { path.AddRectangle(r); path.CloseFigure(); return path; }
                int a = d * 2;
                path.AddArc(r.Left, r.Top, a, a, 180, 90);
                path.AddArc(r.Right - a, r.Top, a, a, 270, 90);
                path.AddArc(r.Right - a, r.Bottom - a, a, a, 0, 90);
                path.AddArc(r.Left, r.Bottom - a, a, a, 90, 90);
                path.CloseFigure();
                return path;
            }

            private void DrawCloseGlyph(Graphics g, Rectangle rc, bool hover)
            {
                var state = g.Save();
                try
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    if (hover)
                    {
                        using (var bg = new SolidBrush(_closeHoverBack))
                            g.FillEllipse(bg, rc);
                    }

                    int pad = Math.Max(2, rc.Width / 4);
                    var p1 = new Point(rc.Left + pad, rc.Top + pad);
                    var p2 = new Point(rc.Right - pad, rc.Bottom - pad);
                    var p3 = new Point(rc.Left + pad, rc.Bottom - pad);
                    var p4 = new Point(rc.Right - pad, rc.Top + pad);

                    using (var pen = new Pen(hover ? _closeHoverFore : Color.DimGray, Math.Max(1.5f, rc.Width / 10f)))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawLine(pen, p1, p2);
                        g.DrawLine(pen, p3, p4);
                    }
                }
                finally { g.Restore(state); }
            }

            private void CreateNewTab()
            {
                var args = new AddTabRequestedEventArgs();
                AddTabRequested?.Invoke(this, args);
                if (args.Cancel) return;

                TabPage page = args.NewPage;
                if (page == null && TabFactory != null)
                    page = TabFactory();

                if (page == null)
                {
                    page = new TabPage($"新分頁 {_newCounter++}");
                    page.Controls.Add(new Label { Text = "這是一個新分頁", AutoSize = true, Location = new Point(12, 12) });
                }

                TabPages.Add(page);
                SelectedTab = page;
            }

            private void EnsureTabStripVisible()
            {
                if (DrawMode != TabDrawMode.OwnerDrawFixed) DrawMode = TabDrawMode.OwnerDrawFixed;
                if (SizeMode != TabSizeMode.Fixed) SizeMode = TabSizeMode.Fixed;

                Multiline = false;

                if (ItemSize.Height < 22)
                    ItemSize = new Size(Math.Max(1, ItemSize.Width), 28);

                if (Alignment == TabAlignment.Left || Alignment == TabAlignment.Right)
                    Alignment = TabAlignment.Top;
            }

            // ===== 隱藏 TabControl 內建捲動箭頭 =====
            private void HideUpDownButton()
            {
                foreach (Control c in this.Controls)
                {
                    var n = c?.GetType().Name;
                    if (n == "UpDownBase" || n == "TabControlUpDownButton")
                    {
                        c.Visible = false;
                        c.Width = 0;
                    }
                }
            }

            // ===== 關掉系統灰外框：攔 TCM_ADJUSTRECT，把內容區外擴吃掉外框 =====
            private const int TCM_ADJUSTRECT = 0x1328;

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT { public int Left, Top, Right, Bottom; }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == TCM_ADJUSTRECT && !DesignMode)
                {
                    base.WndProc(ref m); // 先讓系統計算
                    try
                    {
                        var rc = Marshal.PtrToStructure<RECT>(m.LParam);
                        rc.Left -= 2; rc.Top -= 2;
                        rc.Right += 2; rc.Bottom += 2;
                        Marshal.StructureToPtr(rc, m.LParam, true);
                    }
                    catch { }
                    return;
                }
                base.WndProc(ref m);
            }
        }







    }
}


       