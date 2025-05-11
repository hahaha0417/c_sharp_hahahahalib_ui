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
        public class RoundFrameButton : Control
        {
            // 定义按钮的渐变色的起始和结束颜色（使用亮绿色组合）
            private Color normalColorStart = Color.LimeGreen;
            private Color normalColorEnd = Color.LightGreen;
            private Color hoverColorStart = Color.ForestGreen;
            private Color hoverColorEnd = Color.MediumSeaGreen;
            private Color pressedColorStart = Color.SeaGreen;
            private Color pressedColorEnd = Color.DarkOliveGreen;

            // 定义边框的颜色和宽度
            private Color borderColor = Color.DarkGreen;
            private int borderWidth = 2; // 默认边框宽度为2
            private int borderInset = 4; // 边框内缩的距离

            private bool isHover = false;
            private bool isPressed = false;

            // 正常状态颜色渐变的起始和结束颜色属性
            public Color NormalColorStart
            {
                get { return normalColorStart; }
                set { normalColorStart = value; Invalidate(); }
            }

            public Color NormalColorEnd
            {
                get { return normalColorEnd; }
                set { normalColorEnd = value; Invalidate(); }
            }

            // 悬停状态颜色渐变的起始和结束颜色属性
            public Color HoverColorStart
            {
                get { return hoverColorStart; }
                set { hoverColorStart = value; Invalidate(); }
            }

            public Color HoverColorEnd
            {
                get { return hoverColorEnd; }
                set { hoverColorEnd = value; Invalidate(); }
            }

            // 按下状态颜色渐变的起始和结束颜色属性
            public Color PressedColorStart
            {
                get { return pressedColorStart; }
                set { pressedColorStart = value; Invalidate(); }
            }

            public Color PressedColorEnd
            {
                get { return pressedColorEnd; }
                set { pressedColorEnd = value; Invalidate(); }
            }

            // 边框颜色属性
            public Color BorderColor
            {
                get { return borderColor; }
                set { borderColor = value; Invalidate(); }
            }

            // 边框宽度属性，默认值为2
            public int BorderWidth
            {
                get { return borderWidth; }
                set { borderWidth = value; Invalidate(); }
            }

            // 边框内缩属性
            public int BorderInset
            {
                get { return borderInset; }
                set { borderInset = value; Invalidate(); }
            }

            public RoundFrameButton()
            {
                // 设置样式，启用双缓冲和透明背景
                this.SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.SupportsTransparentBackColor,
                    true);

                this.BackColor = Color.Transparent;
                this.ForeColor = Color.White;  // 默认文字颜色
                this.Font = new Font("Arial", 10, FontStyle.Bold); // 默认字体
                this.Size = new Size(100, 40); // 默认大小
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // 清除背景为父容器背景色
                if (this.Parent != null)
                    e.Graphics.Clear(this.Parent.BackColor);
                else
                    e.Graphics.Clear(SystemColors.Control);

                // 根据按钮的状态选择渐变色
                LinearGradientBrush brush = null;
                Rectangle rect = this.ClientRectangle;
                GraphicsPath path = new GraphicsPath();

                int radius = 30;
                path.AddArc(borderInset, borderInset, radius, radius, 180, 90);
                path.AddArc(rect.Width - radius - borderInset, borderInset, radius, radius, 270, 90);
                path.AddArc(rect.Width - radius - borderInset, rect.Height - radius - borderInset, radius, radius, 0, 90);
                path.AddArc(borderInset, rect.Height - radius - borderInset, radius, radius, 90, 90);
                path.CloseFigure();

                this.Region = new Region(path);

                // 创建渐变色刷子
                if (isPressed)
                {
                    // 按下状态的渐变
                    brush = new LinearGradientBrush(rect, pressedColorStart, pressedColorEnd, LinearGradientMode.Vertical);
                }
                else if (isHover)
                {
                    // 悬停状态的渐变
                    brush = new LinearGradientBrush(rect, hoverColorStart, hoverColorEnd, LinearGradientMode.Vertical);
                }
                else
                {
                    // 默认状态的渐变
                    brush = new LinearGradientBrush(rect, normalColorStart, normalColorEnd, LinearGradientMode.Vertical);
                }

                // 填充按钮背景颜色
                e.Graphics.FillPath(brush, path);

                // 绘制文字
                using (Brush textBrush = new SolidBrush(this.ForeColor))
                {
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(this.Text, this.Font, textBrush, rect, format);
                }

                // 绘制按钮边框
                if (borderWidth > 0)
                {
                    using (Pen borderPen = new Pen(borderColor, borderWidth))
                    {
                        borderPen.StartCap = LineCap.Round;
                        borderPen.EndCap = LineCap.Round;
                        borderPen.LineJoin = LineJoin.Round;

                        // 使用内缩后的矩形绘制边框
                        Rectangle borderRect = new Rectangle(rect.X + borderInset, rect.Y + borderInset, rect.Width - 2 * borderInset, rect.Height - 2 * borderInset);
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }

                // 清理资源
                brush.Dispose();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                isHover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                isHover = false;
                isPressed = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                isPressed = true;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                isPressed = false;
                Invalidate();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (!this.ClientRectangle.Contains(e.Location) && isHover)
                {
                    isHover = false;
                    Invalidate();
                }
            }
        }
    }
}
