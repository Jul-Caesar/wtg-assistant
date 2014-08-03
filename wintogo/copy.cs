using System;
using System.Threading;
using System.Windows.Forms;
namespace wintogo
{
    public partial class copy : Form
    {
        Thread copyfile;
        String udisk;
        public copy(string ud)
        {
            InitializeComponent();
            udisk = ud;
        }

        private void copy_Load(object sender, EventArgs e)
        {
            copyfile = new Thread(new ThreadStart(copyfiles));
            copyfile.Start();
            
        
        }
        private void copyfiles() 
        {
            //MessageBox.Show("hekl");
            if (System.IO.File.Exists(System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhd"))
            {
                System.Diagnostics.Process cp = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\fastcopy.exe", " /auto_close \"" + System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhd\" /to=\"" + udisk + "\"");
                cp.WaitForExit();
            }
            else 
            {
                System.Diagnostics.Process cp = System.Diagnostics.Process.Start(Application.StartupPath+"\\files" + "\\fastcopy.exe", " /auto_close \"" + System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhdx\" /to=\"" + udisk + "\"");
                cp.WaitForExit();
            }
            //////////////////////////////////////////////////////////////
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (DialogResult.No == MessageBox.Show("确认取消？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }

            System.Diagnostics.Process.Start("cmd.exe", "/c taskkill /f /IM fastcopy.exe");
        }
    }
}
