using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using iTuner;
using Microsoft.WimgApi;
using System.Xml;
using System.Text.RegularExpressions;
using Trinet.Core.IO.Ntfs;
namespace wintogo
{
    public partial class Form1 : Form
    {
        private UsbManager manager;
        string vhd_size = "";
        string ud;
        string bcdboot;
        string adlink;
        string vpath;
        string win8vhdfile = "win8.vhd";
        string currentlist;
        string win8iso;
        int wimpart;
        int force=0;
        int formwide=623;
        //bool isformat = true ;
        bool usetemp = true;
        bool useiso = false;
        bool allowesd=false;
        bool needcopy;
        //bool win7togo;
        bool shouldcontinue = true;
        public delegate void AppendTextCallback(string text);
        writeprogress wp = new writeprogress();
        delegate void set_Text(string s); //定义委托
        set_Text Set_Text;
        Thread threadad;
        private Thread threadupdate;
        Thread threadreport;
        private Thread threadwrite;
        bool isesd = false;
        int win7togo;
        private void Fixletter(string targetletter, string currentos) 
        {
            byte[] registData;
            RegistryKey hkml = Registry.LocalMachine ;
            RegistryKey software = hkml.OpenSubKey("SYSTEM", false);
            RegistryKey aimdir = software.OpenSubKey("MountedDevices", false);
            registData = (byte[])aimdir.GetValue("\\DosDevices\\" + currentos);
            if (registData != null) 
            {
                SyncCMD("reg.exe load HKU\\TEMP " + currentos + "\\Windows\\System32\\Config\\SYSTEM  > \"" + Application.StartupPath + "\\logs\\loadreg.log\"");
                RegistryKey hklm = Registry.Users  ;
                RegistryKey temp = hklm.OpenSubKey("TEMP", true);
                try
                {
                    temp.DeleteSubKey("MountedDevices");
                }
                catch { }
                RegistryKey wtgreg = temp.CreateSubKey("MountedDevices");
                wtgreg.SetValue("\\DosDevices\\" + targetletter, registData, RegistryValueKind.Binary );
                wtgreg.Close();
                temp.Close();
                    SyncCMD("reg.exe unload HKU\\TEMP > \"" + Application.StartupPath + "\\logs\\unloadreg.log\"");
                
               

                //string code = ToHexString(registData);
                ////for (int i = 0; i < registData.Length; i++) 
                ////{
                ////    code += ToHexString(registData);
                ////}
                //MessageBox.Show(code);
                
            }
        }
        public static string ToHexString(byte[] bytes) // 0xae00cf => "AE00CF "  
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }  

