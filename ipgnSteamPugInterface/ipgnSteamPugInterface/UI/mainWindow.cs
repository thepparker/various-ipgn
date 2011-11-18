using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ipgnSteamPugInterface
{
    public partial class mainWindow : Form
    {
        public mainWindow()
        {
            InitializeComponent();
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("iPGN Steam Interface Bot written by bladez.\n"
                            + "purpose : stuff");
        }
    }
}
