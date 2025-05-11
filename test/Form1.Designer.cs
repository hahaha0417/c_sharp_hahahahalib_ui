namespace test
{
    partial class Form_Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            roundToggleButton1 = new hahahalib.ui.RoundToggleButton();
            roundTextBox1 = new hahahalib.ui.RoundTextBox();
            roundComboBox1 = new hahahalib.ui.RoundComboBox();
            roundButton1 = new hahahalib.ui.RoundButton();
            roundFrameButton1 = new hahahalib.ui.RoundFrameButton();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(roundFrameButton1);
            panel1.Controls.Add(roundToggleButton1);
            panel1.Controls.Add(roundTextBox1);
            panel1.Controls.Add(roundComboBox1);
            panel1.Controls.Add(roundButton1);
            panel1.Location = new Point(31, 22);
            panel1.Name = "panel1";
            panel1.Size = new Size(543, 371);
            panel1.TabIndex = 0;
            // 
            // roundToggleButton1
            // 
            roundToggleButton1.BorderColor = Color.Black;
            roundToggleButton1.BorderThickness = 5;
            roundToggleButton1.CornerRadius = 25;
            roundToggleButton1.ForeColor = Color.White;
            roundToggleButton1.HoverBackColor = Color.DarkGray;
            roundToggleButton1.IsToggled = false;
            roundToggleButton1.Location = new Point(238, 99);
            roundToggleButton1.Name = "roundToggleButton1";
            roundToggleButton1.NormalColor = Color.Gray;
            roundToggleButton1.Size = new Size(105, 43);
            roundToggleButton1.SliderColor = Color.White;
            roundToggleButton1.SliderHoverColor = Color.LightYellow;
            roundToggleButton1.TabIndex = 3;
            roundToggleButton1.Text = "roundToggleButton1";
            roundToggleButton1.ToggledBackColor = Color.Green;
            roundToggleButton1.UseHoverEffect = true;
            // 
            // roundTextBox1
            // 
            roundTextBox1.BorderColor = Color.LightGreen;
            roundTextBox1.BorderRadius = 10;
            roundTextBox1.BorderWidth = 10;
            roundTextBox1.FocusBorderColor = Color.LimeGreen;
            roundTextBox1.Font = new Font("Microsoft JhengHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point, 136);
            roundTextBox1.Location = new Point(238, 28);
            roundTextBox1.MouseOverBorderColor = Color.MediumSpringGreen;
            roundTextBox1.Name = "roundTextBox1";
            roundTextBox1.Size = new Size(104, 44);
            roundTextBox1.TabIndex = 2;
            roundTextBox1.TextAlign = HorizontalAlignment.Left;
            roundTextBox1.TextBoxBackColor = Color.White;
            roundTextBox1.TextBoxText = "";
            roundTextBox1.TextColor = Color.Black;
            roundTextBox1.TextFont = new Font("Arial", 24F, FontStyle.Regular, GraphicsUnit.Point, 0);
            // 
            // roundComboBox1
            // 
            roundComboBox1.BackColor = SystemColors.Control;
            roundComboBox1.BorderColor = SystemColors.Control;
            roundComboBox1.BorderRadius = 10;
            roundComboBox1.BorderWidth = 10;
            roundComboBox1.EnableGlowEffect = true;
            roundComboBox1.FocusedBorderColor = Color.LimeGreen;
            roundComboBox1.Font = new Font("Microsoft JhengHei UI", 24F, FontStyle.Regular, GraphicsUnit.Point, 136);
            roundComboBox1.ForeColor = Color.Black;
            roundComboBox1.GlowColor = Color.LimeGreen;
            roundComboBox1.InnerPadding = new Padding(2);
            roundComboBox1.Items = new string[]
    {
    "測試1",
    "測試2"
    };
            roundComboBox1.Location = new Point(34, 195);
            roundComboBox1.Name = "roundComboBox1";
            roundComboBox1.SelectedItem = null;
            roundComboBox1.ShowBorder = false;
            roundComboBox1.Size = new Size(145, 73);
            roundComboBox1.TabIndex = 1;
            roundComboBox1.Load += roundComboBox1_Load;
            // 
            // roundButton1
            // 
            roundButton1.BackColor = Color.Transparent;
            roundButton1.Font = new Font("Arial", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            roundButton1.ForeColor = Color.White;
            roundButton1.HoverColorEnd = Color.MediumSeaGreen;
            roundButton1.HoverColorStart = Color.ForestGreen;
            roundButton1.Location = new Point(34, 18);
            roundButton1.Name = "roundButton1";
            roundButton1.NormalColorEnd = Color.LightGreen;
            roundButton1.NormalColorStart = Color.LimeGreen;
            roundButton1.PressedColorEnd = Color.DarkOliveGreen;
            roundButton1.PressedColorStart = Color.SeaGreen;
            roundButton1.Size = new Size(122, 57);
            roundButton1.TabIndex = 0;
            roundButton1.Text = "測試";
            // 
            // roundFrameButton1
            // 
            roundFrameButton1.BackColor = Color.Transparent;
            roundFrameButton1.BorderColor = Color.DarkGreen;
            roundFrameButton1.BorderInset = 4;
            roundFrameButton1.BorderWidth = 10;
            roundFrameButton1.Font = new Font("Arial", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            roundFrameButton1.ForeColor = Color.White;
            roundFrameButton1.HoverColorEnd = Color.MediumSeaGreen;
            roundFrameButton1.HoverColorStart = Color.ForestGreen;
            roundFrameButton1.Location = new Point(34, 99);
            roundFrameButton1.Name = "roundFrameButton1";
            roundFrameButton1.NormalColorEnd = Color.LightGreen;
            roundFrameButton1.NormalColorStart = Color.LimeGreen;
            roundFrameButton1.PressedColorEnd = Color.DarkOliveGreen;
            roundFrameButton1.PressedColorStart = Color.SeaGreen;
            roundFrameButton1.Size = new Size(122, 67);
            roundFrameButton1.TabIndex = 4;
            roundFrameButton1.Text = "測試";
            // 
            // Form_Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(panel1);
            Name = "Form_Main";
            Text = "測試";
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
       
        private hahahalib.ui.RoundToggleButton roundToggleButton1;
        private hahahalib.ui.RoundTextBox roundTextBox1;
        private hahahalib.ui.RoundComboBox roundComboBox1;
        private hahahalib.ui.RoundButton roundButton1;
        private hahahalib.ui.RoundFrameButton roundFrameButton1;
    }
}
