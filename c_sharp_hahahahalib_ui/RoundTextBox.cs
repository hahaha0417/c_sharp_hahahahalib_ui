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
        

        public class RoundTextBox : UserControl
        {
            private TextBox textBox;

            private Color borderColor = Color.LightGreen;
            private Color mouseOverBorderColor = Color.MediumSpringGreen;
            private Color focusBorderColor = Color.LimeGreen;

            private int borderRadius = 10;
            private int borderWidth = 2;

            private bool isMouseOver = false;
            private bool isFocused = false;

            public RoundTextBox()
            {
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();

                textBox = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    Multiline = true,
                    WordWrap = true,
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                    TextAlign = HorizontalAlignment.Left,
                    Font = new Font("Arial", 12)
                };

                this.Controls.Add(textBox);
                this.Resize += (s, e) => UpdateLayout();

                this.MouseEnter += (s, e) => { isMouseOver = true; Invalidate(); };
                this.MouseLeave += (s, e) => { isMouseOver = false; Invalidate(); };
                textBox.MouseEnter += (s, e) => { isMouseOver = true; Invalidate(); };
                textBox.MouseLeave += (s, e) => { isMouseOver = false; Invalidate(); };

                textBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
                textBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };
            }

            private void UpdateLayout()
            {
                AdjustSizeForFont();
                int inset = borderWidth + 1;
                textBox.Location = new Point(inset, inset);
                textBox.Size = new Size(ClientSize.Width - inset * 2, ClientSize.Height - inset * 2);
                Invalidate();
            }

            // 屬性：顏色控制
            public Color BorderColor
            {
                get => borderColor;
                set { borderColor = value; Invalidate(); }
            }

            public Color MouseOverBorderColor
            {
                get => mouseOverBorderColor;
                set { mouseOverBorderColor = value; Invalidate(); }
            }

            public Color FocusBorderColor
            {
                get => focusBorderColor;
                set { focusBorderColor = value; Invalidate(); }
            }

            // 屬性：樣式與內容
            public int BorderRadius
            {
                get => borderRadius;
                set { borderRadius = value; Invalidate(); }
            }

            public int BorderWidth
            {
                get => borderWidth;
                set { borderWidth = value; Invalidate(); }
            }

            public Font TextFont
            {
                get => textBox.Font;
                set { textBox.Font = value; AdjustSizeForFont(); Invalidate(); }
            }

            public Color TextColor
            {
                get => textBox.ForeColor;
                set { textBox.ForeColor = value; }
            }

            public Color TextBoxBackColor
            {
                get => textBox.BackColor;
                set { textBox.BackColor = value; }
            }

            public HorizontalAlignment TextAlign
            {
                get => textBox.TextAlign;
                set { textBox.TextAlign = value; }
            }

            public string TextBoxText
            {
                get => textBox.Text;
                set => textBox.Text = value;
            }

            private void AdjustSizeForFont()
            {
                if (textBox.Font == null) return;
                var fontHeight = TextRenderer.MeasureText("A", textBox.Font).Height;
                this.Height = fontHeight + 2 * (borderWidth + 2);
            }

            private void DrawRoundedBorder(Graphics g)
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    int inset = borderWidth / 2;
                    Rectangle rect = new Rectangle(inset, inset, this.Width - borderWidth, this.Height - borderWidth);

                    int radius = borderRadius * 2;
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    Color colorToUse = borderColor;
                    if (isFocused)
                        colorToUse = focusBorderColor;
                    else if (isMouseOver)
                        colorToUse = mouseOverBorderColor;

                    using (Pen pen = new Pen(colorToUse, borderWidth))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color parentBack = this.Parent?.BackColor ?? this.BackColor;
                g.Clear(parentBack);

                DrawRoundedBorder(g);
            }
        }
    }
}
