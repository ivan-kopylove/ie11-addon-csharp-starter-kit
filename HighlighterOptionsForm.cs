using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InternetExplorerExtension
{
    public partial class HighlighterOptionsForm : Form
    {
        public HighlighterOptionsForm()
        {
            InitializeComponent();
        }

        public string InputText
        {
            get { return this.textBox1.Text; }
            set { this.textBox1.Text = value; }
        }
    }
}
