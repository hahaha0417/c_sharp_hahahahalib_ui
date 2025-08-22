using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace hahahalib
{
    namespace ui
    {


        [DefaultEvent("CheckedChanged")]
        public class ToggleSwitch : CheckBox
        {
            private System.Windows.Forms.Timer _animTimer;
            private float _thumb;              // 0..1，滑塊位置
            private float _from, _to;
            private int _animFrames = 10;
            private int _animTicks = 0;

            // Hover 狀態
            private bool _hoverThumb = false;

            public ToggleSwitch()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                       | ControlStyles.OptimizedDoubleBuffer
                       | ControlStyles.ResizeRedraw
                       | ControlStyles.UserPaint, true);

                MinimumSize = new Size(30, 16);
                Size = new Size(60, 32);
                TabStop = true;
                Appearance = Appearance.Button;
                TextAlign = ContentAlignment.MiddleCenter;

                // 避免與我們的點擊切換邏輯重複
                AutoCheck = false;

                _animTimer = new System.Windows.Forms.Timer { Interval = 15 };
                _animTimer.Tick += AnimTimer_Tick;

                _thumb = Checked ? 1f : 0f;
            }

            // ===== 外觀設定 =====
            private Color _onBackColor = Color.MediumSeaGreen;
            [Category("Appearance")]
            public Color OnBackColor { get => _onBackColor; set { _onBackColor = value; Invalidate(); } }

            private Color _offBackColor = Color.Gray;
            [Category("Appearance")]
            public Color OffBackColor { get => _offBackColor; set { _offBackColor = value; Invalidate(); } }

            private Color _onToggleColor = Color.White;
            [Category("Appearance")]
            public Color OnToggleColor { get => _onToggleColor; set { _onToggleColor = value; Invalidate(); } }

            private Color _offToggleColor = Color.WhiteSmoke;
            [Category("Appearance")]
            public Color OffToggleColor { get => _offToggleColor; set { _offToggleColor = value; Invalidate(); } }

            private bool _showText = false;
            [Category("Appearance")]
            public bool ShowText { get => _showText; set { _showText = value; Invalidate(); } }

            private string _onText = "ON";
            [Category("Appearance")]
            public string OnText { get => _onText; set { _onText = value; Invalidate(); } }

            private string _offText = "OFF";
            [Category("Appearance")]
            public string OffText { get => _offText; set { _offText = value; Invalidate(); } }

            // ===== 動畫設定 =====
            private bool _enableAnimation = true;
            [Category("Behavior")]
            public bool EnableAnimation { get => _enableAnimation; set => _enableAnimation = value; }

            [Category("Behavior")]
            public int AnimationFrames { get => _animFrames; set => _animFrames = Math.Max(1, value); }

            // ===== 焦點光暈設定 =====
            private bool _showFocusGlow = true;
            [Category("Appearance")]
            public bool ShowFocusGlow { get => _showFocusGlow; set { _showFocusGlow = value; Invalidate(); } }

            private Color _focusGlowColor = Color.DeepSkyBlue;
            [Category("Appearance")]
            public Color FocusGlowColor { get => _focusGlowColor; set { _focusGlowColor = value; Invalidate(); } }

            private int _focusGlowSpread = 10;
            [Category("Appearance")]
            public int FocusGlowSpread { get => _focusGlowSpread; set { _focusGlowSpread = Math.Max(1, value); Invalidate(); } }

            private int _focusGlowLayers = 8;
            [Category("Appearance")]
            public int FocusGlowLayers { get => _focusGlowLayers; set { _focusGlowLayers = Math.Max(1, value); Invalidate(); } }

            // ===== 滑塊 Hover 漸層設定 =====
            private bool _enableThumbHoverGradient = true;
            [Category("Appearance")]
            [Description("滑過滑塊時，使用漸層顏色填滿滑塊")]
            public bool EnableThumbHoverGradient
            {
                get => _enableThumbHoverGradient;
                set { _enableThumbHoverGradient = value; Invalidate(); }
            }

            private Color _thumbHoverStart = Color.White;
            [Category("Appearance")]
            [Description("滑塊 Hover 漸層起始色")]
            public Color ThumbHoverStartColor
            {
                get => _thumbHoverStart;
                set { _thumbHoverStart = value; Invalidate(); }
            }

            private Color _thumbHoverEnd = Color.Gainsboro;
            [Category("Appearance")]
            [Description("滑塊 Hover 漸層結束色")]
            public Color ThumbHoverEndColor
            {
                get => _thumbHoverEnd;
                set { _thumbHoverEnd = value; Invalidate(); }
            }

            // ===== 行為 =====
            protected override void OnCheckedChanged(EventArgs e)
            {
                base.OnCheckedChanged(e);
                StartAnimation(Checked ? 1f : 0f);
                Invalidate();
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);

                if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                {
                    Checked = !Checked;
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Left) { Checked = false; e.Handled = true; }
                else if (e.KeyCode == Keys.Right) { Checked = true; e.Handled = true; }
            }

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                base.OnMouseDown(mevent);

                // 左半關、右半開
                bool wantOn = mevent.X >= Width / 2;
                if (Checked != wantOn) Checked = wantOn;

                // 取得焦點顯示光暈
                if (!Focused) Focus();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                bool overThumb = GetThumbRect().Contains(e.Location);

                if (_hoverThumb != overThumb)
                {
                    _hoverThumb = overThumb;
                    Cursor = _hoverThumb ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoverThumb)
                {
                    _hoverThumb = false;
                    Cursor = Cursors.Default;
                    Invalidate();
                }
            }

            // ===== 繪圖 =====
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

                int pad = Math.Max(2, Width / 30);   // 內縮隨大小
                int w = Width - 1;
                int h = Height - 1;
                int trackRadius = h;

                RectangleF track = new RectangleF(0, 0, w, h);

                // 背景膠囊
                using (GraphicsPath path = RoundedRect(track, trackRadius))
                using (SolidBrush back = new SolidBrush(Checked ? OnBackColor : OffBackColor))
                    e.Graphics.FillPath(back, path);

                // 滑塊（含 Hover 漸層）
                RectangleF thumbRect = GetThumbRect();
                if (EnableThumbHoverGradient && _hoverThumb)
                {
                    using var lg = new LinearGradientBrush(thumbRect, ThumbHoverStartColor, ThumbHoverEndColor, LinearGradientMode.Vertical);
                    using var path = new GraphicsPath();
                    path.AddEllipse(thumbRect);
                    e.Graphics.FillPath(lg, path);
                }
                else
                {
                    using var tb = new SolidBrush(Checked ? OnToggleColor : OffToggleColor);
                    e.Graphics.FillEllipse(tb, thumbRect);
                }

                // 文字（可選）
                if (ShowText)
                {
                    string txt = Checked ? OnText : OffText;
                    using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    using var br = new SolidBrush(Color.White);
                    e.Graphics.DrawString(txt, Font, br, track, sf);
                }

                // 焦點光暈
                if (Focused && ShowFocusGlow)
                    DrawFocusGlow(e.Graphics, track, trackRadius, FocusGlowColor, FocusGlowSpread, FocusGlowLayers);
            }

            // ===== 私有工具 =====
            private RectangleF GetThumbRect()
            {
                int pad = Math.Max(2, Width / 30);
                int w = Width - 1;
                int h = Height - 1;
                int diameter = h - pad * 2;
                float travel = w - pad * 2 - diameter;
                float x = pad + travel * _thumb;
                return new RectangleF(x, pad, diameter, diameter);
            }

            private void StartAnimation(float target)
            {
                if (!EnableAnimation)
                {
                    _thumb = target;
                    _animTimer.Stop();
                    Invalidate();
                    return;
                }
                _from = _thumb;
                _to = target;
                _animTicks = 0;
                _animTimer.Start();
            }

            private void AnimTimer_Tick(object? sender, EventArgs e)
            {
                _animTicks++;
                float t = Math.Min(1f, _animTicks / (float)_animFrames);
                t = 1f - (1f - t) * (1f - t);  // ease-out
                _thumb = _from + (_to - _from) * t;
                Invalidate();
                if (t >= 1f) _animTimer.Stop();
            }

            private static GraphicsPath RoundedRect(RectangleF rect, float radius)
            {
                float r = Math.Min(radius, Math.Min(rect.Width, rect.Height));
                float d = r;
                var path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            private void DrawFocusGlow(Graphics g, RectangleF baseRect, int baseRadius, Color glowColor, int spread, int layers)
            {
                for (int i = layers; i >= 1; i--)
                {
                    float t = (float)i / layers;
                    int alpha = (int)(80 * t);
                    int inflate = (int)Math.Round(spread * t);
                    int width = Math.Max(1, (int)Math.Round(2 * t));

                    var c = Color.FromArgb(alpha, glowColor);
                    var r = RectangleF.Inflate(baseRect, inflate, inflate);
                    int radius = baseRadius + inflate;

                    using (var path = RoundedRect(r, radius))
                    using (var pen = new Pen(c, width))
                        g.DrawPath(pen, path);
                }
            }
        }

    }
}


       