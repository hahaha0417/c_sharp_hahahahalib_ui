using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Svg;
using Timer = System.Windows.Forms.Timer;

namespace hahahalib
{
    namespace ui
    {
        // 與 WinForms 一致的圖文關係
        public enum TextImageRelation
        {
            ImageBeforeText,   // 圖左文右
            TextBeforeImage,   // 文左圖右
            ImageAboveText,    // 圖上文下
            TextAboveImage     // 文字上圖在下
        }

        [DefaultEvent("Click")]
        public class RoundIconButton : Control, IButtonControl
        {
            // ---------- 狀態 ----------
            private bool _hovered;
            private bool _pressed;
            private bool _isDefaultButton;

            // ---------- 外觀 ----------
            private int _cornerRadius = 12;
            private int _borderThickness = 1;
            private Color _borderColor = Color.Silver;
            private Color _hoverBorderColor = Color.DodgerBlue;
            private Color _pressedBorderColor = Color.RoyalBlue;

            private Color _backNormal = Color.White;
            private Color _backHover = Color.FromArgb(250, 250, 255);
            private Color _backPressed = Color.FromArgb(235, 240, 255);

            private int _focusInset = 2;

            // ---------- 投影 ----------
            private bool _shadowEnabled = true;
            private Color _shadowColor = Color.FromArgb(90, Color.Black);
            private Point _shadowOffset = new Point(0, 3);
            private int _shadowSoftness = 6; // 1~20

            // ---------- 內容 & 排版 ----------
            private Size _imageSize = new Size(18, 18);
            private int _textImageSpacing = 6;
            private ContentAlignment _imageAlign = ContentAlignment.MiddleLeft;   // 單一內容時用
            private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;  // 單一內容時用
            private TextImageRelation _relation = TextImageRelation.ImageBeforeText;

            // Bitmap
            private Image _image;

            // SVG
            private SvgDocument _svgDoc;
            private string _svgPath;
            private bool _useSvg = true;
            private bool _autoScaleSvgByDpi = true;

            // 覆蓋色（破壞性）
            private Color _svgOverrideFill = Color.Empty;
            private Color _svgOverrideStroke = Color.Empty;
            private bool _svgRecolorPending = false;

            // SVG Bitmap 快取
            private Bitmap _svgCache;
            private float _svgCacheDpiX = 0f, _svgCacheDpiY = 0f;
            private Size _svgCacheLogicalSize = Size.Empty;

            // ---------- Icon Tint（位圖/已轉成位圖後用） ----------
            private Color _iconNormalColor = Color.Empty;
            private Color _iconHoverColor = Color.Empty;
            private Color _iconPressedColor = Color.Empty;
            private Color _iconDisabledColor = Color.Gray;

            // ---------- Loading ----------
            private bool _isLoading = false;
            private bool _disableWhileLoading = true;
            private bool _overlayWhileLoading = true;
            private int _overlayOpacity = 90; // 0~255
            private Color _spinnerColor = Color.DodgerBlue;
            private int _spinnerThickness = 3;
            private int _spinnerRadius = 10;
            private int _spinnerSpeed = 180; // deg/s
            private float _spinnerAngle = 0f;
            private readonly Timer _animTimer;

            // ---------- 焦點框（方法二：可關閉） ----------
            private bool _showFocusOutline = true;

            // ---------- IButtonControl ----------
            private DialogResult _dialogResult = DialogResult.None;

            public RoundIconButton()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.SupportsTransparentBackColor, true);

                BackColor = Color.Transparent; // 需要
                ForeColor = Color.Black;
                Font = new Font("Segoe UI", 9f);
                Size = new Size(120, 36);
                Padding = new Padding(12, 8, 12, 8);
                TabStop = true;

