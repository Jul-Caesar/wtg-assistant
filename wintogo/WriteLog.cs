using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
namespace wintogo
{
    public partial class Log
    {
        public static void WriteLog(string LogName, string WriteInfo)
        {

            if (File.Exists(Application.StartupPath + "\\logs\\" + LogName)) { File.Delete(Application.StartupPath + "\\logs\\" + LogName); }
            FileStream fs0 = new FileStream(Application.StartupPath + "\\logs\\" + LogName, FileMode.Create, FileAccess.Write);
            fs0.SetLength(0);
            StreamWriter sw0 = new StreamWriter(fs0, Encoding.Default);
            string ws0 = "";
            try
            {
                ws0 = Application.ProductName + Application.ProductVersion;
                sw0.WriteLine(ws0);
                ws0 = System.DateTime.Now.ToString();
                sw0.WriteLine(ws0);
                ws0 = WriteInfo;
                sw0.WriteLine(ws0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            sw0.Close();


        }

    }
}