        public Form1()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
        }
        private void set_textboxText(string s)
        {
            
            linkLabel2.Text = s;
            //label4.Visible = true;
            linkLabel2.Visible = true;
        }
        #region REG operation
        private bool IsRegeditExit(string name)
        {
            bool _exit = false;
            string[] subkeyNames;
            RegistryKey hkml = Registry.CurrentUser;
            RegistryKey software = hkml.OpenSubKey("software", true);
            subkeyNames = software.GetSubKeyNames();
            foreach (string keyName in subkeyNames)
            {
                if (keyName == name)
                {
                    _exit = true;
                    return _exit;
                }
            }
            return _exit;
        }
        private string GetRegistData(string name)
        {
            string registData;
            RegistryKey hkml = Registry.CurrentUser;
            RegistryKey software = hkml.OpenSubKey("software", true);
            RegistryKey aimdir = software.OpenSubKey(Application.ProductName, true);
            registData = aimdir.GetValue(name).ToString();
            return registData;
        }
        private void WTRegedit(string name, string tovalue)
        {
            RegistryKey hklm = Registry.CurrentUser;
            RegistryKey software = hklm.OpenSubKey("SOFTWARE", true);
            RegistryKey aimdir = software.CreateSubKey(Application.ProductName);
            aimdir.SetValue(name, tovalue);
        }
        #endregion
        private void ForeachDisk(string path) 
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            try
            {
                foreach(FileInfo  d in dir.GetFiles())
                {
                    MessageBox.Show(d.FullName);
                }

            }
            catch { }
        }
        private void 启动时自动检查更新ToolStripMenuItem_Checked(object sender, EventArgs e)
        {

            if (启动时自动检查更新ToolStripMenuItem.Checked)
            {
                //启动时自动检查更新ToolStripMenuItem.Checked = false;
                WTRegedit("nevercheckupdate", "0");
            }

            if (!启动时自动检查更新ToolStripMenuItem.Checked)
            {
                //启动时自动检查更新ToolStripMenuItem.Checked = true;
                WTRegedit("nevercheckupdate", "1");
            }
        }
        //private void ExecuteCMD(string cmd)
        //{

        //    System.Diagnostics.Process process = new System.Diagnostics.Process();

        //    try
        //    {
        //        process.StartInfo.FileName = "cmd.exe";
        //        process.StartInfo.UseShellExecute = false ;
        //        process.StartInfo.RedirectStandardInput = true;
        //        process.StartInfo.RedirectStandardOutput = true;
        //        process.StartInfo.RedirectStandardError = true;
        //        process.StartInfo.CreateNoWindow = true;

        //        process.Start();
        //        process.StandardInput.WriteLine(cmd);

        //        process.StandardInput.WriteLine("exit");
        //        process.WaitForExit();



        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString ()+"操作失败");
        //    }
        //    finally
        //    {
        //        process.Close();
        //    }
        //}
        private void Checkfiles() 
        
        {
            string[] sw_fl = new string[12]; ;//software filelist
            sw_fl[0]="\\files\\unattend.xml";
            sw_fl[1] = "\\files\\san_policy.xml";
            sw_fl[2] = "\\files\\imagex.exe";
            sw_fl[3] = "\\files\\fbinst.exe";
            sw_fl[4] = "\\files\\FastExt1.dll";
            sw_fl[5] = "\\files\\FastEx64.dll";
            sw_fl[6] = "\\files\\FastCopy.exe";
            sw_fl[7] = "\\files\\bootsect.exe";
            sw_fl[8] = "\\files\\BOOTICE.EXE";
            sw_fl[9] = "\\files\\bcdboot.exe";
            sw_fl[10] = "\\files\\bcdboot7601.exe";
            //sw_fl[11] = "\\files\\osletter7.bat";
            sw_fl[11] = "\\files\\usb.reg";





            for (int i = 1; i < sw_fl.Length; i++) 
            {
                if (!File.Exists(Application.StartupPath + sw_fl[i])) 
                {
                    MessageBox.Show("程序文件不完整，请从官方论坛重新下载解压！\n缺少:" + Application.StartupPath + sw_fl[i], "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    VisitWeb("http://bbs.luobotou.org/thread-761-1-1.html"); 
                    Application.Exit(); 
                }
            }

        }
        private void CleanLockStream() 
        {
                ListFiles(new DirectoryInfo(Application .StartupPath +"\\files"));
        }
        public void ListFiles(FileSystemInfo info)
        {
            try
            {
                if (!info.Exists) return;
                DirectoryInfo dir = info as DirectoryInfo;
                //不是目录
                if (dir == null) return;
                FileSystemInfo[] files = dir.GetFileSystemInfos();
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo file = files[i] as FileInfo;
                    //是文件
                    if (file != null)
                    {
                        //FileInfo file = new FileInfo(@"d:\Hanye.chm");
                        //MessageBox.Show(file.FullName);
                        foreach (AlternateDataStreamInfo s in file.ListAlternateDataStreams())
                        {
                            s.Delete();//删除流
                        }

                        //Console.WriteLine(file.FullName + "\t " + file.Length);
                        //if (file.FullName.Substring(file.FullName.LastIndexOf(".")) == ".jpg")
                        ////此处为显示JPG格式，不加IF可遍历所有格式的文件
                        //{
                        //    //this.list1.Items.Add(file);
                        //    //MessageBox.Show(file.FullName.Substring(file.FullName.LastIndexOf(".")));
                        //}
                    }
                    //对于子目录，进行递归调用
                    else
                    {
                        ListFiles(files[i]);
                    }
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show("NTFS文件流异常\n请放心，此错误不影响正常使用！\n"+ex.ToString(),"警告",MessageBoxButtons .OK ,MessageBoxIcon.Information );
            }
        }
        public string GetFileVersion(string path)
        {
            try
            {
                // Get the file version for the notepad.   
                FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(path);
                return myFileVersionInfo.FileVersion;
                // Print the file name and version number.   
                //textBox1.Text = "File: " + myFileVersionInfo.FileDescription + '\n' +
                //"Version number: " + myFileVersionInfo.FileVersion;
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); return ""; }
        }    

        private void Form1_Load(object sender, EventArgs e)
        {
            //MessageBox.Show(GetFileVersion(System.Environment .GetEnvironmentVariable ("windir")+"\\System32\\dism.exe"));
            Checkfiles();
            CleanLockStream();

            this.Width = (int)((double)this.Width*0.6645);
            bcdboot = "\\bcdboot.exe";
            if (System.Environment.OSVersion.ToString().Contains("5.1")) //XP 禁用功能
            {
                radiovhd.Enabled = false; radiovhdx.Enabled = false; radiochuantong.Checked = true;
                button2.Enabled = false;
                groupBoxadv.Enabled = false;
                checkBoxdiskpart.Checked = false;
                checkBoxdiskpart.Enabled = false;
                bcdboot9200.Checked = false;
                bcdboot7601.Checked = true;
                bcdboot = "\\bcdboot7601.exe";
                label4.Visible = true;
                label5.Visible = true;

            }
            else if (System.Environment.OSVersion.ToString().Contains("6.0")) //vista
            {
                radiovhd.Enabled = false; radiovhdx.Enabled = false; radiochuantong.Checked = true;
                button2.Enabled = false;
                groupBoxadv.Enabled = false;
                checkBoxdiskpart.Checked = false;
                checkBoxdiskpart.Enabled = false;
                bcdboot9200.Checked = false;
                bcdboot7601.Checked = true;
                bcdboot = "\\bcdboot7601.exe";
                label4.Visible = true;
                label5.Visible = true;

            }
            else if (System.Environment.OSVersion.ToString().Contains("6.1"))
            {
                label4.Visible = true;
                label5.Visible = true;
                radiovhd.Checked = true; radiovhdx.Enabled = false;
            } //WIN7
            else if (System.Environment.OSVersion.ToString().Contains("6.2") || System.Environment.OSVersion.ToString().Contains("6.3"))
            {
                radiovhd.Checked = true;
                //WIN8.1 UPDATE1 WIMBOOT
                string dismversion=GetFileVersion(System.Environment.GetEnvironmentVariable("windir") + "\\System32\\dism.exe");
                if (dismversion.Substring(0, 14) == "6.3.9600.17031" || dismversion.Substring(0, 3) == "6.4") 
                { 
                    checkBoxwimboot.Enabled = true;
                    allowesd = true;
                }
            }
            timer1.Start();
            Set_Text = new set_Text(set_textboxText); //实例化
            threadupdate = new Thread(new ThreadStart(update));
            threadupdate.Start();
            threadreport = new Thread(report);
            threadreport.Start();
            threadad = new Thread(ad);
            threadad.Start();
            udlist();
            this.Text += Application.ProductVersion;
        }
        public void Win7REG(string installdrive) 
        {
            //installdriver :ud  such as e:\
            try
            {
                ExecuteCMD("reg.exe", " load HKLM\\sys " + installdrive + "WINDOWS\\system32\\config\\system");
                wp.ShowDialog();
                ExecuteCMD("reg.exe", " import " + Application.StartupPath + "\\files\\usb.reg");
                wp.ShowDialog();
                ExecuteCMD("reg.exe", " unload HKLM\\sys");
                wp.ShowDialog();
                Fixletter("C:", ud.Substring(0, 2));
                //SyncCMD("\""+Application.StartupPath + "\\files\\osletter7.bat\" /targetletter:c /currentos:" + ud.Substring(0, 1) + " > \"" + Application.StartupPath + "\\logs\\osletter7.log\"");
            }
            catch(Exception err) 
            {
                MessageBox.Show("处理WIN7 USB启动时出现问题\n" + err.ToString());
            }
        }
        #region not used USBDRIVER
        //public void USBDrive()
        //{
        //    WindowsImageContainer image1 = new WindowsImageContainer("h:\\sources\\install.wim", WindowsImageContainer.CreateFileMode.OpenExisting, WindowsImageContainer.CreateFileAccess.Read);
            
        //    manager = new UsbManager();
        //    UsbDiskCollection disks = manager.GetAvailableDisks();
        //    foreach (UsbDisk disk in disks)
        //    {
        //        MessageBox.Show(disk.ToString());
        //        //textBox.AppendText(disk.ToString() + CR);
        //    }
        //    //manager.StateChanged += new UsbStateChangedEventHandler(DoStateChanged);

        //}
        private void DoStateChanged(UsbStateChangedEventArgs e)
        {
            MessageBox.Show(e.State.ToString ());

            //foreach (UsbDisk disk in disks)
            //{
            //    MessageBox.Show(disk.ToString());
            //    //textBox.AppendText(disk.ToString() + CR);
            //}

            //textBox.AppendText(e.State + " " + e.Disk.ToString() + CR);
        }

        public static string GetDriveInfoDetail(string driveName)
        {
            WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_DiskDrive  WHERE Name = '{0}'", driveName.Substring(0, 2)));

            ManagementObjectSearcher managerSearch = new ManagementObjectSearcher(wqlObjectQuery);

            List<ulong> driveInfoList = new List<ulong>(2);

            ManagementClass mc = new ManagementClass("Win32_DiskDrive");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mobj in moc)
            {
                MessageBox.Show(mobj["DeviceID"].ToString());


                return (mobj["Index"].ToString());
                //Console.WriteLine("File system: " + mobj["FileSystem"]);

                //Console.WriteLine("Free disk space: " + mobj["FreeSpace"]);

                //Console.WriteLine("Size: " + mobj["Size"]);
            }
            return "ERROR";
        }
        public static string GetDriveWin32_DiskPartition(string driveName)
        {
            //MessageBox.Show(GetDriveInfoDetail(driveName));
            //WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_PhysicalMedia   WHERE Name = '{0}'", GetDriveInfoDetail(driveName)));
            WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_DiskPartition   "));
            ManagementObjectSearcher managerSearch = new ManagementObjectSearcher(wqlObjectQuery);

            List<ulong> driveInfoList = new List<ulong>(2);

            ManagementClass mc = new ManagementClass("Win32_DiskPartition");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mobj in moc)
            {
                //MessageBox.Show("");
                MessageBox.Show(mobj["Name"].ToString());
                //MessageBox.Show("");

                //return (mobj["Model"].ToString());

                //Console.WriteLine("File system: " + mobj["FileSystem"]);

                //Console.WriteLine("Free disk space: " + mobj["FreeSpace"]);

                //Console.WriteLine("Size: " + mobj["Size"]);
            }
            return "ERROR";
        }
        public void Testdrive() 
        {
            
foreach(ManagementObject drive in new ManagementObjectSearcher(
    "select * from Win32_DiskDrive where InterfaceType='USB'").Get())
{
    // associate physical disks with partitions

    foreach(ManagementObject partition in new ManagementObjectSearcher(
        "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive["DeviceID"]+ "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
    {
        Console.WriteLine("Partition=" + partition["Name"]);

        // associate partitions with logical disks (drive letter volumes)

        foreach(ManagementObject disk in new ManagementObjectSearcher(
            "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='"+ partition["DeviceID"]+ "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())
        {
            //MessageBox.Show(disk["Name"].ToString ());
            Console.WriteLine("Disk=" + disk["Name"]);
        }
    }

    // this may display nothing if the physical disk

    // does not have a hardware serial number

    MessageBox.Show ("Serial="+ new ManagementObject("Win32_PhysicalMedia.Tag='"+ drive["DeviceID"] + "'")["SerialNumber"]);
}

        }
        public static string GetDriveWin32_LogicalDisk(string driveName)
        {
            //MessageBox.Show(GetDriveInfoDetail(driveName));
            //WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_PhysicalMedia   WHERE Name = '{0}'", GetDriveInfoDetail(driveName)));
            WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_LogicalDiskToPartition     "));
            ManagementObjectSearcher managerSearch = new ManagementObjectSearcher(wqlObjectQuery);

            List<ulong> driveInfoList = new List<ulong>(2);

            ManagementClass mc = new ManagementClass("Win32_LogicalDiskToPartition");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mobj in moc)
            {
                //MessageBox.Show("");
                MessageBox.Show(mobj["Dependent"].ToString());
                MessageBox.Show(mobj["Antecedent"].ToString());

                
                //MessageBox.Show("");

                //return (mobj["Model"].ToString());

                //Console.WriteLine("File system: " + mobj["FileSystem"]);

                //Console.WriteLine("Free disk space: " + mobj["FreeSpace"]);

                //Console.WriteLine("Size: " + mobj["Size"]);
            }
            return "ERROR";
        }
        #endregion
        public int  iswin7(string wimfile) 
        {
            SyncCMD("\""+Application.StartupPath + "\\files\\imagex.exe\"" + " /info \"" + wimfile + "\" /xml > " + "\""+Application.StartupPath + "\\logs\\wiminfo.xml\"");
            XmlDocument xml = new XmlDocument();

           System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            System.Xml.XmlNodeReader reader = null;
            string strFilename = Application.StartupPath + "\\logs\\wiminfo.xml";
            if (System.IO.File.Exists(strFilename) == false)
            {
                MessageBox.Show(this, strFilename + "WIM文件信息获取失败\n将按WIN8系统安装。", this.Text);
                return 0;
            }
            try
            {
                doc.Load(strFilename);
                reader = new System.Xml.XmlNodeReader(doc);
                while (reader.Read())
                {
                    if (reader.IsStartElement("NAME"
                        ))
                    {

                        //从找到的这个依次往下读取节点
                        System.Xml.XmlNode aa = doc.ReadNode(reader);
                        //MessageBox.Show(aa.InnerText);
                        //MessageBox.Show(aa.InnerText);
                        if (aa.InnerText == "Windows 7 STARTER")
                        {
                          
                            return 1;
                            //break;
                        }
                        else if (aa.InnerText == "Windows 7 HOMEBASIC")
                        {
                            //MessageBox.Show(aa.InnerText); 
                            return 2;
                        }
                        else { return 0; }
                     

                    }
                }
            }
            catch (Exception  ex)
            {
                MessageBox.Show(this, strFilename + "WIM文件分析失败\n将按WIN8系统安装。" + ex.ToString(), this.Text);
                return 0;
            }



            return 0;
        }
        public void udlist()
        {
            string newlist="";
            manager = new UsbManager();
            UsbDiskCollection disks = manager.GetAvailableDisks();
            foreach (UsbDisk disk in disks)
            {
                newlist+=disk.ToString();
                //textBox.AppendText(disk.ToString() + CR);
            }
            if (newlist != currentlist) 
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("请选择可移动设备");
                foreach (UsbDisk disk in disks)
                {
                    comboBox1.Items.Add(disk.ToString());
                    //textBox.AppendText(disk.ToString() + CR);
                }
                currentlist = newlist;
            }
            if (comboBox1.Items.Count != 0)
            {
                comboBox1.SelectedIndex = 0;
            }



        }
            //var drv = System.IO.DriveInfo.GetDrives();
            
            //foreach (var item in drv)
            //{
            //    if (item.DriveType != DriveType.CDRom )
            //    {
            //        string driverletter = item.RootDirectory.ToString();
            //        if (driverletter.Substring(0, 1) != "A" && driverletter.Substring(0, 1) != "C")
            //        {
            //            //MessageBox.Show(GetDriveInfoDetail("F:"));
            //            newlist = newlist + driverletter;
            //            //comboBox1.Items.Add(driverletter + GetDriveInfoDetail(driverletter) + " (" + item.TotalSize / 1073741824 + "GB)");
                        
            //        }

            //    }//获取优盘列表

            //}
            //if (newlist != currentlist) 
            //{
            //    //comboBox1.Items.Clear();
            //    //comboBox1.Items.Add("请选择可移动设备");

            //    //foreach (var item in drv)
            //    //{
            //    //    if (item.DriveType != DriveType.CDRom)
            //    //    {
            //    //        string driverletter = item.RootDirectory.ToString();
            //    //        if (driverletter.Substring(0, 1) != "A" && driverletter.Substring(0, 1) != "C")
            //    //        {
                            
            //    //            //newlist = newlist + driverletter;
            //    //            try { comboBox1.Items.Add(driverletter + " " + item.DriveType  + " (" + item.TotalSize / 1073741824 + "GB)"); }
            //    //            catch { }

            //    //        }

            //    //    }//获取优盘列表

            //    }

                //currentlist = newlist;
//            }
//            //if (comboBox1.Items.Count != 0)
//            //{
//            //    comboBox1.SelectedIndex = 0;
//            //}

//}

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            udlist();
        }
        public static long GetHardDiskSpace(string str_HardDiskName)
        {
            long totalSize = new long();
            //str_HardDiskName = str_HardDiskName;
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
            foreach (System.IO.DriveInfo drive in drives)
            {
                //MessageBox.Show(drive.TotalSize.ToString () );
                if (drive.Name == str_HardDiskName)
                {
                    totalSize = drive.TotalSize / 1024;
                    
                }
            }
            return totalSize;
        }
        public static long GetHardDiskFreeSpace(string str_HardDiskName)
        {
            long totalSize = new long();
            //str_HardDiskName = str_HardDiskName;
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
            foreach (System.IO.DriveInfo drive in drives)
            {
                //MessageBox.Show(drive.TotalSize.ToString () );
                if (drive.Name == str_HardDiskName)
                {
                    totalSize = drive.TotalFreeSpace  / 1024;

                }
            }
            return totalSize;
        }


        public bool IsChina(string CString)
        {
            bool BoolValue = false;
            for (int i = 0; i < CString.Length; i++)
            {
                if (Convert.ToInt32(Convert.ToChar(CString.Substring(i, 1))) > Convert.ToInt32(Convert.ToChar(128)))
                {
                    BoolValue = true;
                }

            }
            return BoolValue;
        }


        #region CORE
        private void gowrite() 
        {
            ud = comboBox1.SelectedItem.ToString().Substring (0,2)+"\\";//优盘
            wimpart = choosepart.part;//读取选择分卷，默认选择第一分卷

            //各种提示
            if (wimbox.Text.Substring(wimbox.Text.Length - 3, 3) != "wim" && wimbox.Text.Substring(wimbox.Text.Length - 3, 3) != "esd")//不是WIM文件
            {
                MessageBox.Show("镜像文件选择错误！请选择install.wim！"); return;
            }
            else
            {
                if (!System.IO.File.Exists(wimbox.Text)) { MessageBox.Show("请选择install.wim文件！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }//文件不存在.
                win8iso = wimbox.Text;
            }


            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择可移动设备！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }//是否选择优盘
            //if (!System.IO.File.Exists(wimbox.Text)) { MessageBox.Show("请选择win8镜像文件或install.wim文件！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (GetHardDiskSpace(ud) <= 12582912) //优盘容量<12 GB提示
            {
                if (DialogResult.No == MessageBox.Show("可移动磁盘容量不足16G，继续写入可能会导致程序出错！您确定要继续吗？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
            }

            if (GetHardDiskSpace(ud) <= 12582912) //优盘容量<12 GB提示
            {
                if (DialogResult.No == MessageBox.Show("可移动磁盘容量不足16G，继续写入可能会导致程序出错！您确定要继续吗？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
            }
            if (GetHardDiskSpace(ud) <= numericUpDown1.Value * 1048576) 
            {
                MessageBox.Show("优盘容量小于VHD设定大小，请修改设置！","错误",MessageBoxButtons .OK ,MessageBoxIcon.Error );
                return;
            }

            if (DialogResult.No == MessageBox.Show("请确认您所选择的 " + ud.Substring(0, 1) + "盘，容量" + GetHardDiskSpace(ud) / 1024 / 1024 + "GB 是将要写入的优盘或移动硬盘\n误格式化，后果自负！", "警告！", MessageBoxButtons.YesNo,MessageBoxIcon.Asterisk )) { return; } 
            if (checkBoxdiskpart.Checked&&!checkBoxuefi.Checked&&!checkBoxuefimbr .Checked  )//勾选重新分区提示
            {
                if (DialogResult.No == MessageBox.Show("您勾选了重新分区，优盘或移动硬盘上的所有文件将被删除！\n注意是整个磁盘，不是一个分区！", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
                if (DialogResult.No == MessageBox.Show("如果您不清楚您在做什么，请立即停止操作！", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }

                diskpart();
            }
            else//普通格式化提示
            {
                if (!checkBoxunformat.Checked)
                {
                    if (DialogResult.No == MessageBox.Show(ud.Substring(0, 1) + "盘将会被格式化，此操作将不可恢复，您确定要继续吗？\n由于写入时间较长，请您耐心等待！\n写入过程中弹出写入可能无效属于正常现象，选是即可。", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
                    //if (DialogResult.No == MessageBox.Show("如果您不清楚您在做什么，请立即停止操作！", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
                }
            }
          



            ///////删除旧LOG文件
            SyncCMD ("cmd.exe /c del /f /s /q \""+Application .StartupPath +"\\logs\\*.*\"");
           //////////////将程序运行信息写入LOG
            Log.WriteLog("Environment.log", "App Version:" + Application.ProductVersion + "\r\nApp Path:" + Application.StartupPath + "\r\nOSVersion:" + System.Environment.OSVersion.ToString() + "\r\nDism Version:" + GetFileVersion(System.Environment.GetEnvironmentVariable("windir") + "\\System32\\dism.exe") + "\r\nWim file:" + wimbox.Text + "\r\nUsb Disk:" + comboBox1.SelectedItem.ToString() + "\r\nClassical:" + radiochuantong.Checked.ToString() + "\r\nVHD:" + radiovhd.Checked.ToString() + "\r\nVHDX:" + radiovhdx.Checked.ToString() + "\r\nRe-Partition:" + checkBoxdiskpart.Checked + "\r\nVHD Size Set:" + numericUpDown1.Value.ToString() + "\r\nFixed VHD:" + checkBoxfixed.Checked.ToString() + "\r\nDonet:" + checkBoxframework.Checked.ToString() + "\r\nDisable-WinRE:" + checkBoxdiswinre.Checked.ToString() + "\r\nBlock Local Disk:" + checkBox_san_policy.Checked.ToString() + "\r\nNoTemp:" + checkBoxnotemp.Checked.ToString() + "\r\nUEFI+GPT:" + checkBoxuefi.Checked.ToString() + "\r\nUEFI+MBR:" + checkBoxuefimbr.Checked.ToString() + "\r\nWIMBOOT:" + checkBoxwimboot.Checked.ToString());

            File .Copy (Environment .GetEnvironmentVariable ("windir")+"\\Logs\\DISM\\dism.log",Application .StartupPath +"\\logs\\dism.log");
            ///////
            //uefi
            // 
            
            if (checkBoxuefi.Checked)
            {
                //UEFI+GPT
                if (System.Environment.OSVersion.ToString().Contains("5.1") || System.Environment.OSVersion.ToString().Contains("5.2")) { MessageBox.Show("XP系统不支持UEFI模式写入！"); return; }
                if (comboBox1.SelectedItem.ToString().Contains("Removable Disk")) 
                { 
                    MessageBox.Show("此优盘不支持UEFI模式\n只有 Fixed Disk格式支持\n详情请看论坛说明！","错误",MessageBoxButtons .OK ,MessageBoxIcon.Error ); 
                    VisitWeb("http://bbs.luobotou.org/thread-6506-1-1.html"); 
                    return;
                }
                if (DialogResult.No == MessageBox.Show("您所选择的是UEFI模式，此模式将会格式化您的整个移动磁盘！\n注意是整个磁盘！！！\n程序将会删除所有优盘分区！\n注意：此模式只能写入64位系统！", "警告！", MessageBoxButtons.YesNo,MessageBoxIcon.Warning )) { return; }
                FileStream fs0 = new FileStream(Application.StartupPath + "\\uefi.txt", FileMode.Create, FileAccess.Write);
                fs0.SetLength(0);
                StreamWriter sw0 = new StreamWriter(fs0, Encoding.Default);
                string ws0 = "";
                try
                {
                    ws0 = "select volume " + ud.Substring (0,1);
                    sw0.WriteLine(ws0);
                    ws0 = "clean";
                    sw0.WriteLine(ws0);
                    ws0 = "convert gpt";
                    sw0.WriteLine(ws0);
                    ws0 = "create partition efi size 350";
                    sw0.WriteLine(ws0);
                    ws0 = "create partition primary";
                    sw0.WriteLine(ws0);
                    ws0 = "select partition 2";
                    sw0.WriteLine(ws0);
                    ws0 = "format fs=fat quick";
                    sw0.WriteLine(ws0);
                    ws0 = "assign letter=x";
                    sw0.WriteLine(ws0);
                    ws0 = "select partition 3";
                    sw0.WriteLine(ws0);
                    ws0 = "format fs=ntfs quick";
                    sw0.WriteLine(ws0);
                    ws0 = "assign letter="+ud.Substring (0,1);
                    sw0.WriteLine(ws0);
                    ws0 = "exit";
                    sw0.WriteLine(ws0);
                }
                catch { }
                sw0.Close();
                ExecuteCMD("diskpart.exe"," /s \"" + Application.StartupPath + "\\uefi.txt\"");
                wp.ShowDialog();
                if (radiochuantong.Checked)
                {//UEFI+GPT 传统
                    //判断是否WIN7，自动选择安装分卷
                    //int win7togo = iswin7(win8iso);
                    if (wimpart==0)
                    {//自动判断模式

                        if (win7togo == 1)
                        {//WIN7 32 bit

                            wimpart = 5;
                        }
                        else if (win7togo == 2)
                        { //WIN7 64 BIT

                            wimpart = 4;
                        }
                        else { wimpart = 1; }
                    }
                    //IMAGEX解压
                    if (checkBoxwimboot.Checked)
                    {
                        ExecuteCMD("Dism.exe", " /Export-Image /WIMBoot /SourceImageFile:\"" + win8iso + "\" /SourceIndex:" + wimpart.ToString() + " /DestinationImageFile:" + ud + "wimboot.wim");
                        wp.ShowDialog();
                        ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + ud + "wimboot.wim" + "\" /ApplyDir:" + ud.Substring(0, 2) + " /Index:" + wimpart.ToString() + " /WIMBoot");
                        wp.ShowDialog();

                    }
                    else
                    {
                        //dism /apply-image /imagefile:9600.17050.winblue_refresh.140317-1640_x64fre_client_Professional_zh-cn-ir3_cpra_x64frer_zh-cn_esd.esd /index:4 /applydir:G:\

                        if (isesd) 
                        {
                            ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + win8iso + "\" /ApplyDir:" + ud.Substring(0, 2) + " /Index:" + wimpart.ToString());
                            wp.ShowDialog();

                        }
                        else
                        {
                            ExecuteCMD(Application.StartupPath + "\\files\\imagex.exe", " /apply " + "\"" + win8iso + "\"" + " " + wimpart.ToString() + " " + ud);
                            wp.ShowDialog();
                        }
                    }
                    //安装EXTRA
                    if (checkBoxframework.Checked)
                    {
                        ExecuteCMD("dism.exe"," /image:" + ud.Substring(0, 2) + " /enable-feature /featurename:NetFX3 /source:" + wimbox.Text.Substring(0, wimbox.Text.Length - 11) + "sxs");
                        wp.ShowDialog();

                    }
                    if (checkBox_san_policy.Checked)//屏蔽本机硬盘
                    {
                        ExecuteCMD("dism.exe", " /image:" + ud.Substring(0, 2) + " /Apply-Unattend:\"" + Application.StartupPath + "\\files\\san_policy.xml\"");
                        wp.ShowDialog();

                    }
                    
                    if (checkBoxdiswinre.Checked)
                    {
                        File.Copy(Application.StartupPath + "\\files\\unattend.xml", ud + "Windows\\System32\\sysprep\\unattend.xml");
                    }
                    //BCDBOOT WRITE BOOT FILE    
                    ExecuteCMD(Application.StartupPath+"\\files\\" + bcdboot , ud + "windows  /s  x: /f UEFI");
                    wp.ShowDialog();

                    ////UEFI卸载X盘
                    //removeletterx();
                    //FileStream fs1 = new FileStream(Application.StartupPath + "\\uefi.txt", FileMode.Create, FileAccess.Write);
                    //fs1.SetLength(0);
                    //StreamWriter sw1 = new StreamWriter(fs0, Encoding.Default);
                    //string ws1 = "";
                    //try
                    //{
                    //    ws0 = "select volume x";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "clean";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "convert gpt";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "create partition efi size 350";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "create partition primary";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "select partition 2";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "format fs=fat quick";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "assign letter=x";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "select partition 3";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "format fs=ntfs quick";
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "assign letter=" + ud.Substring(0, 1);
                    //    sw0.WriteLine(ws0);
                    //    ws0 = "exit";
                    //    sw0.WriteLine(ws0);
                    //}
                    //catch { }
                    //sw1.Close();
                    //ExecuteCMD("diskpart.exe", " /s \"" + Application.StartupPath + "\\uefi.txt\"");
                    //wp.ShowDialog();




                }
                else // UEFI+GPT VHD、VHDX模式
                {
                    if (!shouldcontinue) { return; }

                    cleantemp();
                    if (!shouldcontinue) { return; }

                    createvhd();
                    if (!shouldcontinue) { return; }

                    vhdextra();
                    if (!shouldcontinue) { return; }

                    detachvhd();
                    if (!shouldcontinue) { return; }

                    copyvhd();
                    if (!shouldcontinue) { return; }

                    if (!checkBoxfixed.Checked)
                    {
                        vhd_dynamic_instructions();
                    }

                    if (System.IO.File.Exists(ud + win8vhdfile))
                    {
                        ////finish f = new finish();
                        ////f.ShowDialog();
                    }
                    else
                    {
                        error er = new error("VHD文件创建出错！");
                        er.ShowDialog();
                        //MessageBox.Show("Win8 VHD文件不存在！，可到论坛发帖求助！\n建议将程序目录下logs文件夹打包上传，谢谢！","出错啦！",MessageBoxButtons .OK ,MessageBoxIcon.Error );
                        //System.Diagnostics.Process.Start("http://bbs.luobotou.org/forum-88-1.html");
                    }
                }
                removeletterx();
                finish f = new finish();
                f.ShowDialog();

                //MessageBox.Show("UEFI模式写入完成！\n请重启电脑用优盘启动\n如有问题，可去论坛反馈！","完成啦！",MessageBoxButtons .OK ,MessageBoxIcon.Information );
            }
            else if (checkBoxuefimbr.Checked) 
            {
                //UEFI+MBR
                if (comboBox1.SelectedItem.ToString().Contains("Removable Disk"))
                {
                    MessageBox.Show("此优盘不支持UEFI模式\n只有 Fixed Disk格式支持\n详情请看论坛说明！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    VisitWeb("http://bbs.luobotou.org/thread-6506-1-1.html");
                    return;
                }
                if (DialogResult.No == MessageBox.Show("您所选择的是UEFI模式，此模式将会格式化您的整个移动磁盘！\n注意是整个磁盘！！！\n程序将会删除所有优盘分区！\n注意：此模式只能写入64位系统！", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
                FileStream fs0 = new FileStream(Application.StartupPath + "\\uefimbr.txt", FileMode.Create, FileAccess.Write);
                fs0.SetLength(0);
                StreamWriter sw0 = new StreamWriter(fs0, Encoding.Default);
                string ws0 = "";
                try
                {
                    ws0 = "select volume " + ud.Substring(0, 1);
                    sw0.WriteLine(ws0);
                    ws0 = "clean";
                    sw0.WriteLine(ws0);
                    ws0 = "convert mbr";
                    sw0.WriteLine(ws0);
                    ws0 = "create partition primary size 350";
                    sw0.WriteLine(ws0);
                    ws0 = "create partition primary";
                    sw0.WriteLine(ws0);
                    ws0 = "select partition 1";
                    sw0.WriteLine(ws0);
                    ws0 = "format fs=fat quick";
                    sw0.WriteLine(ws0);
                    ws0 = "assign letter=x";
                    sw0.WriteLine(ws0);
                    ws0 = "select partition 2";
                    sw0.WriteLine(ws0);
                    ws0 = "format fs=ntfs quick";
                    sw0.WriteLine(ws0);
                    ws0 = "assign letter=" + ud.Substring(0, 1);
                    sw0.WriteLine(ws0);
                    ws0 = "exit";
                    sw0.WriteLine(ws0);
                }
                catch { }
                sw0.Close();
                ExecuteCMD("diskpart.exe", " /s \"" + Application.StartupPath + "\\uefimbr.txt\"");
                wp.ShowDialog();
                if (radiochuantong.Checked)
                {
                    //判断是否WIN7，自动选择安装分卷
                    //int win7togo = iswin7(win8iso);
                    if (wimpart == 0)
                    {//自动判断模式

                        if (win7togo == 1)
                        {//WIN7 32 bit

                            wimpart = 5;
                        }
                        else if (win7togo == 2)
                        { //WIN7 64 BIT

                            wimpart = 4;
                        }
                        else { wimpart = 1; }
                    }
                    //IMAGEX解压
                    if (checkBoxwimboot.Checked)
                    {
                        ExecuteCMD("Dism.exe", " /Export-Image /WIMBoot /SourceImageFile:\"" + win8iso + "\" /SourceIndex:" + wimpart.ToString() + " /DestinationImageFile:" + ud + "wimboot.wim");
                        wp.ShowDialog();
                        ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + ud + "wimboot.wim" + "\" /ApplyDir:" + ud.Substring(0, 2) + " /Index:" + wimpart.ToString() + " /WIMBoot");
                        wp.ShowDialog();

                    }
                    else
                    {
                        if (isesd)
                        {
                            ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + win8iso + "\" /ApplyDir:" + ud.Substring(0, 2) + " /Index:" + wimpart.ToString());
                            wp.ShowDialog();
                        }
                        else
                        {
                            ExecuteCMD(Application.StartupPath + "\\files\\imagex.exe", " /apply " + "\"" + win8iso + "\"" + " " + wimpart.ToString() + " " + ud);
                            wp.ShowDialog();
                        }
                    }
                    //安装EXTRA
                    if (checkBoxframework.Checked)
                    {
                        ExecuteCMD("dism.exe", " /image:" + ud.Substring(0, 2) + " /enable-feature /featurename:NetFX3 /source:" + wimbox.Text.Substring(0, wimbox.Text.Length - 11) + "sxs");
                        wp.ShowDialog();

                    }
                    if (checkBox_san_policy.Checked)
                    {
                        ExecuteCMD("dism.exe", " /image:" + ud.Substring(0, 2) + " /Apply-Unattend:\"" + Application.StartupPath + "\\files\\san_policy.xml\"");
                        wp.ShowDialog();

                    }

                    if (checkBoxdiswinre.Checked)
                    {
                        File.Copy(Application.StartupPath + "\\files\\unattend.xml", ud + "Windows\\System32\\sysprep\\unattend.xml");
                    }
                    //BCDBOOT WRITE BOOT FILE    
                    ExecuteCMD(Application.StartupPath + "\\files\\" + bcdboot, ud + "windows  /s  x: /f ALL");
                    wp.ShowDialog();
                    System.Diagnostics.Process p2 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\bootice.exe", " /DEVICE=x: /partitions /activate  /quiet");
                    p2.WaitForExit();

                    //finish f = new finish();
                    //f.ShowDialog();

                }
                else //uefi VHD、VHDX模式
                {
                    if (!shouldcontinue) { return; }

                    cleantemp();
                    if (!shouldcontinue) { return; }

                    createvhd();
                    if (!shouldcontinue) { return; }

                    vhdextra();
                    if (!shouldcontinue) { return; }

                    detachvhd();
                    if (!shouldcontinue) { return; }

                    copyvhd();
                    if (!shouldcontinue) { return; }

                    if (!checkBoxfixed.Checked)
                    {
                        vhd_dynamic_instructions();
                    }
                    if (checkBoxuefimbr.Checked)
                    {
                        System.Diagnostics.Process pbr = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=X: /pbr /install /type=bootmgr /quiet"));//写入引导
                        pbr.WaitForExit();
                        System.Diagnostics.Process act = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\bootice.exe", " /DEVICE=X: /partitions /activate /quiet");
                        act.WaitForExit();
                    }

                    if (System.IO.File.Exists(ud + win8vhdfile))
                    {
                        //finish f = new finish();
                        //f.ShowDialog();
                    }
                    else
                    {
                        error er = new error("Win8 VHD文件不存在！");
                        er.ShowDialog();
                        shouldcontinue = false;
                    }
                }
                finish f = new finish();
                f.ShowDialog();

                //MessageBox.Show("UEFI模式写入完成！\n请重启电脑用优盘启动\n如有问题，可去论坛反馈！", "完成啦！", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else //非UEFI模式
            {
                //传统
                if (!checkBoxdiskpart.Checked && !checkBoxunformat .Checked)//普通格式化
                {
                    ExecuteCMD("cmd.exe", "/c format " + ud.Substring(0, 2) + "/FS:ntfs /q /V: /Y");
                    wp.ShowDialog();


                }
                if (force == 1) //强制格式化
                {
                    System.Diagnostics.Process ud1 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files\\" + "\\fbinst.exe", (" " + ud.Substring(0, 2) + " format -r -f"));//Format disk
                    ud1.WaitForExit();
                }
                ///////////////////////////////////正式开始////////////////////////////////////////////////
                if (radiochuantong.Checked)
                {
                    //int win7togo = iswin7(win8iso);
                    if (wimpart == 0)
                    {//自动判断模式

                        if (win7togo == 1)
                        {//WIN7 32 bit

                            wimpart = 5;
                        }
                        else if (win7togo == 2)
                        { //WIN7 64 BIT

                            wimpart = 4;
                        }
                        else { wimpart = 1; }
                    }
                    if (checkBoxwimboot.Checked)
                    {
                        ExecuteCMD("Dism.exe"," /Export-Image /WIMBoot /SourceImageFile:\""+win8iso+"\" /SourceIndex:"+wimpart .ToString ()+" /DestinationImageFile:"+ud+"wimboot.wim");
                        wp.ShowDialog();
                        ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + ud + "wimboot.wim" + "\" /ApplyDir:" + ud.Substring(0, 2) + " /Index:" + wimpart.ToString() + " /WIMBoot");
                        wp.ShowDialog();

                    }
                    else
                    {
                        if (isesd)
                        {
                            ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + win8iso + "\" /ApplyDir:" + ud.Substring(0, 2) + " /Index:" + wimpart.ToString());
                            wp.ShowDialog();
                        }
                        else
                        {
                            ExecuteCMD(Application.StartupPath + "\\files" + "\\imagex.exe", " /apply " + "\"" + win8iso + "\"" + " " + wimpart.ToString() + " " + ud);
                            wp.ShowDialog();
                        }
                    }
                    /////////////
                    if (win7togo != 0) { Win7REG(ud); }
                    /////////////
                    System.Diagnostics.Process booice = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=" + ud.Substring(0, 2) + " /mbr /install /type=nt60 /quiet"));//写入引导
                    booice.WaitForExit();
                    System.Diagnostics.Process pbr = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=" + ud.Substring(0, 2) + " /pbr /install /type=bootmgr  /quiet"));//写入引导
                    pbr.WaitForExit();
                    System.Diagnostics.Process p2 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\bootice.exe", " /DEVICE=" + ud.Substring(0, 2) + " /partitions /activate  /quiet");
                    p2.WaitForExit();
                    SyncCMD("\"" + Application.StartupPath + "\\files\\" + bcdboot + "\"  " + ud.Substring(0, 3) + "windows  /s  " + ud.Substring(0, 2));


                    /////////////.net3.5//////////////////////
                    if (checkBoxframework.Checked)
                    {
                        if (win7togo == 0)
                        {
                            ExecuteCMD("dism.exe", " /image:" + ud.Substring(0, 2) + " /enable-feature /featurename:NetFX3 /source:" + wimbox.Text.Substring(0, wimbox.Text.Length - 11) + "sxs");
                            wp.ShowDialog();
                        }

                    }
                    ///////////////////////////////////////////
                    if (checkBox_san_policy.Checked)
                    {
                        ExecuteCMD("dism.exe", " /image:" + ud.Substring(0, 2) + " /Apply-Unattend:\"" + Application.StartupPath + "\\files\\san_policy.xml\"");
                        wp.ShowDialog();
                    }
                    ///////////////////////////////////////////
                    if (checkBoxdiswinre.Checked)
                    {
                        File.Copy(Application.StartupPath + "\\files\\unattend.xml", ud + "Windows\\System32\\sysprep\\unattend.xml");
                    }
                    ///////////////////////////////////////////
                    if (!System.IO.File.Exists(ud + "\\Boot\\BCD"))
                    {
                        error er = new error("引导文件写入出错！boot文件夹不存在！");
                        er.ShowDialog();
                        //MessageBox.Show("引导文件写入出错！boot文件夹不存在\n请看论坛教程！", "出错啦", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //System.Diagnostics.Process.Start("http://bbs.luobotou.org/thread-1625-1-1.html");
                    }
                    else if (!System.IO.File.Exists(ud + "bootmgr"))
                    {
                        error er = new error("文件写入出错！bootmgr不存在！\n请检查写入过程是否中断");
                        er.ShowDialog();

                        //MessageBox.Show("文件写入出错！bootmgr不存在！\n请检查写入过程是否中断\n如有疑问，请访问官方论坛！");
                    }
                    else
                    {
                        finish f = new finish();
                        f.ShowDialog();
                    }

                }
                else //非UEFI VHD VHDX
                {
                    if (!shouldcontinue) { return; }
                    cleantemp();
                    if (!shouldcontinue) { return; }

                    createvhd();
                    if (!shouldcontinue) { return; }

                     vhdextra();
                     if (!shouldcontinue) { return; }

                     detachvhd();
                     if (!shouldcontinue) { return; }

                     if (checkBoxcommon.Checked||usetemp)
                     {
                         copyvhdbootfile();
                     }
                     if (!shouldcontinue) { return; }

                    if (!checkBoxfixed.Checked)
                    {
                        vhd_dynamic_instructions();

                    }

                    if (!System.IO.File.Exists(ud + win8vhdfile))
                    {
                        error er = new error("Win8 VHD文件不存在！未知错误原因！");
                        er.ShowDialog();
                        //MessageBox.Show("Win8 VHD文件不存在！可到论坛发帖求助！\n建议将logs文件夹打包上传！");
                        //System.Diagnostics.Process.Start("http://bbs.luobotou.org/forum-88-1.html");                
                    }

                    else if (!System.IO.File.Exists(ud + "\\Boot\\BCD"))
                    {
                        error er = new error("VHD模式下BCDBOOT执行出错！");
                        er.ShowDialog();

                        //MessageBox.Show("VHD模式下BCDBOOT执行出错！\nboot文件夹不存在\n请看论坛教程！","出错啦",MessageBoxButtons .OK ,MessageBoxIcon.Error );
                        //System.Diagnostics.Process.Start("http://bbs.luobotou.org/forum.php?mod=viewthread&tid=8561");
                    }
                    else if (!System.IO.File.Exists(ud + "bootmgr"))
                    {
                        error er = new error("文件写入出错！bootmgr不存在！");
                        er.ShowDialog();

                        //MessageBox.Show("文件写入出错！bootmgr不存在！\n请检查写入过程是否中断\n如有疑问，请访问官方论坛！");
                    }
                    else
                    {
                        finish f = new finish();
                        f.ShowDialog();
                    }


                }
            }
        }
        //private string GetRegistData(string name)
        //{
        //    string registData;
        //    RegistryKey hkml = Registry.LocalMachine;
        //    RegistryKey software = hkml.OpenSubKey("SOFTWARE", true);
        //    RegistryKey aimdir = software.OpenSubKey("XXX", true);
        //    registData = aimdir.GetValue(name).ToString();
        //    return registData;
        //} 
        private void createvhd() 
        {
            ////////////////vhd设定///////////////////////
            string vhd_type = "expandable";
            vhd_size ="";
            if (checkBoxfixed.Checked)
            {
                vhd_type = "fixed";
            }
            if (numericUpDown1.Value != 0)
            {
                vhd_size = (numericUpDown1.Value * 1024).ToString();
            }
            else 
            {
                if (!checkBoxwimboot.Checked)
                {
                    if (GetHardDiskFreeSpace(ud) / 1024 >= 21504) { vhd_size = "20480"; }
                    else { vhd_size = (GetHardDiskFreeSpace(ud) / 1024 - 500).ToString(); }
                }
                else 
                {
                    if (GetHardDiskFreeSpace(ud) / 1024 >= 24576) { vhd_size = "20480"; }
                    else { vhd_size = (GetHardDiskFreeSpace(ud) / 1024 - 4096).ToString(); }
                }
            }
             needcopy = false;
            wimpart = choosepart.part;
            ////win7////
            //int win7togo = iswin7(win8iso);
            //if (win7togo != 0 && radiovhdx.Checked) { MessageBox.Show("WIN7 WTG系统不支持VHDX模式！"); return; }
            if (wimpart == 0)
            {//自动判断模式

                if (win7togo == 1)
                {//WIN7 32 bit

                    wimpart = 5;
                }
                else if (win7togo == 2)
                { //WIN7 64 BIT

                    wimpart = 4;
                }
                else { wimpart = 1; }
            }
            //MessageBox.Show(wimpart.ToString());
            //////////////

            ////////////////判断临时文件夹,VHD needcopy?///////////////////
            int vhdmaxsize;
            if (checkBoxfixed.Checked)
            {
                vhdmaxsize = System.Int32.Parse(vhd_size) * 1048576 + 1024;
            }
            else 
            {
                vhdmaxsize = 10485670;
            }
            if (GetHardDiskFreeSpace(System.Environment.GetEnvironmentVariable("TEMP").Substring(0, 3)) <= vhdmaxsize || IsChina(System.Environment.GetEnvironmentVariable("TEMP")) || !usetemp)
            {
                usetemp = false;
                vpath = ud + win8vhdfile;
            }
            else
            {
                usetemp = true;
                vpath = System.Environment.GetEnvironmentVariable("TEMP") + "\\" + win8vhdfile;
                needcopy = true;
            }
           
          
            /////////////////////////////////////////////////////
            
            FileStream fs = new FileStream(Application.StartupPath + "\\create.txt", FileMode.Create, FileAccess.Write);
            fs.SetLength(0);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            string ws = "";
            //MessageBox.Show((GetHardDiskFreeSpace(ud) / 1024 - 1000).ToString());
            try
            {
                ws = "create vdisk file=" + vpath + " type=" + vhd_type + " maximum=" + vhd_size;
                sw.WriteLine(ws);
                ws = "select vdisk file=" + vpath;
                sw.WriteLine(ws);
                ws = "attach vdisk";
                sw.WriteLine(ws);
                ws = "create partition primary";
                sw.WriteLine(ws);
                ws = "format fs=ntfs quick";
                sw.WriteLine(ws);
                ws = "assign letter=v";
                sw.WriteLine(ws);
                ws = "exit";
                sw.WriteLine(ws);
            }
            catch { }
            sw.Close();
          
                ExecuteCMD("diskpart.exe", " /s \"" + Application.StartupPath + "\\create.txt\"");
                wp.ShowDialog();
            
            try
            {
                if (!System.IO.Directory.Exists("V:\\"))
                {
                    error er = new error("创建VHD文件失败！");
                    er.ShowDialog();
                    shouldcontinue = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                error er = new error("创建VHD文件失败！");
                er.ShowDialog();
                shouldcontinue = false;

            }
            if (checkBoxwimboot.Checked)
            {
                ExecuteCMD("Dism.exe", " /Export-Image /WIMBoot /SourceImageFile:\"" + win8iso + "\" /SourceIndex:" + wimpart.ToString() + " /DestinationImageFile:" + ud + "wimboot.wim");
                wp.ShowDialog();
                ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + ud + "wimboot.wim" + "\" /ApplyDir:v: /Index:" + wimpart.ToString() + " /WIMBoot");
                wp.ShowDialog();

            }
            else 
            {
                if (isesd)
                {
                    ExecuteCMD("Dism.exe", " /Apply-Image /ImageFile:\"" + win8iso + "\" /ApplyDir:v: /Index:"+wimpart.ToString());
                    wp.ShowDialog();

                }
                else
                {
                    ExecuteCMD(Application.StartupPath + "\\files" + "\\imagex.exe", " /apply " + "\"" + win8iso + "\"" + " " + wimpart.ToString() + " " + "v:\\");
                    wp.ShowDialog();
                }
            }

            //////////////
            if (win7togo != 0) { Win7REG("V:\\"); }
            //////////////
            if (checkBoxuefi.Checked) 
            {
                Fixletter("C:", "V:");

                //SyncCMD("\""+Application.StartupPath + "\\files\\osletter7.bat\" /targetletter:c /currentos:v  > \"" + Application.StartupPath + "\\logs\\osletter7.log\"");

            }

            if (!usetemp)
            {
                if (checkBoxuefi.Checked )
                {
                    ExecuteCMD(Application.StartupPath + "\\files\\" + bcdboot, "  " + "V:\\" + "windows  /s  x: /f UEFI");
                    wp.ShowDialog();

                }
                else if (checkBoxuefimbr.Checked) 
                {
                    ExecuteCMD(Application.StartupPath + "\\files\\" + bcdboot, "  " + "V:\\" + "windows  /s  x: /f ALL");
                    wp.ShowDialog();

                }
                else
                {
                    if (!checkBoxcommon.Checked)
                    {
                        ExecuteCMD(Application.StartupPath + "\\files\\" + bcdboot, "  " + "V:\\" + "windows  /s  " + ud.Substring(0, 2) + " /f BIOS");
                        wp.ShowDialog();
                    }
                    else
                    {
                        //copyvhdbootfile();
                    }
                }
            }
        }
        private void removeletterx() 
        {
            FileStream fs0 = new FileStream(Application.StartupPath + "\\removex.txt", FileMode.Create, FileAccess.Write);
            fs0.SetLength(0);
            StreamWriter sw0 = new StreamWriter(fs0, Encoding.Default);
            string ws0 = "";
            try
            {
                ws0 = "select volume x";
                sw0.WriteLine(ws0);
                ws0 = "remove";
                sw0.WriteLine(ws0);
                ws0 = "exit";
                sw0.WriteLine(ws0);
            }
            catch { }
            sw0.Close();
            ExecuteCMD("diskpart.exe", " /s \"" + Application.StartupPath + "\\removex.txt\"");
            wp.ShowDialog();

        }
        private void copyvhd() 
        {
      

            if (needcopy)
            {
                
                copy cp = new copy(ud);
                cp.ShowDialog();
             
            }

        }
        private void vhdextra() 
        {
      
            ////////////.net 3.5//////////////////
            if (checkBoxframework.Checked)
            {
                ExecuteCMD("dism.exe", " /image:v: /enable-feature /featurename:NetFX3 /source:" + wimbox.Text.Substring(0, wimbox.Text.Length - 11) + "sxs");
                wp.ShowDialog();

            }
            /////////////////屏蔽本机硬盘///////////////////////////////////
            if (checkBox_san_policy.Checked)
            {
                ExecuteCMD("dism.exe", " /image:v: /Apply-Unattend:\"" + Application.StartupPath + "\\files\\san_policy.xml\"");
                wp.ShowDialog();
            }
            /////////////////////禁用WINRE//////////////////////////////
            if (checkBoxdiswinre.Checked)
            {
                File.Copy(Application.StartupPath + "\\files\\unattend.xml", "v:\\Windows\\System32\\sysprep\\unattend.xml");
            }
            //////////////
          

        }
        private void detachvhd() 
        {
            if (File.Exists(Application.StartupPath + "\\detach.txt")) { File.Delete(Application.StartupPath + "\\detach.txt"); }
            ///////////////////detach vdisk/////////////////////
            FileStream fs1 = new FileStream(Application.StartupPath + "\\detach.txt", FileMode.Create, FileAccess.Write);
            fs1.SetLength(0);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            string ws = "";
            try
            {
                ws = "select vdisk file=" + vpath;
                sw1.WriteLine(ws);
                ws = "detach vdisk";
                sw1.WriteLine(ws);
            }
            catch { }
            sw1.Close();
            ExecuteCMD("diskpart.exe", " /s \"" + Application.StartupPath + "\\detach.txt\"");
            wp.ShowDialog();
            

        }
        private void copyvhdbootfile() 
        {
            ExecuteCMD("xcopy.exe", "\"" + Application.StartupPath + "\\files" + "\\" + "vhd" + "\\" + "*.*" + "\"" + " " + ud + " /e /h /y");
            wp.ShowDialog();
            if (radiovhdx.Checked)
            {
                ExecuteCMD("xcopy.exe", "\"" + Application.StartupPath + "\\files" + "\\" + "vhdx" + "\\" + "*.*" + "\"" + " " + ud + "\\boot\\ /e /h /y");
                wp.ShowDialog();
            }
            /////////////////////////////////////////////////////
            copyvhd();
            System.Diagnostics.Process booice = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=" + ud.Substring(0, 2) + " /mbr /install /type=nt60 /quiet"));//写入引导
            booice.WaitForExit();
            System.Diagnostics.Process pbr = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=" + ud.Substring(0, 2) + " /pbr /install /type=bootmgr /quiet"));//写入引导
            pbr.WaitForExit();
            System.Diagnostics.Process act = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\bootice.exe", " /DEVICE=" + ud.Substring(0, 2) + " /partitions /activate /quiet");
            act.WaitForExit();

        }

        private void vhd_dynamic_instructions() 
        {
            FileStream fs1 = new FileStream(ud + "VHD模式说明.txt", FileMode.Create, FileAccess.Write);
            fs1.SetLength(0);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            string ws1 = "";
            try
            {
                ws1 = "您创建的VHD为动态大小VHD，实际VHD容量：" + vhd_size + "MB\n";
                sw1.WriteLine(ws1);
                ws1 = "在VHD系统启动后将自动扩充为实际容量。请您在优盘留有足够空间确保系统正常启动！";
                sw1.WriteLine(ws1);
            }
            catch { }
            sw1.Close();

        }
        //private void cleanvhdtemp() 
        //{
        //    /////////////////////删除临时文件////////////////////
        //    cleantemp();
        //}
        #endregion
        private void button1_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process KILL = System.Diagnostics.Process.Start("cmd.exe", "/c taskkill /f /IM VD.exe");
            //KILL.WaitForExit();
         
            try { if (threadwrite.IsAlive) { MessageBox.Show("正在写入..."); return; } }
            catch { }
           
            threadwrite = new Thread(new ThreadStart(gowrite));
            threadwrite.Start();
          
        }

        private void isobutton_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            if (System.IO.File.Exists(openFileDialog1.FileName)) { wimbox.Text = openFileDialog1.FileName; }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Feedback frmf = new Feedback();
            //frmf.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //contextMenuStrip1.Show(Control.MousePosition);

            folderBrowserDialog1.ShowDialog();
            if (folderBrowserDialog1.SelectedPath.Length != 3)
            {
                if (folderBrowserDialog1.SelectedPath != "")
                { MessageBox.Show("请选择优盘根目录"); }
                return;

            }
            comboBox1.Items.Add(folderBrowserDialog1.SelectedPath);
            comboBox1.SelectedItem = folderBrowserDialog1.SelectedPath;
        }
        private void report()
        {
            string pageHtml;
            try
            {

                WebClient MyWebClient = new WebClient();

                MyWebClient.Credentials = CredentialCache.DefaultCredentials;//获取或设置用于对向Internet资源的请求进行身份验证的网络凭据。

                Byte[] pageData = MyWebClient.DownloadData("http://bbs.luobotou.org/app/wintogo.txt"); //从指定网站下载数据

                pageHtml = Encoding.Default.GetString(pageData);
                //MessageBox.Show(pageHtml);
                int index = pageHtml.IndexOf("report=");
             
                if (pageHtml.Substring(index + 7, 1) == "1")
                {
                    MailAddress from = new MailAddress("nkc3g4software@163.com", "Report"); //邮件的发件人

                    MailMessage mail = new MailMessage();

                    //设置邮件的标题
                    mail.Subject = "【程序报告】" + Application.ProductName + " " + Application.ProductVersion + " " ;

                    //设置邮件的发件人
                    //Pass:如果不想显示自己的邮箱地址，这里可以填符合mail格式的任意名称，真正发mail的用户不在这里设定，这个仅仅只做显示用
                    mail.From = from;

                    //设置邮件的收件人
                    string address = "";
                    string displayName = "";
                    /*  这里这样写是因为可能发给多个联系人，每个地址用 ; 号隔开
                      一般从地址簿中直接选择联系人的时候格式都会是 ：用户名1 < mail1 >; 用户名2 < mail 2>; 
                      因此就有了下面一段逻辑不太好的代码
                      如果永远都只需要发给一个收件人那么就简单了 mail.To.Add("收件人mail");
                    */
                    string[] mailNames = ("microsoft5133@126.com" + ";").Split(';');
                    foreach (string name in mailNames)
                    {
                        if (name != string.Empty)
                        {
                            if (name.IndexOf('<') > 0)
                            {
                                displayName = name.Substring(0, name.IndexOf('<'));
                                address = name.Substring(name.IndexOf('<') + 1).Replace('>', ' ');
                            }
                            else
                            {
                                displayName = string.Empty;
                                address = name.Substring(name.IndexOf('<') + 1).Replace('>', ' ');
                            }
                            mail.To.Add(new MailAddress(address, displayName));
                        }
                    }

                    //设置邮件的抄送收件人
                    //这个就简单多了，如果不想快点下岗重要文件还是CC一份给领导比较好
                    //mail.CC.Add(new MailAddress("Manage@hotmail.com", "尊敬的领导"));

                    //设置邮件的内容
                    mail.Body = Application.ProductName + " " + Application.ProductVersion + " " + System.Environment.OSVersion.ToString();
                    //设置邮件的格式
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.IsBodyHtml = true;
                    //设置邮件的发送级别
                    mail.Priority = MailPriority.Normal;

                    //设置邮件的附件，将在客户端选择的附件先上传到服务器保存一个，然后加入到mail中
                    //string fileName = txtUpFile.PostedFile.FileName.Trim();
                    //fileName = "D:/UpFile/" + fileName.Substring(fileName.LastIndexOf("/") + 1);
                    //txtUpFile.PostedFile.SaveAs(fileName); // 将文件保存至服务器
                    //mail.Attachments.Add(new Attachment(fileName));

                    mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;

                    SmtpClient client = new SmtpClient();
                    //设置用于 SMTP 事务的主机的名称，填IP地址也可以了
                    client.Host = "smtp.163.com";
                    //设置用于 SMTP 事务的端口，默认的是 25
                    //client.Port = 25;
                    client.UseDefaultCredentials = true;
                    //这里才是真正的邮箱登陆名和密码，比如我的邮箱地址是 hbgx@hotmail， 我的用户名为 hbgx ，我的密码是 xgbh
                    client.Credentials = new System.Net.NetworkCredential("nkc3g4software@163.com", "nkc3g4");
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    //都定义完了，正式发送了，很是简单吧！
                    try { client.Send(mail); }
                    catch (Exception e) { Console.WriteLine("Exception throw out:{0}", e.Message); }
                }
                {
                    //   update frmf = new update(ver);
                    // frmf.ShowDialog();
                    //     //frmf.Show();
                }

            }
            catch (WebException webEx)
            {

                Console.WriteLine(webEx.Message.ToString());

            }
        }
        private void update()
        {
        if (IsRegeditExit(Application.ProductName)) { if ((GetRegistData("nevercheckupdate")) == "1") { return; } }

            string pageHtml;
            try
            {

                WebClient MyWebClient = new WebClient();

                MyWebClient.Credentials = CredentialCache.DefaultCredentials;//获取或设置用于对向Internet资源的请求进行身份验证的网络凭据。

                Byte[] pageData = MyWebClient.DownloadData("http://bbs.luobotou.org/app/wintogo.txt"); //从指定网站下载数据

                pageHtml = Encoding.UTF8 .GetString(pageData);
               //essageBox.Show(pageHtml );
                int index = pageHtml.IndexOf("~");
                String ver;
         
                ver = pageHtml.Substring(index + 1, 7);
                if (ver != Application.ProductVersion)
                {
                    try
                    {
                        update frmf = new update(ver);
                        frmf.ShowDialog();
                    }
                    catch { }
               
                }

            } 
            catch (WebException webEx)
            {

                Console.WriteLine(webEx.Message.ToString());

            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/forum.php?mod=viewthread&tid=2427");
            //System.Diagnostics.Process.Start("http://bbs.luobotou.org/forum.php?mod=viewthread&tid=2427");
        }
        private void cleantemp() 
        {
            if (System.IO.Directory.Exists("V:\\"))
            {
                int vhdmaxsize;
                if (checkBoxfixed.Checked)
                {
                    vhdmaxsize = System.Int32.Parse(vhd_size) * 1048576 + 1024;
                }
                else
                {
                    vhdmaxsize = 10485670;
                }
                if (GetHardDiskFreeSpace(System.Environment.GetEnvironmentVariable("TEMP").Substring(0, 3)) <= vhdmaxsize || IsChina(System.Environment.GetEnvironmentVariable("TEMP")) || !usetemp)
                {
                    vpath = ud + win8vhdfile;
                }
                else
                {
                    vpath = System.Environment.GetEnvironmentVariable("TEMP") + "\\" + win8vhdfile;
                    needcopy = true;
                }
                detachvhd();
            }
            if (System.IO.Directory.Exists("V:\\")) 
            {
                error er = new error("盘符V不能被占用！");
                er.ShowDialog();
            }
            //if (useiso) { SyncCMD("\""+Application.StartupPath + "\\files\\" + "\\isocmd.exe\" -eject 0: "); }
            try
            {
                SyncCMD("taskkill /f /IM imagex.exe");
                //KILL.Start();
                //KILL.WaitForExit();
            }
            catch { }
            try { threadupdate.Abort(); }
            catch { }
            try { threadad.Abort(); }
            catch { }
            
            try
            {
                if (File.Exists(Application.StartupPath + "\\create.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\create.txt");
                }
                if (File.Exists(Application.StartupPath + "\\removex.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\removex.txt");
                }

                if (File.Exists(Application.StartupPath + "\\detach.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\detach.txt");
                }
                if (File.Exists(Application.StartupPath + "\\uefi.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\uefi.txt");
                }
                if (File.Exists(Application.StartupPath + "\\uefimbr.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\uefimbr.txt");
                }
                if (File.Exists(Application.StartupPath + "\\dp.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\dp.txt");
                }
                if (File.Exists(Application.StartupPath + "\\attach.txt"))
                {
                    //如果存在则删除
                    File.Delete(Application.StartupPath + "\\attach.txt");
                }

                if (File.Exists(System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhd"))
                {
                    //如果存在则删除
                    File.Delete(System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhd");
                }
                if (File.Exists(System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhdx"))
                {
                    //如果存在则删除
                    File.Delete(System.Environment.GetEnvironmentVariable("TEMP") + "\\win8.vhdx");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("程序删除临时文件出错！可重启程序或重启电脑重试！\n" + ex.ToString());
                shouldcontinue = false;
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                if (threadwrite.IsAlive)
                {
                    if (DialogResult.No == MessageBox.Show("正在写入，您确定要取消吗？", "警告！", MessageBoxButtons.YesNo)) { e.Cancel = true; }
                    threadwrite.Abort();
                }
            }
            catch { }
            cleantemp();

           
        }

        private void 打开程序运行目录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Application.StartupPath);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            VisitWeb(adlink);
            //System.Diagnostics.Process.Start(adlink);
        }
        private void ad()
        {
            string pageHtml1;
            try
            {

                WebClient MyWebClient = new WebClient();

                MyWebClient.Credentials = CredentialCache.DefaultCredentials;//获取或设置用于对向Internet资源的请求进行身份验证的网络凭据。

                Byte[] pageData = MyWebClient.DownloadData("http://bbs.luobotou.org/app/wintogo.txt"); //从指定网站下载数据

                pageHtml1 = Encoding.UTF8.GetString(pageData);
               // MessageBox.Show(pageHtml1);
                int index = pageHtml1.IndexOf("announcement=");
                // MessageBox.Show(pageHtml1.Substring(index + 13, 1));
                if (pageHtml1.Substring(index + 13, 1) != "0")
                {
                    string pageHtml;
                    try
                    {

                        WebClient MyWebClient1 = new WebClient();

                        MyWebClient1.Credentials = CredentialCache.DefaultCredentials;//获取或设置用于对向Internet资源的请求进行身份验证的网络凭据。

                        Byte[] pageData1 = MyWebClient1.DownloadData("http://bbs.luobotou.org/app/announcement.txt"); //从指定网站下载数据

                        pageHtml = Encoding.UTF8.GetString(pageData1);
                        //MessageBox.Show(pageHtml);
                        int index1 = pageHtml.IndexOf(Application.ProductName);
                        int startindex = pageHtml.IndexOf("~", index1);
                        int endindex = pageHtml.IndexOf("结束", index1);
                        int adprogram = index1 + Application.ProductName.Length + 1;
                        String adtitle;
                        //MessageBox.Show(adprogram.ToString() + " " + startindex);
                        adtitle = pageHtml.Substring(adprogram, startindex - adprogram);
                     
                        adlink = pageHtml.Substring(startindex + 1, endindex - startindex - 1);
                        linkLabel2.Invoke(Set_Text, new object[] { adtitle });
                        //MessageBox.Show("");

                        //MessageBox.Show(adtitle + "     " + adlink);
                    }
                    catch (Exception Ex)
                    {
                        //MessageBox.Show(Ex.ToString());
                        Console.WriteLine(Ex.Message.ToString());

                    }
                }
            }
            catch { }

        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/forum-88-1.html");
        }

        private void 不格式化磁盘ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //isformat = !isformat ;
        }

        private void imagex解压写入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wimpart = choosepart.part;
            if (wimpart == 0)
            {//自动判断模式
                win7togo = iswin7(win8iso);

                if (win7togo == 1)
                {//WIN7 32 bit

                    wimpart = 5;
                }
                else if (win7togo == 2)
                { //WIN7 64 BIT

                    wimpart = 4;
                }
                else { wimpart = 1; }
            }

            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }
            if (!System.IO.File.Exists(wimbox.Text)) { MessageBox.Show("请选择install.wim文件！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            if (DialogResult.No == MessageBox.Show("请再次确认您所选择的 " + ud.Substring(0, 1) + "盘是将要写入的优盘或移动硬盘\n误格式化，后果自负！", "警告！", MessageBoxButtons.YesNo)) { return; }
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\imagex.exe", " /apply " + "\"" + wimbox.Text + "\"" + " " + wimpart + " " + ud);
            p.WaitForExit();
            MessageBox.Show("操作完成！");
        }

        private void 写入引导文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }

            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            if (DialogResult.No == MessageBox.Show("请再次确认您所选择的 " + ud.Substring(0, 1) + "盘是将要写入的优盘或移动硬盘\n误格式化，后果自负！", "警告！", MessageBoxButtons.YesNo)) { return; }
            System.Diagnostics.Process p1 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + bcdboot, "  " + ud.Substring(0, 3) + "windows  /s  " + ud.Substring(0, 2));
            p1.WaitForExit();
            MessageBox.Show("操作完成！");
        }

        private void 设置活动分区ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }

            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘

            if (DialogResult.No == MessageBox.Show("请再次确认您所选择的 " + ud.Substring(0, 1) + "盘是将要写入的优盘或移动硬盘\n误格式化，后果自负！", "警告！", MessageBoxButtons.YesNo)) { return; }
            System.Diagnostics.Process p2 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\bootice.exe", " /DEVICE=" + ud.Substring(0, 2) + " /partitions /activate /quiet");
            p2.WaitForExit();
            MessageBox.Show("操作完成！");
        }

        private void 写入磁盘引导ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }

            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            if (DialogResult.No == MessageBox.Show("请再次确认您所选择的 " + ud.Substring(0, 1) + "盘是将要写入的优盘或移动硬盘\n误格式化，后果自负！", "警告！", MessageBoxButtons.YesNo)) { return; }
            System.Diagnostics.Process booice = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=" + ud.Substring(0, 2) + " /mbr /install /type=nt60 /quiet"));//写入引导
            booice.WaitForExit();
            System.Diagnostics.Process pbr = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\BOOTICE.exe", (" /DEVICE=" + ud.Substring(0, 2) + " /pbr /install /type=bootmgr /quiet"));//写入引导
            pbr.WaitForExit();
            MessageBox.Show("操作完成！");
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

      
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) 
            {
                udlist();
                
            }
        }

        private void linkLabel3_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
           
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            bcdboot9200.Checked = false;
            bcdboot = "\\bcdboot7601.exe";
        }

        private void bcdboot9200_Click(object sender, EventArgs e)
        {
            bcdboot7601.Checked = false;
            bcdboot = "\\bcdboot.exe";
        }

        private void 选择安装分卷ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            choosepart  frmf = new choosepart ();
            frmf.Show();
        }

        private void 强制格式化ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            force = 1;
        }
        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // 这里仅做输出的示例，实际上您可以根据情况取消获取命令行的内容  
            // 参考：process.CancelOutputRead()  
            try
            {

                if (String.IsNullOrEmpty(e.Data) == false)
                    this.AppendText(e.Data + "\r\n");
            }
            catch { }
        }
        #region 解决多线程下控件访问的问题

        private void progress_Exited(object sender, EventArgs e) 
        {
            //MessageBox.Show("exz");
            try
            {
                wp.Close();
            }
            catch { }
        }

        public void AppendText(string text)
        {
            try
            {
                if (wp.textBox1.Lines.Length == 0 || wp.textBox1.Lines.Length == 1 || text != wp.textBox1.Lines[wp.textBox1.Lines.Length - 2] + "\r\n")
                {
                    //if (text.Contains("Leaving")) { wp.Close(); }
                    //if (wp.textBox1.Lines.Length != 0)
                    //MessageBox.Show(text+"\n/////////////\n"+ wp.textBox1.Lines[wp.textBox1.Lines.Length - 2] + "\r\n");
                    if (wp.textBox1.InvokeRequired)
                    {
                        AppendTextCallback d = new AppendTextCallback(AppendText);
                        wp.textBox1.Invoke(d, text);
                    }
                    else
                    {
                        wp.textBox1.AppendText(text);
                        //this.textBox1.AppendText(text);
                    }
                }
            }
            catch { }
        }

        #endregion 
        private void ExecuteCMD(string StartFileName, string StartFileArg)
        {

            System.Diagnostics.Process process = new System.Diagnostics.Process();

            try
            {
                AppendText("Command:" + StartFileName + StartFileArg+"\r\n");
                process.StartInfo.FileName = StartFileName;
                process.StartInfo.Arguments = StartFileArg;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += new DataReceivedEventHandler(process_OutputDataReceived);
                process.EnableRaisingEvents = true;
                process.Exited += new EventHandler(progress_Exited);

                process.Start();

               
                process.BeginOutputReadLine();
              

            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败"+ex.ToString ());
            }
          
        }
        private void SyncCMD(string cmd) 
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();

            try
            {
                process.StartInfo.FileName = "cmd.exe";
             
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
               

                process.Start();
                process.StandardInput.WriteLine(cmd);

                process.StandardInput.WriteLine("exit");
             
                process.WaitForExit();
              
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败"+ex.ToString ());
            }
            finally
            {
                process.Close();
              
            }
        }
        private void wimbox_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            bool mount_successfully = false;
            if (System.IO.File.Exists(openFileDialog1.FileName)) 
            {
                wimbox.Text = openFileDialog1.FileName;
                if (openFileDialog1.FileName.Substring(openFileDialog1.FileName.Length - 3, 3) == "iso")
                {

                    //ExecuteCMD(Application.StartupPath + "\\isocmd.exe  -i");
                    SyncCMD("\"" + Application.StartupPath + "\\files" + "\\isocmd.exe\" -i");
                    SyncCMD("\"" + Application.StartupPath + "\\files" + "\\isocmd.exe\" -s");
                    SyncCMD("\"" + Application.StartupPath + "\\files" + "\\isocmd.exe\" -NUMBER 1");
                    SyncCMD("\"" + Application.StartupPath + "\\files" + "\\isocmd.exe\" -eject 0: ");
                    SyncCMD("\"" + Application.StartupPath + "\\files" + "\\isocmd.exe\" -MOUNT 0: \"" + openFileDialog1.FileName + "\"");
                    //mount.WaitForExit();
                    for (int i = 68; i <= 90; i++)
                    {
                        string ascll_to_eng = Convert.ToChar(i).ToString();
                        if (File.Exists(ascll_to_eng + ":\\sources\\install.wim"))
                        {
                            wimbox.Text = ascll_to_eng + ":\\sources\\install.wim";
                            mount_successfully = true;
                            break;
                        }
                    }
                    if (!mount_successfully)
                    {
                        MessageBox.Show("虚拟光驱加载失败，请手动加载，之后选择install.wim");
                    }
                    else
                    {
                        useiso = true;
                    }
                }
                else if (openFileDialog1.FileName.Substring(openFileDialog1.FileName.Length - 3, 3) == "esd")
                {
                    if (!allowesd)
                    {
                        MessageBox.Show("此系统不支持ESD文件处理！");

                        return;
                    }
                    else
                    {
                        isesd = true;
                        checkBoxwimboot.Checked = false;
                        checkBoxwimboot.Enabled = false;
                    }

                }
                else
                {
                    win7togo = iswin7(wimbox.Text);
                    if (win7togo != 0) //WIN7 cannot comptible with VHDX disk &wimboot
                    {
                        if (radiovhdx.Checked) { radiovhd.Checked = true; }
                        radiovhdx.Enabled = false;
                        checkBoxwimboot.Checked = false;
                        checkBoxwimboot.Enabled = false;
                    }
                }

            }
           
        }

        private void label1_Click(object sender, EventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/forum.php?mod=viewthread&tid=2427&extra=page%3D1");

        }

       

        private void 萝卜头IT论坛ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org");

        }

        private void 创建VHDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            createvhd();
            
        }

        private void 向V盘写入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wimpart = choosepart.part;

            if (wimpart == 0)
            {//自动判断模式
                win7togo = iswin7(win8iso);

                if (win7togo == 1)
                {//WIN7 32 bit

                    wimpart = 5;
                }
                else if (win7togo == 2)
                { //WIN7 64 BIT

                    wimpart = 4;
                }
                else { wimpart = 1; }
            }

            System.Diagnostics.Process p = System.Diagnostics.Process.Start(Application.StartupPath + "\\files" + "\\imagex.exe", " /apply " + "\"" + wimbox.Text + "\"" + " " + wimpart.ToString() + " " + "v:\\");
            p.WaitForExit();

        }

        private void 卸载V盘ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            detachvhd();

        }

        private void 复制VHD启动文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            ExecuteCMD("takeown.exe", " /f \""+ud+"\\boot\\"+"\" /r /d y && icacls \""+ud+"\\boot\\"+"\" /grant administrators:F /t");
            wp.ShowDialog();

            
            ExecuteCMD("xcopy.exe", "\"" + Application.StartupPath + "\\files" + "\\" + "vhd" + "\\" + "*.*" + "\"" + " " + ud + " /e /h /y");
            wp.ShowDialog();

            //copyvhdbootfile();
           
        }

        private void 复制win8vhdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            //Copy(System.Environment.GetEnvironmentVariable("TEMP") + "\\" + win8vhdfile, ud);
            copy cp = new copy(ud);
            cp.ShowDialog();

            //Copy(System.Environment.GetEnvironmentVariable("TEMP") + "\\" + win8vhdfile, ud + win8vhdfile);

        }

        private void 清理临时文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cleantemp();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //MessageBox.Show(Copy(System.Environment.GetEnvironmentVariable("TEMP") + "\\"+win8vhdfile, ud).ToString());
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            //MessageBox.Show(Copy(System.Environment.GetEnvironmentVariable("TEMP") + "\\"+win8vhdfile, ud + win8vhdfile).ToString());
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radiovhdx.Checked) { win8vhdfile = "win8.vhdx"; }
            else { win8vhdfile = "win8.vhd"; }
            //checkBoxuefi.Enabled = !radiovhdx.Checked;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
        }

        private void radiovhd_CheckedChanged(object sender, EventArgs e)
        {
            if (radiovhdx.Checked) { win8vhdfile = "win8.vhdx"; }
            else { win8vhdfile = "win8.vhd"; }
            //checkBoxuefi.Enabled = !radiovhd.Checked;
        }

        private void radiochuantong_CheckedChanged(object sender, EventArgs e)
        {
            if (radiovhdx.Checked) { win8vhdfile = "win8.vhdx"; }
            else { win8vhdfile = "win8.vhd"; }

        }

        private void bcdbootToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (button2.Text == ">")
            {
                formwide = this.Width;
                this.Width = this.Width * 100 / 66;
                //MessageBox.Show((this.Width * 100 / 66).ToString());
                button2.Text = "<";
            }
            else 
            {
                this.Width = formwide;
                //MessageBox.Show((this.Width * 66 / 100).ToString());

                button2.Text = ">";

            }
        }

        private void checkBoxframework_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBoxuefi_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxuefimbr.Checked && checkBoxuefi.Checked ) { checkBoxuefimbr.Checked = false; checkBoxuefi.Checked = true; }
            checkBoxdiskpart.Enabled = !checkBoxuefi.Checked;
            checkBoxdiskpart.Checked = checkBoxuefi.Checked;
        }

        private void radiovhd_EnabledChanged(object sender, EventArgs e)
        {
            radiochuantong.Checked = true;
        }

        private void bootsectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }

            System.Diagnostics.Process p1 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files\\" + "\\bootsect.exe", " /nt60 " + ud.Substring(0, 2) + " /force /mbr");
            p1.WaitForExit();
            MessageBox.Show("操作完成！");
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void diskpart重新分区ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            diskpart();


        }

        private void linkLabel3_LinkClicked_2(object sender, LinkLabelLinkClickedEventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/thread-3566-1-1.html");
            //System.Diagnostics.Process.Start("http://bbs.luobotou.org/thread-3566-1-1.html");
        }

        private void checkBoxnotemp_CheckedChanged(object sender, EventArgs e)
        {
            usetemp = !usetemp;
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/thread-6098-1-1.html");
        }

        private void 在线帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/forum.php?mod=viewthread&tid=2427");

        }

        private void 官方论坛ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VisitWeb("http://bbs.luobotou.org/forum-88-1.html");

        }

        private void diskpart重新分区ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }

            if (System.Environment.OSVersion.ToString().Contains("5.1") || System.Environment.OSVersion.ToString().Contains("5.2")) { MessageBox.Show("XP系统不支持此操作！"); return; }
            if (DialogResult.No == MessageBox.Show("此操作将会清除移动磁盘所有分区的所有数据，确认？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
            if (DialogResult.No == MessageBox.Show("您确定要继续吗？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; } 

            diskpart();
            MessageBox.Show("完成！");

        }
        private void diskpart() 
        {

            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            //if (DialogResult.No == MessageBox.Show("此操作将会清除移动磁盘所有分区的所有数据，确认？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; }
            //if (DialogResult.No == MessageBox.Show("您确定要继续吗？", "警告！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) { return; } 

            FileStream fs0 = new FileStream(Application.StartupPath + "\\dp.txt", FileMode.Create, FileAccess.Write);
            fs0.SetLength(0);
            StreamWriter sw0 = new StreamWriter(fs0, Encoding.Default);
            string ws0 = "";
            try
            {
                ws0 = "select volume " + ud.Substring(0, 1);
                sw0.WriteLine(ws0);
                ws0 = "clean";
                sw0.WriteLine(ws0);
                ws0 = "convert mbr";
                sw0.WriteLine(ws0);
                ws0 = "create partition primary";
                sw0.WriteLine(ws0);
                ws0 = "select partition 1";
                sw0.WriteLine(ws0);
                ws0 = "format fs=ntfs quick";
                sw0.WriteLine(ws0);
                ws0 = "active";
                sw0.WriteLine(ws0);
                ws0 = "assign letter=" + ud.Substring(0, 1);
                sw0.WriteLine(ws0);
                ws0 = "exit";
                sw0.WriteLine(ws0);
            }
            catch { }
            sw0.Close();
            
            ExecuteCMD("diskpart.exe" , " /s \"" + Application.StartupPath + "\\dp.txt\"");
            wp.ShowDialog();
            //System.Diagnostics.Process dpc = System.Diagnostics.Process.Start("diskpart.exe", " /s " + Application.StartupPath + "\\dp.txt");
            //dpc.WaitForExit();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void vHD扩容ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (System.Environment.OSVersion.ToString().Contains("5.1") || System.Environment.OSVersion.ToString().Contains("5.2")) { MessageBox.Show("XP系统不支持此操作！"); return; }
            //vhdexpand vdp = new vhdexpand();
            //vdp.Show();
        }

        private void 打开程序运行目录ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath);
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("萝卜头IT论坛 nkc3g4制作\nQQ:1443112740\nEmail:microsoft5133@126.com","关于");
            AboutBox abx = new AboutBox();
            abx.Show();
        }

        private void vHDUEFIBCDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘
            //MessageBox.Show("/store X:\\efi\\microsoft\\boot\\bcd /set {92382214-91cb-4c08-bed7-5c48c55d46bc} device vhd=[" + ud.Substring(0, 2) + "]\\" + win8vhdfile);
            //if (File.Exists(@"C:\Windows\WinSxS\amd64_microsoft-windows-b..iondata-cmdlinetool_31bf3856ad364e35_6.3.9600.16384_none_78e95cd07922a6bf\\bcdedit.exe")) { MessageBox.Show("存在"); } else { MessageBox.Show("不存在！"); }

            //System.Diagnostics.Process cv = System.Diagnostics.Process.Start(Application.StartupPath + "\\files\\" + "\\bcdedit.exe", " /store X:\\efi\\microsoft\\boot\\bcd /set {92382214-91cb-4c08-bed7-5c48c55d46bc} device vhd=[" + ud.Substring(0, 2) + "]\\" + win8vhdfile);
            //    cv.WaitForExit();
            //    System.Diagnostics.Process cv1 = System.Diagnostics.Process.Start(Application.StartupPath + "\\files\\" + "\\bcdedit.exe", " /store X:\\efi\\microsoft\\boot\\bcd /set {92382214-91cb-4c08-bed7-5c48c55d46bc} osdevice vhd=[" + ud.Substring(0, 2) + "]\\" + win8vhdfile);
            //    cv1.WaitForExit();



            //ExecuteCMD("bcdedit /store X:\\efi\\microsoft\\boot\\bcd /set {92382214-91cb-4c08-bed7-5c48c55d46bc} device vhd=[" + ud.Substring(0, 2) + "]\\" + win8vhdfile);
            //ExecuteCMD("bcdedit /store X:\\efi\\microsoft\\boot\\bcd /set {92382214-91cb-4c08-bed7-5c48c55d46bc} osdevice vhd=[" + ud.Substring(0, 2) + "]\\" + win8vhdfile);

        }

        private void 自动检查更新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            threadupdate = new Thread(update);
            threadupdate.Start();
            MessageBox.Show("若无弹出窗口，则当前程序已是最新版本.");
        }

        private void checkBoxdiskpart_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxdiskpart.Checked)
            {
                MessageBox.Show("请注意勾选此选项将会清空移动磁盘所有数据！", "警告！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                checkBoxunformat.Checked = false;
                checkBoxunformat.Enabled = false;
            }
            else
            {
                checkBoxunformat.Enabled = true;
            }
        }

        private void 选择安装分卷ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            choosepart frmf = new choosepart();
            frmf.Show();

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void comboBox1_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(this.comboBox1, comboBox1.SelectedItem.ToString()); ;
        }

        private void wIN7TOGOToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            //win7togo = wIN7TOGOToolStripMenuItem.Checked;
        }

        private void wIN7USBBOOTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";
            Win7REG(ud);
        }

        private void bOOTICEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + "\\files\\bootice.exe");
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + "\\logs");

        }

        private void checkBoxuefimbr_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxuefi.Checked && checkBoxuefimbr.Checked) { checkBoxuefi.Checked = false; checkBoxuefimbr.Checked = true;  }
            if (checkBoxuefimbr.Checked) { checkBoxdiswinre.Checked = true; }
            checkBoxdiskpart.Enabled = !checkBoxuefimbr.Checked;
            checkBoxdiskpart.Checked = checkBoxuefimbr.Checked;
        }
        public static  void VisitWeb(string url) 
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"http\shell\open\command\");
                string s = key.GetValue("").ToString();

                Regex reg = new Regex("\"([^\"]+)\"");
                MatchCollection matchs = reg.Matches(s);

                string filename = "";
                if (matchs.Count > 0)
                {
                    filename = matchs[0].Groups[1].Value;
                    System.Diagnostics.Process.Start(filename, url);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("程序遇到严重错误\n官方支持论坛：bbs.luobotou.org\n" + ex.ToString());
               
            }


        }

        private void 错误提示测试ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start("c:\\windows\\system32\\bcdboot.exe");
            //MessageBox.Show(Environment.GetEnvironmentVariable("windir") + "\\system32\\bcdboot.exe");
            //ExecuteCMD(Environment.GetEnvironmentVariable("windir") + "\\system32\\bcdboot.exe", "  " + "V:\\" + "windows  /s  x: /f UEFI");
            //wp.ShowDialog();
            MessageBox.Show(Application.ProductName);
            //Fixletter("C:","J:");
            //error ex = new error("测试错误信息！TEST!");
            //ex.Show();
        }

        private void checkBoxunformat_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxunformat.Checked) { MessageBox.Show("此选项仅在非UEFI模式下有效！"); }
        }

        private void toolStripMenuItemvhdx_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) { MessageBox.Show("请选择优盘！"); return; }
            ud = comboBox1.SelectedItem.ToString().Substring(0, 2) + "\\";//优盘

            ExecuteCMD("xcopy.exe", "\"" + Application.StartupPath + "\\files" + "\\" + "vhd" + "\\" + "*.*" + "\"" + " " + ud + " /e /h /y");
            wp.ShowDialog();
            ExecuteCMD("xcopy.exe", "\"" + Application.StartupPath + "\\files" + "\\" + "vhdx" + "\\" + "*.*" + "\"" + " " + ud + "\\boot\\ /e /h /y");
                wp.ShowDialog();
            

        }
    }
}
