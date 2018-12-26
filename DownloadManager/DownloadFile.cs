using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class DownloadFile : Form
    {
        public DownloadFile(MainForm frm)
        {
            InitializeComponent();
            _mainForm = frm;
        }

        WebClient client;
        public string Url { get; set; }
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public double Percent { get; set; }
        private MainForm _mainForm;
        DownloadProgressTracker tracker;

        private void DownloadForm_Load(object sender, EventArgs e)
        {
            tracker = new DownloadProgressTracker(50, TimeSpan.FromMilliseconds(500));
            client = new WebClient();
            client.Proxy = null;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            Url = txtAddress.Text;
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Database.FilesDataRow row = DatabaseManager.Database.FilesData.NewFilesDataRow();
            row.Url = Url;
            row.FileName = FileName;
            row.FileSize = (string.Format("{0:0.##} KB", FileSize / 1024));
            row.DateTime = DateTime.Now;
            row.Path = txtPath.Text;
            DatabaseManager.Database.FilesData.AddFilesDataRow(row);
            DatabaseManager.Database.AcceptChanges();
            DatabaseManager.Database.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
            ListViewItem item = new ListViewItem(row.Id.ToString());
            item.SubItems.Add(row.Url);
            item.SubItems.Add(row.FileName);
            item.SubItems.Add(row.FileSize);
            item.SubItems.Add(row.DateTime.ToLongDateString());
            item.SubItems.Add(row.Path);
            _mainForm.listView1.Items.Add(item);
            this.Close();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            prgrBar.Minimum = 0;
            double recieve = double.Parse(e.BytesReceived.ToString());
            FileSize = double.Parse(e.TotalBytesToReceive.ToString());
            Percent = recieve / FileSize * 100;
            lblStatus.Text = $"Downloaded {string.Format("{0:0.##}", Percent)}";
            prgrBar.Value = int.Parse(Math.Truncate(Percent).ToString());
            prgrBar.Update();

            tracker.SetProgress(e.BytesReceived, e.TotalBytesToReceive);
            label3.Text = e.BytesReceived + "/" + e.TotalBytesToReceive;
            label4.Text = tracker.GetBytesPerSecondString();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Url = txtAddress.Text;
            btnStart.Enabled = false;
            btnBrowse.Enabled = false;
            var errorList = "";
            if (txtPath.Text == "")
            {
                errorList += "Select download folder\n";
            }
            if (txtAddress.Text == "")
            {
                errorList += "Enter file url\n";
            }
            if (errorList != "")
            {
                MessageBox.Show(errorList);
                return;
            }
            Uri uri = new Uri(this.Url);
            FileName = System.IO.Path.GetFileName(uri.AbsolutePath);
            client.DownloadFileAsync(uri, Properties.Settings.Default.Path+"/"+FileName);
            prgrBar.Value = 0;
            label3.Text = "";
            label4.Text = "";
            btnStart.Enabled = true;
            btnBrowse.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            client.CancelAsync();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog()
            {
                Description = "Select your path."
            })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = fbd.SelectedPath;
                    Properties.Settings.Default.Path = txtPath.Text;
                    Properties.Settings.Default.Save();
                }
            }
        }
    }

    public class DownloadProgressTracker
    {
        private long _totalFileSize;
        private readonly int _sampleSize;
        private readonly TimeSpan _valueDelay;

        private DateTime _lastUpdateCalculated;
        private long _previousProgress;

        private double _cachedSpeed;

        private Queue<Tuple<DateTime, long>> _changes = new Queue<Tuple<DateTime, long>>();

        public DownloadProgressTracker(int sampleSize, TimeSpan valueDelay)
        {
            _lastUpdateCalculated = DateTime.Now;
            _sampleSize = sampleSize;
            _valueDelay = valueDelay;
        }

        public void NewFile()
        {
            _previousProgress = 0;
        }

        public void SetProgress(long bytesReceived, long totalBytesToReceive)
        {
            _totalFileSize = totalBytesToReceive;

            long diff = bytesReceived - _previousProgress;
            if (diff <= 0)
                return;

            _previousProgress = bytesReceived;

            _changes.Enqueue(new Tuple<DateTime, long>(DateTime.Now, diff));
            while (_changes.Count > _sampleSize)
                _changes.Dequeue();
        }

        public double GetProgress()
        {
            return _previousProgress / (double)_totalFileSize;
        }

        public string GetProgressString()
        {
            return String.Format("{0:P0}", GetProgress());
        }

        public string GetBytesPerSecondString()
        {
            double speed = GetBytesPerSecond();
            var prefix = new[] { "", "K", "M", "G" };

            int index = 0;
            while (speed > 1024 && index < prefix.Length - 1)
            {
                speed /= 1024;
                index++;
            }

            int intLen = ((int)speed).ToString().Length;
            int decimals = 3 - intLen;
            if (decimals < 0)
                decimals = 0;

            string format = String.Format("{{0:F{0}}}", decimals) + "{1}B/s";

            return String.Format(format, speed, prefix[index]);
        }

        public double GetBytesPerSecond()
        {
            if (DateTime.Now >= _lastUpdateCalculated + _valueDelay)
            {
                _lastUpdateCalculated = DateTime.Now;
                _cachedSpeed = GetRateInternal();
            }

            return _cachedSpeed;
        }

        private double GetRateInternal()
        {
            if (_changes.Count == 0)
                return 0;

            TimeSpan timespan = _changes.Last().Item1 - _changes.First().Item1;
            long bytes = _changes.Sum(t => t.Item2);

            double rate = bytes / timespan.TotalSeconds;

            if (double.IsInfinity(rate) || double.IsNaN(rate))
                return 0;

            return rate;
        }
    }
}
