using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace hahahalib
{
    namespace ui
    {
        public class RoundComboBox : UserControl
        {
            private ComboBox comboBox;

            // 屬性
            public Color BorderColor { get; set; }  // 边框颜色，默认与父容器颜色相同
            public Color FocusedBorderColor { get; set; } = Color.LimeGreen;  // 焦点时的边框颜色
            public int BorderRadius { get; set; } = 10;  // 圆角半径
            public int BorderWidth { get; set; } = 2;  // 边框宽度
            public bool ShowBorder { get; set; } = true;  // 控制是否显示边框，默认为显示

            private Padding innerPadding = new Padding(2);
            public Padding InnerPadding
            {
                get => innerPadding;
                set { innerPadding = value; PerformLayout(); }
            }

            // 控制是否启用光晕效果
            public bool EnableGlowEffect { get; set; } = true;

            // 光晕的颜色
            public Color GlowColor { get; set; } = Color.LimeGreen;  // 光晕颜色，亮绿色

            public string[] Items
            {
                get => comboBox.Items.Cast<string>().ToArray();
                set { comboBox.Items.Clear(); comboBox.Items.AddRange(value); }
            }

            public string SelectedItem
            {
                get => comboBox.SelectedItem?.ToString();
                set => comboBox.SelectedItem = value;
            }

            private bool isFocused = false;

            public RoundComboBox()
            {
                this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

                comboBox = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                    Font = this.Font,
                    IntegralHeight = false,
                    Margin = new Padding(0),
                };

                comboBox.FontChanged += (s, e) => this.PerformLayout();
                comboBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
                comboBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };

                this.Controls.Add(comboBox);
                this.Controls.SetChildIndex(comboBox, 0);

                this.BackColor = Color.White;  // 背景颜色设置为白色
                this.ForeColor = Color.Black;

                // 设置默认的边框颜色为父容器的背景颜色
                BorderColor = this.Parent?.BackColor ?? Color.White;
            }

            // 监听父容器背景色的变化，更新边框颜色
            protected override void OnParentChanged(EventArgs e)
            {
                base.OnParentChanged(e);

                // 如果父容器存在，更新 BorderColor 为父容器的背景颜色
                if (this.Parent != null)
                {
                    BorderColor = this.Parent.BackColor;
                    Invalidate();  // 强制重绘控件
                }
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                comboBox.Font = this.Font;
                this.PerformLayout();
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);

                Size preferred = comboBox.PreferredSize;

                // 設定 ComboBox 大小與位置
                int x = BorderWidth + InnerPadding.Left;
                int y = BorderWidth + InnerPadding.Top;
                comboBox.Location = new Point(x, y);
                comboBox.Size = preferred;

                // 控制整體大小
                int totalWidth = preferred.Width + InnerPadding.Horizontal + 2 * BorderWidth;
                int totalHeight = preferred.Height + InnerPadding.Vertical + 2 * BorderWidth;
                this.Size = new Size(totalWidth, totalHeight);
                this.BackColor = SystemColors.Control;

                UpdateRegion();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateRegion();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

                using (GraphicsPath path = GetRoundedRectanglePath(rect, BorderRadius))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    using (SolidBrush brush = new SolidBrush(this.BackColor))  // 使用背景色填充
                        e.Graphics.FillPath(brush, path);

                    // 画光晕效果（仅在启用光晕并且获得焦点时）
                    if (EnableGlowEffect && isFocused)
                    {
                        // 使用 PathGradientBrush 绘制渐变光晕效果
                        using (PathGradientBrush glowBrush = new PathGradientBrush(path))
                        {
                            glowBrush.CenterColor = GlowColor;  // 光晕中心颜色
                            glowBrush.SurroundColors = new Color[] { this.BackColor };  // 外圈与背景色一致
                            glowBrush.FocusScales = new PointF(0.4f, 0.4f);  // 调整焦点范围，减少光晕的大小

                            // 光晕绘制
                            e.Graphics.FillPath(glowBrush, path);
                        }
                    }

                    // 如果 ShowBorder 为 true，则绘制边框
                    if (ShowBorder)
                    {
                        // 如果启用边框，绘制边框
                        if (isFocused)
                        {
                            using (Pen pen = new Pen(FocusedBorderColor, BorderWidth))  // 使用焦点时的亮绿色边框
                            {
                                pen.Alignment = PenAlignment.Inset;
                                e.Graphics.DrawPath(pen, path);
                            }
                        }
                        else
                        {
                            using (Pen pen = new Pen(BorderColor, BorderWidth))  // 默认边框颜色
                            {
                                pen.Alignment = PenAlignment.Inset;
                                e.Graphics.DrawPath(pen, path);
                            }
                        }
                    }
                }
            }

            private void UpdateRegion()
            {
                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                using (GraphicsPath path = GetRoundedRectanglePath(rect, BorderRadius))
                {
                    this.Region?.Dispose();
                    this.Region = new Region(path);
                }
            }

            private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();

                if (radius <= 0)
                {
                    path.AddRectangle(rect);
                    return path;
                }

                int diameter = radius * 2;
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        
    }
}
