using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class DownloadFtp : Form
    {
        private MainForm _mainForm;
        FtpClient ftp;
        string serverDir = "";

        public DownloadFtp(MainForm frm)
        {
            InitializeComponent();
            _mainForm = frm;
        }

        private void FtpForm_Load(object sender, EventArgs e)
        {
            ftp = new FtpClient();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("Server name can't be empty\nEnter password and login if required");
                return;
            }

            ftp.Host = textBox1.Text;
            ftp.UserName = textBox2.Text;
            ftp.Password = textBox3.Text;
            FileStruct[] FileList = ftp.ListDirectory("");
            try
            {
                listView1.Items.Clear();

                ListViewItem checkItem = new ListViewItem();
                checkItem.Text = "<--";
                listView1.Items.Add(checkItem);
                foreach (FileStruct s in FileList)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = s.Name;
                    lvi.SubItems.Add(s.CreateTime);
                    if (textBox3.Text == "")
                    {
                        lvi.SubItems.Add("-");
                    }
                    else
                    {
                        IsExist();
                    }
                    lvi.SubItems.Add(ftp.GetFileSize(s.Name, serverDir));
                    listView1.Items.Add(lvi);
                }
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems[0] == null)
            {
                MessageBox.Show("Choose folder");
            }
            if (listView1.SelectedItems[0].Text.Contains("."))
            {
                listView1.SelectedItems[0].Checked = false;
                MessageBox.Show("Choose folder!");
                return;
            }    
            try
            {
                FileStruct[] FileList;
                string directory;

                if (listView1.SelectedItems[0].Text == "<--")
                {
                    FileList = ftp.ListDirectory("");
                }
                else
                {
                    directory = listView1.SelectedItems[0].SubItems[0].Text.Trim();
                    FileList = ftp.ListDirectory("/" + directory);
                }

                listView1.Items.Clear();
                ListViewItem checkItem = new ListViewItem();
                checkItem.Text = "<--";
                listView1.Items.Add(checkItem);

                foreach (FileStruct s in FileList)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = s.Name;
                    lvi.SubItems.Add(s.CreateTime);
                    if (textBox3.Text == "")
                    {
                        lvi.SubItems.Add("-");
                    }
                    else
                    {
                        IsExist();
                    }
                    lvi.SubItems.Add(ftp.GetFileSize(s.Name, serverDir));
                    listView1.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var errorList = "";
            button3.Enabled = false;
            if (listView1.CheckedItems.Count == 0)
            {
                errorList += "Check files to download\n";
            }
            if (textBox4.Text == "")
            {
                errorList += "Select download folder\n";
            }
            if (errorList != "")
            {
                MessageBox.Show(errorList);
                button3.Enabled = true;
                return;
            }
            button3.Enabled = false;
            try
            {
                progressBar2.Minimum = 0;
                progressBar2.Maximum = listView1.CheckedItems.Count;
                foreach (ListViewItem item in listView1.CheckedItems)
                {
                    if (item.SubItems[2].Text == "true")
                    {
                        if (MessageBox.Show($"The file '{item.Text}' is already exists.\nDo you want to rewrite it?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            string directory = item.Text.Trim();
                            await ftp.DownloadFile(textBox4.Text, serverDir, directory);
                            progressBar2.Value++;

                            Database.FilesDataRow row = DatabaseManager.Database.FilesData.NewFilesDataRow();
                            row.Url = textBox1.Text;
                            row.FileName = item.Text;
                            row.FileSize = item.SubItems[3].Text;
                            row.DateTime = DateTime.Now;
                            row.Path = textBox4.Text + item.Text;
                            DatabaseManager.Database.FilesData.AddFilesDataRow(row);
                            DatabaseManager.Database.AcceptChanges();
                            DatabaseManager.Database.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
                            ListViewItem item1 = new ListViewItem(row.Id.ToString());
                            item1.SubItems.Add(row.Url);
                            item1.SubItems.Add(row.FileName);
                            item1.SubItems.Add(row.FileSize);
                            item1.SubItems.Add(row.DateTime.ToLongDateString());
                            item1.SubItems.Add(row.Path);
                            _mainForm.listView1.Items.Add(item1);
                        }
                        else
                        {
                            progressBar2.Value++;
                            continue;
                        }
                    }
                    else
                    {
                        string directory = item.Text.Trim();
                        await ftp.DownloadFile(textBox4.Text, serverDir, directory);
                        progressBar2.Value++;

                        Database.FilesDataRow row = DatabaseManager.Database.FilesData.NewFilesDataRow();
                        row.Url = textBox1.Text;
                        row.FileName = item.Text;
                        row.FileSize = item.SubItems[3].Text;
                        row.DateTime = DateTime.Now;
                        row.Path = textBox4.Text + item.Text;
                        DatabaseManager.Database.FilesData.AddFilesDataRow(row);
                        DatabaseManager.Database.AcceptChanges();
                        DatabaseManager.Database.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
                        ListViewItem item1 = new ListViewItem(row.Id.ToString());
                        item1.SubItems.Add(row.Url);
                        item1.SubItems.Add(row.FileName);
                        item1.SubItems.Add(row.FileSize);
                        item1.SubItems.Add(row.DateTime.ToLongDateString());
                        item1.SubItems.Add(row.Path);
                        _mainForm.listView1.Items.Add(item1);
                    }
                }
                MessageBox.Show("Download completed!");    
                button3.Enabled = true;
                IsExist();

                for (int i = 1; i < listView1.Items.Count; i++)
                {
                    listView1.Items[i].Checked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                button3.Enabled = true;
            }
            progressBar2.Value = 0;
            progressBar1.Value = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog()
            {
                Description = "Select your path."
            })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBox4.Text = fbd.SelectedPath;
                    Properties.Settings.Default.Path = textBox4.Text;
                    Properties.Settings.Default.Save();
                }
            }
            IsExist();
        }

        private void IsExist()
        {
            for (int i = 1; i < listView1.Items.Count; i++)
            {
                string path = textBox4.Text + @"\" + listView1.Items[i].Text;
                if (File.Exists(path))
                {
                    listView1.Items[i].SubItems[2].Text = "true";
                }
                else
                {
                    listView1.Items[i].SubItems[2].Text = "false";
                }
            }
        }

        private void progressBar2_Click(object sender, EventArgs e)
        {

        }

        private void listView1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == listView1.Items[0].Index)
            {
                for (int i = 1; i < listView1.Items.Count; i++)
                {
                    if (listView1.Items[0].Checked)
                    {
                        listView1.Items[i].Checked = false;
                    }
                    else
                    {
                        listView1.Items[i].Checked = true;
                    }
                }
            }
        }
    }

    class FtpClient
    {
        private ListView listView1 = Application.OpenForms["DownloadFtp"].Controls["listView1"] as ListView;
        private Label label1 = Application.OpenForms["DownloadFtp"].Controls["label1"] as Label;
        private ProgressBar progressBar1 = Application.OpenForms["DownloadFtp"].Controls["progressBar1"] as ProgressBar;

        private string _Host;
        private string _UserName;
        private string _Password;
        FtpWebRequest ftpRequest;
        FtpWebResponse ftpResponse;
        private bool _UseSSL = false;

        public string Host
        {
            get
            {
                return _Host;
            }
            set
            {
                _Host = value;
            }
        }
        public string UserName
        {
            get
            {
                return _UserName;
            }
            set
            {
                _UserName = value;
            }
        }
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                _Password = value;
            }
        }
        public bool UseSSL
        {
            get
            {
                return _UseSSL;
            }
            set
            {
                _UseSSL = value;
            }
        }

        public string GetFileSize(string fileName, string serverDir)
        {
            NetworkCredential credentials = new NetworkCredential(_UserName, _Password);

            WebRequest sizeRequest = WebRequest.Create("ftp://" + _Host + serverDir + "/" + fileName);
            sizeRequest.Credentials = credentials;
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            try
            {
                long size = (long)sizeRequest.GetResponse().ContentLength;
                return ToFileSize(size);
            }
            catch
            {
                return "";
            }
        }

        public static string ToFileSize(double value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) +
                " " + suffixes[suffixes.Length - 1];
        }

        private static string ThreeNonZeroDigits(double value)
        {
            return String.Format("{0:0.##}", value);
        }

        public FileStruct[] ListDirectory(string path)
        {
            if (path == null || path == "")
            {
                path = "/";
            }

            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path);
            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            ftpRequest.EnableSsl = _UseSSL;
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            string content = "";

            StreamReader sr = new StreamReader(ftpResponse.GetResponseStream(), System.Text.Encoding.ASCII);
            content = sr.ReadToEnd();
            sr.Close();
            ftpResponse.Close();

            DirectoryListParser parser = new DirectoryListParser(content);
            return parser.FullListing;
        }

        public void FillData()
        {
            while (true)
            {
                int i = 0;
                listView1.SelectedItems[0].SubItems[0].Text.Trim();
                Application.DoEvents();
            }
        }

        public async Task DownloadFile(string path, string serverDir, string fileName)
        {
            NetworkCredential credentials = new NetworkCredential(_UserName, _Password);

            WebRequest sizeRequest = WebRequest.Create("ftp://" + _Host + serverDir + "/" + fileName);
            sizeRequest.Credentials = credentials;
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            int size = (int)sizeRequest.GetResponse().ContentLength;

            progressBar1.Invoke(
                (MethodInvoker)(() => progressBar1.Maximum = size));

            WebRequest request = WebRequest.Create("ftp://" + _Host + serverDir + "/" + fileName);
            request.Credentials = credentials;
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            using (Stream ftpStream = request.GetResponse().GetResponseStream())
            using (Stream fileStream = new FileStream($@"{path}\{fileName}", FileMode.Create, FileAccess.ReadWrite))
            {
                byte[] buffer = new byte[10240];
                int read;
                while ((read = await ftpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    int position = (int)fileStream.Position;
                    progressBar1.Invoke(
                        (MethodInvoker)(() => progressBar1.Value = position));
                }
            }

        }
    }

    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public bool IsDirectory;
        public string CreateTime;
        public string Name;
    }

    public enum FileListStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }

    public class DirectoryListParser
    {
        private List<FileStruct> _myListArray;

        public FileStruct[] FullListing
        {
            get
            {
                return _myListArray.ToArray();
            }
        }

        public FileStruct[] FileList
        {
            get
            {
                List<FileStruct> _fileList = new List<FileStruct>();
                foreach (FileStruct thisstruct in _myListArray)
                {
                    if (!thisstruct.IsDirectory)
                    {
                        _fileList.Add(thisstruct);
                    }
                }
                return _fileList.ToArray();
            }
        }

        public FileStruct[] DirectoryList
        {
            get
            {
                List<FileStruct> _dirList = new List<FileStruct>();
                foreach (FileStruct thisstruct in _myListArray)
                {
                    if (thisstruct.IsDirectory)
                    {
                        _dirList.Add(thisstruct);
                    }
                }
                return _dirList.ToArray();
            }
        }

        public DirectoryListParser(string responseString)
        {
            _myListArray = GetList(responseString);
        }

        private List<FileStruct> GetList(string datastring)
        {
            List<FileStruct> myListArray = new List<FileStruct>();
            string[] dataRecords = datastring.Split('\n');

            FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (_directoryListStyle != FileListStyle.Unknown && s != "")
                {
                    FileStruct f = new FileStruct();
                    f.Name = "..";
                    switch (_directoryListStyle)
                    {
                        case FileListStyle.UnixStyle:
                            f = ParseFileStructFromUnixStyleRecord(s);
                            break;
                        case FileListStyle.WindowsStyle:
                            f = ParseFileStructFromWindowsStyleRecord(s);
                            break;
                    }
                    if (f.Name != "" && f.Name != "." && f.Name != "..")
                    {
                        myListArray.Add(f);
                    }
                }
            }
            return myListArray;
        }

        private FileStruct ParseFileStructFromWindowsStyleRecord(string Record)
        {
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();

            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            f.CreateTime = dateStr + " " + timeStr;

            if (processstr.Substring(0, 5) == "<DIR>")
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                processstr = strs[1];
                f.IsDirectory = false;
            }

            f.Name = processstr;
            return f;
        }

        public FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {

                if (s.Length > 10
                    && Regex.IsMatch(s.Substring(0, 10), "(-|d)((-|r)(-|w)(-|x)){3}"))
                {
                    return FileListStyle.UnixStyle;
                }

                else if (s.Length > 8
                    && Regex.IsMatch(s.Substring(0, 8), "[0-9]{2}-[0-9]{2}-[0-9]{2}"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }

        private FileStruct ParseFileStructFromUnixStyleRecord(string record)
        {

            FileStruct f = new FileStruct();
            if (record[0] == '-' || record[0] == 'd')
            {
                string processstr = record.Trim();
                f.Flags = processstr.Substring(0, 9);
                f.IsDirectory = (f.Flags[0] == 'd');
                processstr = (processstr.Substring(11)).Trim();

                _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.Owner = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.CreateTime = getCreateTimeString(record);

                int fileNameIndex = record.IndexOf(f.CreateTime) + f.CreateTime.Length;

                f.Name = record.Substring(fileNameIndex).Trim();
            }
            else
            {
                f.Name = "";
            }
            return f;
        }

        private string getCreateTimeString(string record)
        {
            string month = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)";
            string space = @"(\040)+";
            string day = "([0-9]|[1-3][0-9])";
            string year = "[1-2][0-9]{3}";
            string time = "[0-9]{1,2}:[0-9]{2}";
            Regex dateTimeRegex = new Regex(month + space + day + space + "(" + year + "|" + time + ")", RegexOptions.IgnoreCase);
            Match match = dateTimeRegex.Match(record);
            return match.Value;
        }

        private string _cutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }
    }
}
