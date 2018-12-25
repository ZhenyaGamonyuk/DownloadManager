using System;
using System.Net;
using System.Windows.Forms;

namespace DownloadManager
{
    public class FileModel
    {
        WebClient client;
        public Uri Uri
        {
            get { return new Uri(Url); }
            set { }
        }
        public string Url { get; set; }
        public string FileName
        {
            get { return System.IO.Path.GetFileName(Uri.AbsolutePath); }
            set { }
        }
        public double FileSize { get; set; }
        public double Percent { get; set; }
        public int FilesCount { get; set; }
        private MainForm _mainForm;

        private ProgressBar progressBar1 = Application.OpenForms["FilesForm"].Controls["prgrBar1"] as ProgressBar;
        private ProgressBar progressBar2 = Application.OpenForms["FilesForm"].Controls["prgrBar2"] as ProgressBar;
        private ListView listView1 = Application.OpenForms["FilesForm"].Controls["listView1"] as ListView;

        public FileModel(MainForm frm, int filesCount)
        {
            _mainForm = frm;
            FileSize = default(double);
            FilesCount = filesCount;
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            FileSize = double.Parse(e.TotalBytesToReceive.ToString());
            progressBar1.Minimum = 0;
            double recieve = double.Parse(e.BytesReceived.ToString());
            Percent = recieve / FileSize * 100;
            progressBar1.Value = int.Parse(Math.Truncate(Percent).ToString());
        }

        public void ListDownload(string folder)
        {
            progressBar2.Minimum = 0;
            progressBar2.Maximum = listView1.CheckedItems.Count;
            if (listView1.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more files to download.");
            }
            else
            {
                foreach (ListViewItem item in listView1.CheckedItems)
                {
                    if (item.Text == "")
                    {
                        continue;
                    }
                    else
                    {
                        if (item.SubItems[3].Text == "true")
                        {
                            if (MessageBox.Show($"The file '{item.SubItems[1]}' is already exists.\nDo you want to rewrite it?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                Url = item.Text;
                                string path = folder + FileName;
                                Download(folder, item);
                                progressBar2.Value++;
                            }
                            else
                            {
                                progressBar2.Value++;
                                continue;
                            }
                        }
                        else
                        {
                            Url = item.Text;
                            string path = folder + FileName;
                            Download(folder, item);
                            progressBar2.Value++;
                        }
                    }
                }
            }
        }

        private void Download(string path, ListViewItem item)
        {
            using (client = new WebClient())
            {
                client.Proxy = null;
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadFileAsync(Uri, path + FileName, $@"{path}{FileName}");
            }
            Database.FilesDataRow row = App.Database.FilesData.NewFilesDataRow();
            row.Url = Url;
            row.FileName = FileName;
            row.FileSize = item.SubItems[2].Text;
            row.DateTime = DateTime.Now;
            row.Path = $@"{path}{FileName}";
            App.Database.FilesData.AddFilesDataRow(row);
            App.Database.AcceptChanges();
            App.Database.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
            ListViewItem item1 = new ListViewItem(row.Id.ToString());
            item1.SubItems.Add(row.Url);
            item1.SubItems.Add(row.FileName);
            item1.SubItems.Add(row.FileSize);
            item1.SubItems.Add(row.DateTime.ToLongDateString());
            item1.SubItems.Add(row.Path);
            _mainForm.listView1.Items.Add(item1);
        }
    }
}
