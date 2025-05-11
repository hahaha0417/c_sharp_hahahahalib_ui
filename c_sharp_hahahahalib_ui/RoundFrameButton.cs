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
        
		public class ToggleButton : Control
		{
			private bool isToggled;
			private Rectangle sliderRectangle;
			private Timer animationTimer;
			private int animationSpeed = 10;
			private bool isHovered = false;

			private Color sliderColor = Color.White;
			private Color sliderHoverColor = Color.LightYellow; // 滑塊懸停顏色
			private Color normalColor = Color.Gray;
			private Color toggledBackColor = Color.Green;

			private Color borderColor = Color.Black;
			private int borderThickness = 2;
			private int cornerRadius = 25;

			private Color hoverBackColor = Color.DarkGray;
			private bool useHoverEffect = true;

			public event EventHandler CheckedChanged;

			public ToggleButton()
			{
				this.Size = new Size(100, 50);
				this.ForeColor = Color.White;
				this.Cursor = Cursors.Hand;
				this.sliderRectangle = new Rectangle(3, 3, this.Height - 6, this.Height - 6);

				// ✅ 啟用雙緩衝以防閃爍
				this.SetStyle(ControlStyles.AllPaintingInWmPaint |
							  ControlStyles.OptimizedDoubleBuffer |
							  ControlStyles.ResizeRedraw |
							  ControlStyles.UserPaint, true);
				this.UpdateStyles();

				animationTimer = new Timer();
				animationTimer.Interval = 10;
				animationTimer.Tick += AnimationTimer_Tick;

				this.Click += ToggleButton_Click;
				this.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
				this.MouseLeave += (s, e) => { isHovered = false; Invalidate(); };

				SetButtonRegion();
			}

			private void SetButtonRegion()
			{
				// 使用圓形區域
				if (this.Width == this.Height)
				{
					using (GraphicsPath path = new GraphicsPath())
					{
						path.AddEllipse(0, 0, this.Width, this.Height);
						this.Region = new Region(path);
					}
				}
				else
				{
					using (GraphicsPath path = new GraphicsPath())
					{
						path.AddArc(0, 0, cornerRadius, cornerRadius, 180, 90);
						path.AddArc(this.Width - cornerRadius, 0, cornerRadius, cornerRadius, 270, 90);
						path.AddArc(this.Width - cornerRadius, this.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
						path.AddArc(0, this.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
						path.CloseAllFigures();
						this.Region = new Region(path);
					}
				}
			}

			private void ToggleButton_Click(object sender, EventArgs e)
			{
				if (animationTimer.Enabled) return;

				isToggled = !isToggled;
				animationTimer.Start();
				CheckedChanged?.Invoke(this, EventArgs.Empty);
				Invalidate();
			}

			private void AnimationTimer_Tick(object sender, EventArgs e)
			{
				int targetX = isToggled ? this.Width - sliderRectangle.Width - 3 : 3;

				if (sliderRectangle.X < targetX)
				{
					sliderRectangle.X += animationSpeed;
					if (sliderRectangle.X > targetX) sliderRectangle.X = targetX;
				}
				else if (sliderRectangle.X > targetX)
				{
					sliderRectangle.X -= animationSpeed;
					if (sliderRectangle.X < targetX) sliderRectangle.X = targetX;
				}

				if (sliderRectangle.X == targetX)
				{
					animationTimer.Stop();
				}

				Invalidate();
			}

			private void DrawRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
			{
				using (GraphicsPath path = new GraphicsPath())
				{
					path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
					path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
					path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
					path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
					path.CloseAllFigures();
					g.FillPath(brush, path);
				}
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				// 內縮比例，防止鋸齒
				int inset = 2;

				// 根據是否被點擊或滑鼠是否移入選擇背景顏色
				Color backColorToUse = normalColor;
				if (isHovered && useHoverEffect)
					backColorToUse = hoverBackColor;
				if (isToggled)
					backColorToUse = toggledBackColor;

				// 設定內縮後的矩形範圍
				Rectangle backgroundRect = new Rectangle(inset, inset, this.Width - 2 * inset, this.Height - 2 * inset);

				using (Brush bgBrush = new SolidBrush(backColorToUse))
				{
					DrawRoundedRectangle(e.Graphics, bgBrush, backgroundRect, cornerRadius);
				}

				// Toggled 狀態左半
				if (isToggled)
				{
					int leftWidth = (int)(this.Width * 0.6) - inset; // 左半部分內縮
					Rectangle leftHalf = new Rectangle(inset, inset, leftWidth, this.Height - 2 * inset);
					using (Brush toggledBrush = new SolidBrush(toggledBackColor))
					{
						DrawRoundedRectangle(e.Graphics, toggledBrush, leftHalf, cornerRadius);
					}
				}

				// 滑塊顏色：根據滑鼠是否懸停來變化
				Color sliderBrushColor = isHovered ? sliderHoverColor : sliderColor;
				Rectangle sliderRect = new Rectangle(sliderRectangle.X + inset, inset, sliderRectangle.Width, this.Height - 2 * inset);
				using (Brush sliderBrush = new SolidBrush(sliderBrushColor))
				{
					DrawRoundedRectangle(e.Graphics, sliderBrush, sliderRect, sliderRect.Height / 2);
				}

				// 邊框（圓角）
				using (GraphicsPath borderPath = new GraphicsPath())
				{
					if (this.Width == this.Height)
					{
						borderPath.AddEllipse(0, 0, this.Width, this.Height);
					}
					else
					{
						borderPath.AddArc(0, 0, cornerRadius, cornerRadius, 180, 90);
						borderPath.AddArc(this.Width - cornerRadius, 0, cornerRadius, cornerRadius, 270, 90);
						borderPath.AddArc(this.Width - cornerRadius, this.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
						borderPath.AddArc(0, this.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
					}

					borderPath.CloseAllFigures();
					using (Pen borderPen = new Pen(borderColor, borderThickness))
					{
						borderPen.Alignment = PenAlignment.Inset;
						e.Graphics.DrawPath(borderPen, borderPath);
					}
				}
			}

			protected override void OnResize(EventArgs e)
			{
				base.OnResize(e);
				sliderRectangle = new Rectangle(3, 3, this.Height - 6, this.Height - 6);
				SetButtonRegion();
				Invalidate();
			}

			// ===== 公開屬性 =====

			[Browsable(true)]
			[Category("Appearance")]
			public bool IsToggled
			{
				get => isToggled;
				set
				{
					if (isToggled != value)
					{
						isToggled = value;
						sliderRectangle.X = isToggled ? this.Width - sliderRectangle.Width - 3 : 3;
						CheckedChanged?.Invoke(this, EventArgs.Empty);
						Invalidate();
					}
				}
			}

			[Browsable(true)]
			[Category("Appearance")]
			public Color BorderColor
			{
				get => borderColor;
				set { borderColor = value; Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public int BorderThickness
			{
				get => borderThickness;
				set { borderThickness = Math.Max(1, value); Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public int CornerRadius
			{
				get => cornerRadius;
				set { cornerRadius = Math.Max(0, value); SetButtonRegion(); Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public Color NormalColor
			{
				get => normalColor;
				set { normalColor = value; Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public Color ToggledBackColor
			{
				get => toggledBackColor;
				set { toggledBackColor = value; Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public Color SliderColor
			{
				get => sliderColor;
				set { sliderColor = value; Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public Color SliderHoverColor
			{
				get => sliderHoverColor;
				set { sliderHoverColor = value; Invalidate(); }
			}

			[Browsable(true)]
			[Category("Appearance")]
			public Color HoverBackColor
			{
				get => hoverBackColor;
				set { hoverBackColor = value; Invalidate(); }
			}

			[Browsable(true)]
			[Category("Behavior")]
			public bool UseHoverEffect
			{
				get => useHoverEffect;
				set { useHoverEffect = value; Invalidate(); }
			}
		}
        
    }
}
