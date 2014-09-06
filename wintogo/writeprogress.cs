using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace wintogo
{
    public partial class writeprogress : Form
    {
        public writeprogress()
        {
            InitializeComponent();
        }

        private void writeprogress_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //if (System.IO.Directory .Exists ())
                FileStream fs = new FileStream(Application.StartupPath + "\\logs\\" + DateTime.Now.ToFileTime() + ".log", FileMode.Create, FileAccess.Write);
                fs.SetLength(0);
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                string ws = "";
                ws = Application.StartupPath + "\r\n程序版本："+Application.ProductVersion+"\r\n" + System.DateTime.Now;
                sw.WriteLine(ws);
                try
                {
                    ws = textBox1.Text;
                    sw.WriteLine(ws);
                }
                catch { }
                sw.Close();
            }
            catch { }

        }

        private void writeprogress_Load(object sender, EventArgs e)
        {
            //textBox1.Text = "";
        }
    }
}
