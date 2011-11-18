namespace ipgnSteamPugInterface
{
    using System.Windows.Forms;
    using System.ComponentModel;

    partial class mainWindow
    {
        /* This form will contain a window showing current chat, basic diagnostics, etc
         * 
         * 
         * 
         */

        private System.ComponentModel.IContainer components = null;

        //clean resources
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ipgnBotCommandBox = new System.Windows.Forms.TextBox();
            this.clearButton = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.aboutButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ipgnBotCommandBox
            // 
            this.ipgnBotCommandBox.BackColor = System.Drawing.SystemColors.WindowText;
            this.ipgnBotCommandBox.ForeColor = System.Drawing.Color.White;
            this.ipgnBotCommandBox.Location = new System.Drawing.Point(1, 0);
            this.ipgnBotCommandBox.Multiline = true;
            this.ipgnBotCommandBox.Name = "ipgnBotCommandBox";
            this.ipgnBotCommandBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ipgnBotCommandBox.Size = new System.Drawing.Size(720, 522);
            this.ipgnBotCommandBox.TabIndex = 0;
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(475, 528);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(69, 21);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(550, 528);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(69, 21);
            this.button2.TabIndex = 2;
            this.button2.Text = "Do shit";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // aboutButton
            // 
            this.aboutButton.Location = new System.Drawing.Point(625, 528);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(69, 21);
            this.aboutButton.TabIndex = 3;
            this.aboutButton.Text = "About";
            this.aboutButton.UseVisualStyleBackColor = true;
            this.aboutButton.Click += new System.EventHandler(this.aboutButton_Click);
            // 
            // mainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 553);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.ipgnBotCommandBox);
            this.Name = "mainWindow";
            this.Text = "iPGN Steam Bot";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainWindow_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion


        private void mainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private TextBox ipgnBotCommandBox;

        public void Print(string printString)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ipgnBotCommandBox.AppendText(printString);
                ipgnBotCommandBox.AppendText("\n");
            });
        }

        private Button clearButton;
        private Button button2;
        private Button aboutButton;
    }
}

