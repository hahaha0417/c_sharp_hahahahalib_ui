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
        [DefaultProperty(nameof(Text))]
        public class GlowTextBox : UserControl
        {
            private readonly TextBox _tb = new TextBox();
            private bool _hovered;
            private bool _focused;

            private int _cornerRadius = 12;
            private int _borderThickness = 1;
            private int _glowSize = 6;

            private Color _fillColor = Color.White;
            private Color _borderColor = Color.Silver;
            private Color _focusBorderColor = Color.DodgerBlue;
            private Color _hoverBorderColor = Color.SteelBlue; // 新增：Hover 邊框色
            private Color _glowColor = Color.DodgerBlue;

            private Padding _innerPadding = new Padding(10, 6, 10, 6);
            private bool _glowOnFocus = true;
            private bool _glowOnHover = false;

            private string _placeholderText = string.Empty;

            private bool _autoFitWidth = false;
            private bool _autoFitHeight = false;
            private bool _showBorder = true;

            public GlowTextBox()
            {
                SetStyle(ControlStyles.UserPaint |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.SupportsTransparentBackColor, true);

                BackColor = Color.Transparent;
                Font = new Font("Segoe UI", 9f);

                // 內部 TextBox：移除預設立體邊框
                _tb.BorderStyle = BorderStyle.None;
                _tb.BackColor = _fillColor;
                _tb.Font = this.Font;
                _tb.ForeColor = this.ForeColor;
                _tb.TextChanged += (_, __) =>
                {
                    base.Text = _tb.Text;
                    UpdateAutoFitSize();
                    OnTextChanged(EventArgs.Empty);
                };
                _tb.GotFocus += (_, __) => { _focused = true; Invalidate(); };
                _tb.LostFocus += (_, __) => { _focused = false; Invalidate(); };
                _tb.MouseEnter += (_, __) => { _hovered = true; Invalidate(); };
                _tb.MouseLeave += (_, __) => { _hovered = false; Invalidate(); };

                Controls.Add(_tb);

                this.MouseEnter += (_, __) => { _hovered = true; Invalidate(); };
                this.MouseLeave += (_, __) => { _hovered = false; Invalidate(); };
                this.GotFocus += (_, __) => _tb.Focus();

                Size = new Size(200, 36);
                UpdateTextBoxBounds();
            }

            #region Win32 Cue Banner
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
            private const int EM_SETCUEBANNER = 0x1501;

            private void ApplyPlaceholder()
            {
                if (IsHandleCreated && _tb.IsHandleCreated)
                    SendMessage(_tb.Handle, EM_SETCUEBANNER, 1, _placeholderText ?? string.Empty);
            }
            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                ApplyPlaceholder();
            }
            #endregion

            #region Public Properties
            [Category("Appearance")]
            public Color FillColor
            {
                get => _fillColor;
                set { _fillColor = value; _tb.BackColor = value; Invalidate(); }
            }

            [Category("Appearance")]
            public Color BorderColor
            {
                get => _borderColor;
                set { _borderColor = value; Invalidate(); }
            }

            [Category("Appearance")]
            public Color FocusBorderColor
            {
                get => _focusBorderColor;
                set { _focusBorderColor = value; Invalidate(); }
            }

            [Category("Appearance")]
            public Color HoverBorderColor
            {
                get => _hoverBorderColor;
                set { _hoverBorderColor = value; Invalidate(); }
            }

            [Category("Appearance")]
            public Color GlowColor
            {
                get => _glowColor;
                set { _glowColor = value; Invalidate(); }
            }

            [Category("Appearance")]
            [DefaultValue(12)]
            public int CornerRadius
            {
                get => _cornerRadius;
                set { _cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
            }

            [Category("Appearance")]
            [DefaultValue(1)]
            public int BorderThickness
            {
                get => _borderThickness;
                set { _borderThickness = Math.Max(0, value); UpdateTextBoxBounds(); Invalidate(); }
            }

            [Category("Appearance")]
            [DefaultValue(true)]
            public bool ShowBorder
            {
                get => _showBorder;
                set { _showBorder = value; Invalidate(); }
            }

            [Category("Appearance")]
            [DefaultValue(6)]
            public int GlowSize
            {
                get => _glowSize;
                set { _glowSize = Math.Max(0, value); Invalidate(); }
            }

            [Category("Layout")]
            public Padding InnerPadding
            {
                get => _innerPadding;
                set { _innerPadding = value; UpdateTextBoxBounds(); UpdateAutoFitSize(); }
            }

            [Category("Behavior")]
            [DefaultValue(true)]
            public bool GlowOnFocus
            {
                get => _glowOnFocus;
                set { _glowOnFocus = value; Invalidate(); }
            }

            [Category("Behavior")]
            [DefaultValue(false)]
            public bool GlowOnHover
            {
                get => _glowOnHover;
                set { _glowOnHover = value; Invalidate(); }
            }

            [Category("Appearance")]
            [DefaultValue("")]
            public string PlaceholderText
            {
                get => _placeholderText;
                set { _placeholderText = value ?? string.Empty; ApplyPlaceholder(); UpdateAutoFitSize(); }
            }

            [Category("Behavior")]
            [DefaultValue(false)]
            public bool Multiline
            {
                get => _tb.Multiline;
                set
                {
                    _tb.Multiline = value;
                    _tb.AcceptsReturn = value;
                    UpdateTextBoxBounds();
                    UpdateAutoFitSize();
                }
            }

            [Category("Behavior")]
            [DefaultValue(false)]
            public bool UseSystemPasswordChar
            {
                get => _tb.UseSystemPasswordChar;
                set => _tb.UseSystemPasswordChar = value;
            }

            [Category("Behavior")]
            [DefaultValue(false)]
            public bool AutoFitWidth
            {
                get => _autoFitWidth;
                set { _autoFitWidth = value; UpdateAutoFitSize(); }
            }

            [Category("Behavior")]
            [DefaultValue(false)]
            public bool AutoFitHeight
            {
                get => _autoFitHeight;
                set { _autoFitHeight = value; UpdateAutoFitSize(); }
            }

            public override string Text
            {
                get => _tb.Text;
                set { _tb.Text = value; UpdateAutoFitSize(); Invalidate(); }
            }

            public override Font Font
            {
                get => base.Font;
                set { base.Font = value; if (_tb != null) _tb.Font = value; UpdateTextBoxBounds(); UpdateAutoFitSize(); Invalidate(); }
            }

            public override Color ForeColor
            {
                get => base.ForeColor;
                set { base.ForeColor = value; if (_tb != null) _tb.ForeColor = value; }
            }
            #endregion

            #region Layout & Painting
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateRegion();
                UpdateTextBoxBounds();
                if (_autoFitHeight) UpdateAutoFitSize();
            }

            private void UpdateTextBoxBounds()
            {
                int inset = Math.Max(0, _borderThickness) + 1;
                var rect = new Rectangle(
                    _innerPadding.Left + inset,
                    _innerPadding.Top + inset,
                    Math.Max(10, Width - _innerPadding.Horizontal - inset * 2),
                    Math.Max(10, Height - _innerPadding.Vertical - inset * 2));

                _tb.Location = new Point(rect.Left, rect.Top);
                _tb.Size = new Size(rect.Width, rect.Height);
            }

            private void UpdateRegion()
            {
                using (var gp = CreateRoundRectPath(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), _cornerRadius))
                    this.Region = new Region(gp);
            }

            private void UpdateAutoFitSize()
            {
                if (!(_autoFitWidth || _autoFitHeight)) { UpdateTextBoxBounds(); return; }

                string measureText =
                    !string.IsNullOrEmpty(_tb.Text) ? _tb.Text :
                    (!string.IsNullOrEmpty(_placeholderText) ? _placeholderText : "測試字");

                int borderPad = _borderThickness * 2 + 6;
                int desiredWidth = Width;
                int desiredHeight = Height;

                if (!_tb.Multiline)
                {
                    var size = TextRenderer.MeasureText(measureText, this.Font, new Size(int.MaxValue, int.MaxValue),
                                                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                    if (_autoFitWidth)
                        desiredWidth = size.Width + _innerPadding.Horizontal + borderPad;
                    if (_autoFitHeight)
                        desiredHeight = size.Height + _innerPadding.Vertical + borderPad;
                }
                else
                {
                    var lines = measureText.Replace("\r", "").Split('\n');
                    if (_autoFitWidth)
                    {
                        int maxLineWidth = 0;
                        foreach (var line in lines)
                        {
                            var s = TextRenderer.MeasureText(line.Length == 0 ? " " : line, this.Font,
                                        new Size(int.MaxValue, int.MaxValue),
                                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                            if (s.Width > maxLineWidth) maxLineWidth = s.Width;
                        }
                        desiredWidth = maxLineWidth + _innerPadding.Horizontal + borderPad;
                    }

                    if (_autoFitHeight)
                    {
                        int wrapWidth = _autoFitWidth ? desiredWidth : this.Width;
                        int textAreaWidth = Math.Max(10, wrapWidth - _innerPadding.Horizontal - borderPad);
                        var size = TextRenderer.MeasureText(measureText.Length == 0 ? " " : measureText, this.Font,
                                    new Size(textAreaWidth, int.MaxValue),
                                    TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak);
                        desiredHeight = size.Height + _innerPadding.Vertical + borderPad;
                    }
                }

                if (desiredWidth != this.Width || desiredHeight != this.Height)
                    this.Size = new Size(desiredWidth, desiredHeight);

                UpdateTextBoxBounds();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                var rect = new RectangleF(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f));

                using (var path = CreateRoundRectPath(Rectangle.Round(rect), _cornerRadius))
                {
                    // Glow
                    bool showGlow = (_glowOnFocus && _focused) || (_glowOnHover && _hovered);
                    if (showGlow && _glowSize > 0)
                    {
                        for (int i = _glowSize; i >= 1; i--)
                        {
                            float inset = _borderThickness + i * 1.5f;
                            int alpha = (int)(60f * (i / (float)_glowSize));

                            using (var inner = CreateInsetPath(path, inset, _cornerRadius))
                            using (var p = new Pen(Color.FromArgb(alpha, _glowColor), Math.Max(1f, _borderThickness + i)))
                            {
                                p.LineJoin = LineJoin.Round;
                                p.Alignment = PenAlignment.Inset;
                                g.DrawPath(p, inner);
                            }
                        }
                    }

                    // 填滿背景
                    using (var br = new SolidBrush(_fillColor))
                        g.FillPath(br, path);

                    // 邊框（Focus > Hover > Normal）
                    if (_showBorder && _borderThickness > 0)
                    {
                        Color border = _focused
                            ? _focusBorderColor
                            : (_hovered ? _hoverBorderColor : _borderColor);

                        using (var p = new Pen(border, _borderThickness))
                        {
                            p.LineJoin = LineJoin.Round;
                            p.Alignment = PenAlignment.Inset;
                            g.DrawPath(p, path);
                        }
                    }
                }
            }
            #endregion

            #region Geometry Helpers
            private static GraphicsPath CreateRoundRectPath(Rectangle r, int radius)
            {
                int d = Math.Max(0, radius * 2);
                var gp = new GraphicsPath();
                if (d <= 0) { gp.AddRectangle(r); return gp; }

                int right = r.Right - 1, bottom = r.Bottom - 1;
                gp.AddArc(r.Left, r.Top, d, d, 180, 90);
                gp.AddArc(right - d + 1, r.Top, d, d, 270, 90);
                gp.AddArc(right - d + 1, bottom - d + 1, d, d, 0, 90);
                gp.AddArc(r.Left, bottom - d + 1, d, d, 90, 90);
                gp.CloseFigure();
                return gp;
            }

            private static GraphicsPath CreateInsetPath(GraphicsPath basePath, float inset, int baseCornerRadius)
            {
                var b = basePath.GetBounds();
                var shrunk = Rectangle.Round(new RectangleF(
                    b.X + inset,
                    b.Y + inset,
                    Math.Max(1f, b.Width - inset * 2f),
                    Math.Max(1f, b.Height - inset * 2f)
                ));

                int r = Math.Max(0, (int)Math.Round(baseCornerRadius - inset * 0.6f));
                return CreateRoundRectPath(shrunk, r);
            }
            #endregion
        }
    }
}