                // 滑鼠
                MouseEnter += (s, e) => { _hovered = true; Invalidate(); };
                MouseLeave += (s, e) => { _hovered = false; _pressed = false; Invalidate(); };
                MouseDown += (s, e) => { if (e.Button == MouseButtons.Left && CanInteract()) { _pressed = true; Focus(); Invalidate(); } };
                MouseUp += (s, e) => { if (_pressed && e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); if (ClientRectangle.Contains(e.Location) && CanInteract()) PerformClick(); } };

                // 鍵盤
                KeyDown += (s, e) => { if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && CanInteract()) { _pressed = true; Invalidate(); } };
                KeyUp += (s, e) => { if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && CanInteract()) { _pressed = false; Invalidate(); PerformClick(); } };

                // Loading 動畫
                _animTimer = new Timer { Interval = 16 }; // ~60FPS
                _animTimer.Tick += (s, e) =>
                {
                    _spinnerAngle = (_spinnerAngle + _spinnerSpeed * (_animTimer.Interval / 1000f)) % 360f;
                    Invalidate();
                };

                UpdateRegion(); // 啟動即建立真圓角可見區
            }

            // ---------- 真透明：畫父背景而非方形底 ----------
            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                if (Parent == null) return; // 不呼叫 base，避免方形底
                var g = pevent.Graphics;
                var state = g.Save();
                try
                {
                    // 只畫父容器背景，避免把父前景/子控件一起畫造成閃爍
                    g.TranslateTransform(-Left, -Top);
                    using var eBg = new PaintEventArgs(g, Parent.ClientRectangle);
                    InvokePaintBackground(Parent, eBg);
                }
                finally { g.Restore(state); }
            }

            // ---------- 外觀屬性 ----------
            [Category("Appearance"), DefaultValue(12)]
            public int CornerRadius
            {
                get => _cornerRadius;
                set { _cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
            }

            [Category("Appearance"), DefaultValue(1)]
            public int BorderThickness { get => _borderThickness; set { _borderThickness = Math.Max(0, value); Invalidate(); } }

            [Category("Appearance")]
            public Color BorderColor { get => _borderColor; set { _borderColor = value; Invalidate(); } }

            [Category("Appearance")]
            public Color HoverBorderColor { get => _hoverBorderColor; set { _hoverBorderColor = value; Invalidate(); } }

            [Category("Appearance")]
            public Color PressedBorderColor { get => _pressedBorderColor; set { _pressedBorderColor = value; Invalidate(); } }

            [Category("Appearance")]
            public Color BackNormal { get => _backNormal; set { _backNormal = value; Invalidate(); } }

            [Category("Appearance")]
            public Color BackHover { get => _backHover; set { _backHover = value; Invalidate(); } }

            [Category("Appearance")]
            public Color BackPressed { get => _backPressed; set { _backPressed = value; Invalidate(); } }

            [Category("Appearance")]
            public new Padding Padding { get => base.Padding; set { base.Padding = value; Invalidate(); } }

            // ---------- 投影 ----------
            [Category("Shadow"), DefaultValue(true)]
            public bool ShadowEnabled { get => _shadowEnabled; set { _shadowEnabled = value; Invalidate(); } }

            [Category("Shadow")]
            public Color ShadowColor { get => _shadowColor; set { _shadowColor = value; Invalidate(); } }

            [Category("Shadow")]
            public Point ShadowOffset { get => _shadowOffset; set { _shadowOffset = value; Invalidate(); } }

            [Category("Shadow"), DefaultValue(6)]
            public int ShadowSoftness { get => _shadowSoftness; set { _shadowSoftness = Math.Max(0, Math.Min(20, value)); Invalidate(); } }

            // ---------- 焦點框（方法二） ----------
            [Category("Behavior"), DefaultValue(true)]
            public bool ShowFocusOutline
            {
                get => _showFocusOutline;
                set { _showFocusOutline = value; Invalidate(); }
            }

            // ---------- 內容 & 排版 ----------
            [Category("Content")]
            public Image Image { get => _image; set { _useSvg = false; _image = value; Invalidate(); } }

            [Category("Content"), DefaultValue(typeof(Size), "18,18")]
            public Size ImageSize
            {
                get => _imageSize;
                set { _imageSize = new Size(Math.Max(0, value.Width), Math.Max(0, value.Height)); InvalidateSvgCache(); Invalidate(); }
            }

            [Category("Content"), DefaultValue(typeof(ContentAlignment), "MiddleLeft")]
            public ContentAlignment ImageAlign { get => _imageAlign; set { _imageAlign = value; Invalidate(); } }

            [Category("Content"), DefaultValue(typeof(ContentAlignment), "MiddleCenter")]
            public ContentAlignment TextAlign { get => _textAlign; set { _textAlign = value; Invalidate(); } }

            [Category("Content"), DefaultValue(6)]
            public int TextImageSpacing { get => _textImageSpacing; set { _textImageSpacing = Math.Max(0, value); Invalidate(); } }

            [Category("Content"), DefaultValue(TextImageRelation.ImageBeforeText)]
            public TextImageRelation TextImageRelation { get => _relation; set { _relation = value; Invalidate(); } }

            // ---------- SVG ----------
            [Category("SVG"), DefaultValue(true)]
            public bool UseSvg { get => _useSvg; set { _useSvg = value; Invalidate(); } }

            [Category("SVG"), DefaultValue(true)]
            public bool AutoScaleSvgByDpi { get => _autoScaleSvgByDpi; set { _autoScaleSvgByDpi = value; InvalidateSvgCache(); Invalidate(); } }

            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public SvgDocument SvgDocument
            {
                get => _svgDoc;
                set
                {
                    _svgDoc = value;               // 允許為 null
                    _useSvg = _svgDoc != null;
                    _svgRecolorPending = true;     // 新文件需要套用覆蓋色
                    InvalidateSvgCache();
                    Invalidate();
                }
            }

            [Category("SVG")]
            public string SvgPath
            {
                get => _svgPath;
                set
                {
                    _svgPath = value;
                    _svgDoc = null;

                    if (!string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            _svgDoc = SvgDocument.Open(value);
                            _useSvg = true;
                            _svgRecolorPending = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"SvgPath open failed: {ex.Message}");
                            _svgDoc = null;
                        }
                    }

                    InvalidateSvgCache();
                    Invalidate();
                }
            }

            // 覆蓋 Fill/Stroke（破壞性；清空請設為 Color.Empty 並重新載入文件）
            [Category("SVG Override")]
            public Color SvgOverrideFillColor
            {
                get => _svgOverrideFill;
                set { _svgOverrideFill = value; _svgRecolorPending = true; InvalidateSvgCache(); Invalidate(); }
            }

            [Category("SVG Override")]
            public Color SvgOverrideStrokeColor
            {
                get => _svgOverrideStroke;
                set { _svgOverrideStroke = value; _svgRecolorPending = true; InvalidateSvgCache(); Invalidate(); }
            }

            // ---------- Icon Tint（針對轉成 Bitmap 後的著色；與上面不同用法） ----------
            [Category("Icon")]
            public Color IconNormalColor { get => _iconNormalColor; set { _iconNormalColor = value; Invalidate(); } }

            [Category("Icon")]
            public Color IconHoverColor { get => _iconHoverColor; set { _iconHoverColor = value; Invalidate(); } }

            [Category("Icon")]
            public Color IconPressedColor { get => _iconPressedColor; set { _iconPressedColor = value; Invalidate(); } }

            [Category("Icon")]
            public Color IconDisabledColor { get => _iconDisabledColor; set { _iconDisabledColor = value; Invalidate(); } }

            // ---------- Loading ----------
            [Category("Loading"), DefaultValue(false)]
            public bool IsLoading
            {
                get => _isLoading;
                set
                {
                    _isLoading = value;
                    if (_isLoading) _animTimer.Start(); else _animTimer.Stop();
                    Invalidate();
                }
            }

            [Category("Loading"), DefaultValue(true)]
            public bool DisableWhileLoading { get => _disableWhileLoading; set { _disableWhileLoading = value; Invalidate(); } }

            [Category("Loading"), DefaultValue(true)]
            public bool OverlayWhileLoading { get => _overlayWhileLoading; set { _overlayWhileLoading = value; Invalidate(); } }

            [Category("Loading"), DefaultValue(90)]
            public int LoadingOverlayOpacity { get => _overlayOpacity; set { _overlayOpacity = Math.Max(0, Math.Min(255, value)); Invalidate(); } }

            [Category("Loading")]
            public Color SpinnerColor { get => _spinnerColor; set { _spinnerColor = value; Invalidate(); } }

            [Category("Loading"), DefaultValue(3)]
            public int SpinnerThickness { get => _spinnerThickness; set { _spinnerThickness = Math.Max(1, value); Invalidate(); } }

            [Category("Loading"), DefaultValue(10)]
            public int SpinnerRadius { get => _spinnerRadius; set { _spinnerRadius = Math.Max(4, value); Invalidate(); } }

            [Category("Loading"), DefaultValue(180)]
            public int SpinnerSpeed { get => _spinnerSpeed; set { _spinnerSpeed = Math.Max(30, Math.Min(720, value)); } }

            // ---------- IButtonControl ----------
            [Browsable(true), Category("Behavior"), DefaultValue(typeof(DialogResult), "None")]
            public DialogResult DialogResult { get => _dialogResult; set => _dialogResult = value; }

            void IButtonControl.NotifyDefault(bool value) { _isDefaultButton = value; Invalidate(); }

            public void PerformClick()
            {
                if (!CanInteract()) return;
                OnClick(EventArgs.Empty);
                if (FindForm() is Form frm)
                    frm.DialogResult = DialogResult;
            }

            // ---------- 真圓角可見區 ----------
            protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateRegion(); }
            private void UpdateRegion()
            {
                using var path = RoundedRect(ClientRectangle, _cornerRadius);
                Region?.Dispose();
                Region = new Region(path);
            }

            // ---------- 互動可用性 ----------
            private bool CanInteract() => Enabled && !(_isLoading && _disableWhileLoading);

            // ---------- 繪製 ----------
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var bounds = ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0) return;

                // 陰影（畫在外圍）
                if (_shadowEnabled) DrawShadow(e.Graphics, bounds, _cornerRadius, _shadowColor, _shadowOffset, _shadowSoftness);

                // 背景 & 邊框
                int bt = _borderThickness;
                Color fill = Enabled ? (_pressed ? _backPressed : (_hovered ? _backHover : _backNormal))
                                     : Blend(_backNormal, SystemColors.Control, 0.5);
                Color border = Enabled ? (_pressed ? _pressedBorderColor : (_hovered ? _hoverBorderColor : _borderColor))
                                       : Blend(_borderColor, SystemColors.GrayText, 0.5);

                using (var path = RoundedRect(bounds, _cornerRadius))
                using (var br = new SolidBrush(fill))
                using (var pen = bt > 0 ? new Pen(border, bt) : null)
                {
                    e.Graphics.FillPath(br, path);
                    if (bt > 0 && pen != null) e.Graphics.DrawPath(pen, path);
                }

                // 內容區域
                var inner = new Rectangle(bounds.Left + bt, bounds.Top + bt, bounds.Width - 2 * bt, bounds.Height - 2 * bt);
                var content = new Rectangle(inner.Left + Padding.Left, inner.Top + Padding.Top,
                                            Math.Max(0, inner.Width - Padding.Horizontal),
                                            Math.Max(0, inner.Height - Padding.Vertical));

                // 尺寸
                Size imgSz = new Size(Math.Max(0, _imageSize.Width), Math.Max(0, _imageSize.Height));
                SizeF txtSzF = e.Graphics.MeasureString(Text ?? string.Empty, Font);
                Size txtSz = new Size((int)Math.Ceiling(txtSzF.Width), (int)Math.Ceiling(txtSzF.Height));

                bool hasSvg = _useSvg && _svgDoc != null;
                bool showImage = (hasSvg || _image != null) && imgSz.Width > 0 && imgSz.Height > 0;
                bool showText = !string.IsNullOrEmpty(Text);

                Rectangle imgRect = Rectangle.Empty, txtRect = Rectangle.Empty;

                // 排版
                if (showImage && showText)
                {
                    switch (_relation)
                    {
                        default:
                        case TextImageRelation.ImageBeforeText:
                            {
                                int totalW = imgSz.Width + _textImageSpacing + txtSz.Width;
                                int startX = content.Left + Math.Max(0, (content.Width - totalW) / 2);
                                int centerY = content.Top + content.Height / 2;

                                imgRect = new Rectangle(startX, centerY - imgSz.Height / 2, imgSz.Width, imgSz.Height);
                                txtRect = new Rectangle(imgRect.Right + _textImageSpacing, centerY - txtSz.Height / 2, txtSz.Width, txtSz.Height);
                                break;
                            }
                        case TextImageRelation.TextBeforeImage:
                            {
                                int totalW = txtSz.Width + _textImageSpacing + imgSz.Width;
                                int startX = content.Left + Math.Max(0, (content.Width - totalW) / 2);
                                int centerY = content.Top + content.Height / 2;

                                txtRect = new Rectangle(startX, centerY - txtSz.Height / 2, txtSz.Width, txtSz.Height);
                                imgRect = new Rectangle(txtRect.Right + _textImageSpacing, centerY - imgSz.Height / 2, imgSz.Width, imgSz.Height);
                                break;
                            }
                        case TextImageRelation.ImageAboveText:
                            {
                                int totalH = imgSz.Height + _textImageSpacing + txtSz.Height;
                                int startY = content.Top + Math.Max(0, (content.Height - totalH) / 2);
                                int centerX = content.Left + content.Width / 2;

                                imgRect = new Rectangle(centerX - imgSz.Width / 2, startY, imgSz.Width, imgSz.Height);
                                txtRect = new Rectangle(content.Left, imgRect.Bottom + _textImageSpacing, content.Width, txtSz.Height);
                                break;
                            }
                        case TextImageRelation.TextAboveImage:
                            {
                                int totalH = txtSz.Height + _textImageSpacing + imgSz.Height;
                                int startY = content.Top + Math.Max(0, (content.Height - totalH) / 2);
                                int centerX = content.Left + content.Width / 2;

                                txtRect = new Rectangle(content.Left, startY, content.Width, txtSz.Height);
                                imgRect = new Rectangle(centerX - imgSz.Width / 2, txtRect.Bottom + _textImageSpacing, imgSz.Width, imgSz.Height);
                                break;
                            }
                    }
                }
                else if (showImage) imgRect = AlignWithin(content, imgSz, _imageAlign);
                else if (showText) txtRect = AlignWithin(content, txtSz, _textAlign);

                // 圖示
                if (showImage)
                {
                    Color tint = ResolveIconColor(); // 位圖/轉成位圖的色彩覆寫
                    bool effectiveEnabled = Enabled && !(_isLoading && _overlayWhileLoading);
                    using var ia = BuildTintAttributes(tint, effectiveEnabled);
                    var target = new Rectangle(imgRect.X, imgRect.Y, Math.Max(1, imgRect.Width), Math.Max(1, imgRect.Height));

                    if (hasSvg)
                    {
                        var bmp = GetOrRebuildSvgBitmapCache(e.Graphics);
                        if (bmp != null)
                            e.Graphics.DrawImage(bmp, target, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia);
                    }
                    else if (_image != null)
                    {
                        e.Graphics.DrawImage(_image, target, 0, 0, _image.Width, _image.Height, GraphicsUnit.Pixel, ia);
                    }
                }

                // 文字
                if (showText)
                {
                    var textColorBase = Enabled ? ForeColor : Blend(ForeColor, SystemColors.GrayText, 0.5);
                    var textColor = (_isLoading && _overlayWhileLoading) ? Color.FromArgb(180, textColorBase) : textColorBase;

                    using var sf = (showImage && showText)
                        ? new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }
                        : BuildStringFormat(_textAlign);
                    using var br = new SolidBrush(textColor);
                    var textRect = (showImage && showText) ? txtRect : AlignWithin(content, txtSz, _textAlign);
                    e.Graphics.DrawString(Text, Font, br, textRect, sf);
                }

                // Loading 覆蓋 + Spinner
                if (_isLoading)
                {
                    if (_overlayWhileLoading)
                    {
                        using var overlayPath = RoundedRect(bounds, _cornerRadius);
                        using var overlayBr = new SolidBrush(Color.FromArgb(_overlayOpacity, Color.White));
                        e.Graphics.FillPath(overlayBr, overlayPath);
                    }
                    DrawSpinner(e.Graphics, bounds);
                }

                // 焦點提示（方法二：可關閉）
                if (_showFocusOutline && (Focused || _isDefaultButton))
                {
                    using var focus = RoundedRect(Rectangle.Inflate(bounds, -_focusInset, -_focusInset), Math.Max(0, _cornerRadius - _focusInset));
                    using var pen = new Pen(Color.FromArgb(120, Color.DodgerBlue), 1f) { DashStyle = DashStyle.Dash };
                    e.Graphics.DrawPath(pen, focus);
                }
            }

            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
            protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
            protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
            protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Invalidate(); }
            protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _svgCache?.Dispose();
                    _animTimer?.Dispose();
                    Region?.Dispose();
                }
                base.Dispose(disposing);
            }

            // ---------- 陰影（疊畫柔邊） ----------
            private static void DrawShadow(Graphics g, Rectangle bounds, int radius, Color color, Point offset, int softness)
            {
                var shadowRect = new Rectangle(bounds.X + offset.X, bounds.Y + offset.Y, bounds.Width, bounds.Height);
                int layers = Math.Max(0, softness);
                if (layers == 0) return;

                int aBase = color.A;
                var baseColor = Color.FromArgb(255, color);
                for (int i = layers; i >= 1; i--)
                {
                    float t = (float)i / (layers + 1);
                    int a = (int)(aBase * t * 0.5f);
                    using var p = new Pen(Color.FromArgb(Math.Max(0, Math.Min(255, a)), baseColor), 2f) { LineJoin = LineJoin.Round };
                    using var expanded = RoundedRect(Rectangle.Inflate(shadowRect, i, i), Math.Max(0, radius + i));
                    g.DrawPath(p, expanded);
                }
            }

            // ---------- Spinner ----------
            private void DrawSpinner(Graphics g, Rectangle bounds)
            {
                var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                int r = Math.Min(_spinnerRadius, Math.Max(8, Math.Min(bounds.Width, bounds.Height) / 2 - 6));
                int thick = _spinnerThickness;

                using var penBg = new Pen(Color.FromArgb(50, _spinnerColor), thick) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                using var penFg = new Pen(_spinnerColor, thick) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                var rect = new Rectangle(center.X - r, center.Y - r, 2 * r, 2 * r);

                g.DrawArc(penBg, rect, 0, 360);            // 背圈
                g.DrawArc(penFg, rect, _spinnerAngle, 270); // 旋轉 270°
            }

            // ---------- Tint & 小工具 ----------
            private Color ResolveIconColor()
            {
                if (!Enabled) return _iconDisabledColor;
                if (_pressed && _iconPressedColor != Color.Empty) return _iconPressedColor;
                if (_hovered && _iconHoverColor != Color.Empty) return _iconHoverColor;
                if (_iconNormalColor != Color.Empty) return _iconNormalColor;
                return Color.Empty;
            }

            private static ImageAttributes BuildTintAttributes(Color tint, bool effectiveEnabled)
            {
                var ia = new ImageAttributes();
                if (tint != Color.Empty)
                {
                    float r = tint.R / 255f, g = tint.G / 255f, b = tint.B / 255f;
                    var cm = new ColorMatrix(new float[][]
                    {
                    new float[]{r,0,0,0,0},
                    new float[]{0,g,0,0,0},
                    new float[]{0,0,b,0,0},
                    new float[]{0,0,0,1,0},
                    new float[]{0,0,0,0,1}
                    });
                    ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                }
                else if (!effectiveEnabled)
                {
                    var cm = new ColorMatrix(new float[][]
                    {
                    new float[]{1,0,0,0,0},
                    new float[]{0,1,0,0,0},
                    new float[]{0,0,1,0,0},
                    new float[]{0,0,0,0.4f,0},
                    new float[]{0,0,0,0,1}
                    });
                    ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                }
                return ia;
            }

            private static GraphicsPath RoundedRect(Rectangle rect, int radius)
            {
                int r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
                int d = r * 2;
                var path = new GraphicsPath();

                if (r <= 0)
                {
                    path.AddRectangle(rect);
                    path.CloseFigure();
                    return path;
                }

                var arc = new Rectangle(rect.Location, new Size(d, d));
                path.AddArc(arc, 180, 90);                       // 左上
                arc.X = rect.Right - d; path.AddArc(arc, 270, 90); // 右上
                arc.Y = rect.Bottom - d; path.AddArc(arc, 0, 90);  // 右下
                arc.X = rect.Left; path.AddArc(arc, 90, 90); // 左下
                path.CloseFigure();
                return path;
            }

            private static Color Blend(Color a, Color b, double t)
            {
                t = Math.Max(0, Math.Min(1, t));
                return Color.FromArgb(
                    (int)(a.A * (1 - t) + b.A * t),
                    (int)(a.R * (1 - t) + b.R * t),
                    (int)(a.G * (1 - t) + b.G * t),
                    (int)(a.B * (1 - t) + b.B * t));
            }

            private static Rectangle AlignWithin(Rectangle outer, Size inner, ContentAlignment align)
            {
                int x = outer.X, y = outer.Y;

                if (IsLeft(align)) x = outer.Left;
                else if (IsCenter(align)) x = outer.Left + (outer.Width - inner.Width) / 2;
                else if (IsRight(align)) x = outer.Right - inner.Width;

                if (IsTop(align)) y = outer.Top;
                else if (IsMiddle(align)) y = outer.Top + (outer.Height - inner.Height) / 2;
                else if (IsBottom(align)) y = outer.Bottom - inner.Height;

                return new Rectangle(new Point(x, y), inner);
            }

            private static StringFormat BuildStringFormat(ContentAlignment align)
            {
                var sf = new StringFormat(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter };

                if (IsLeft(align)) sf.Alignment = StringAlignment.Near;
                else if (IsCenter(align)) sf.Alignment = StringAlignment.Center;
                else sf.Alignment = StringAlignment.Far;

                if (IsTop(align)) sf.LineAlignment = StringAlignment.Near;
                else if (IsMiddle(align)) sf.LineAlignment = StringAlignment.Center;
                else sf.LineAlignment = StringAlignment.Far;

                return sf;
            }

            // 對齊輔助
            private static bool IsLeft(ContentAlignment a) => a == ContentAlignment.TopLeft || a == ContentAlignment.MiddleLeft || a == ContentAlignment.BottomLeft;
            private static bool IsRight(ContentAlignment a) => a == ContentAlignment.TopRight || a == ContentAlignment.MiddleRight || a == ContentAlignment.BottomRight;
            private static bool IsCenter(ContentAlignment a) => a == ContentAlignment.TopCenter || a == ContentAlignment.MiddleCenter || a == ContentAlignment.BottomCenter;

            private static bool IsTop(ContentAlignment a) => a == ContentAlignment.TopLeft || a == ContentAlignment.TopCenter || a == ContentAlignment.TopRight;
            private static bool IsBottom(ContentAlignment a) => a == ContentAlignment.BottomLeft || a == ContentAlignment.BottomCenter || a == ContentAlignment.BottomRight;
            private static bool IsMiddle(ContentAlignment a) => a == ContentAlignment.MiddleLeft || a == ContentAlignment.MiddleCenter || a == ContentAlignment.MiddleRight;

            private void InvalidateSvgCache()
            {
                _svgCache?.Dispose();
                _svgCache = null;
                _svgCacheLogicalSize = Size.Empty;
                _svgCacheDpiX = _svgCacheDpiY = 0f;
            }

            private Bitmap GetOrRebuildSvgBitmapCache(Graphics g)
            {
                if (_svgDoc == null) return null;

                // 需要時先套用覆蓋色（破壞性）
                if (_svgRecolorPending && (_svgOverrideFill != Color.Empty || _svgOverrideStroke != Color.Empty))
                {
                    try
                    {
                        ApplySvgColorOverrides(_svgDoc, _svgOverrideFill, _svgOverrideStroke);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ApplySvgColorOverrides failed: " + ex.Message);
                    }
                    finally
                    {
                        _svgRecolorPending = false;
                    }
                }

                float dpiX = g.DpiX <= 0 ? 96f : g.DpiX;
                float dpiY = g.DpiY <= 0 ? 96f : g.DpiY;

                var logical = _imageSize;
                if (logical.Width <= 0 || logical.Height <= 0) return null;

                int pxW = logical.Width, pxH = logical.Height;
                if (_autoScaleSvgByDpi)
                {
                    pxW = (int)Math.Round(logical.Width * dpiX / 96.0);
                    pxH = (int)Math.Round(logical.Height * dpiY / 96.0);
                }

                bool need =
                    _svgCache == null ||
                    _svgCacheLogicalSize != logical ||
                    _svgCacheDpiX != dpiX ||
                    _svgCacheDpiY != dpiY ||
                    _svgCache.Width != pxW ||
                    _svgCache.Height != pxH;

                if (!need) return _svgCache;

                try
                {
                    var bmp = _svgDoc.Draw(pxW, pxH);
                    bmp.SetResolution(dpiX, dpiY);

                    InvalidateSvgCache();
                    _svgCache = bmp;
                    _svgCacheLogicalSize = logical;
                    _svgCacheDpiX = dpiX;
                    _svgCacheDpiY = dpiY;
                    return _svgCache;
                }
                catch
                {
                    InvalidateSvgCache();
                    return null;
                }
            }

            // 遞迴覆蓋 Fill/Stroke（破壞性）
            private static void ApplySvgColorOverrides(SvgElement node, Color fill, Color stroke)
            {
                if (node is SvgVisualElement vis)
                {
                    if (fill != Color.Empty) vis.Fill = new SvgColourServer(fill);
                    if (stroke != Color.Empty) vis.Stroke = new SvgColourServer(stroke);
                }

                if (node is SvgDocument doc)
                {
                    foreach (var child in doc.Children)
                        ApplySvgColorOverrides(child, fill, stroke);
                }
                else
                {
                    foreach (var child in node.Children)
                        ApplySvgColorOverrides(child, fill, stroke);
                }
            }

            // 無障礙
            protected override AccessibleObject CreateAccessibilityInstance() => new ControlAccessibleObject(this) { };
        }








    }

}



       