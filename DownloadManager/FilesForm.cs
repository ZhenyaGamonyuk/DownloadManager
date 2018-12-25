using HtmlAgilityPack;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class FilesForm : Form
    {
        private MainForm _mainForm;
        WebClient client;

        public FilesForm(MainForm frm)
        {
            InitializeComponent();
            _mainForm = frm;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if(textBox2.Text == "")
                {
                    MessageBox.Show("Choose file type");
                }
                listView1.Items.Clear();
                String[] words = textBox2.Text.Split(new char[] { ';', '*' }, StringSplitOptions.RemoveEmptyEntries);
                var doc = new HtmlWeb().Load(textBox1.Text);
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");
                ListViewItem checkItem = new ListViewItem();
                listView1.Items.Add(checkItem);
                foreach (var s in words)
                {
                    if (s.Trim() != "")
                    {
                        foreach (var n in nodes)
                        {
                            string href = n.Attributes["href"].Value;
                            if (href.Contains(s))
                            {
                                var baseUrl = new Uri(textBox1.Text);
                                var url = new Uri(baseUrl, href);
                                ListViewItem item = new ListViewItem(url.AbsoluteUri);
                                item.SubItems.Add("");
                                item.SubItems.Add("");
                                item.SubItems.Add("-");
                                listView1.Items.Add(item);
                            }
                        }
                    }
                }
                for (int i = 1; i < listView1.Items.Count; i++)
                {
                    var size = FileSize(listView1.Items[i].SubItems[0].Text);
                    var url = new Uri(listView1.Items[i].SubItems[0].Text);
                    listView1.Items[i].SubItems[1].Text = System.IO.Path.GetFileName(url.AbsolutePath);
                    listView1.Items[i].SubItems[2].Text = size;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error has occurred. Please check your internet connection." + Environment.NewLine +
                    Environment.NewLine + "More details: " + ex.ToString());
            }
            finally
            {
                button1.Enabled = true;
            }
        }

        private void IsExist()
        {
            for (int i = 1; i < listView1.Items.Count; i++)
            {
                string path = textBox3.Text + listView1.Items[i].SubItems[1].Text;
                if (File.Exists(path))
                {
                    listView1.Items[i].SubItems[3].Text = "true";
                }
                else
                {
                    listView1.Items[i].SubItems[3].Text = "false";
                }
            }
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
                    textBox3.Text = fbd.SelectedPath;
                    Properties.Settings.Default.Path = textBox3.Text;
                    Properties.Settings.Default.Save();
                }
            }
            IsExist();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var errorList = "";
            button1.Enabled = false;
            if (textBox3.Text == "")
            {
                errorList += "Select download folder\n";
            }
            if (errorList != "")
            {
                MessageBox.Show(errorList);
                button1.Enabled = true;
                return;
            }
            FileModel fm = new FileModel(_mainForm, listView1.CheckedItems.Count);
            fm.ListDownload(textBox3.Text);
            IsExist();
            prgrBar1.Value = 0;
            prgrBar2.Value = 0;
            button1.Enabled = true;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (Char.IsDigit(number) && number != 8 && number != 42 && number != 46 && number != 59) // цифры и клавиша BackSpace
            {
                e.Handled = true;
            }
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

        private string FileSize(string url)
        {
            using(client = new WebClient())
            {
                Stream stream = client.OpenRead(url);
                long bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                stream.Dispose();
                return ToFileSize(bytes_total);
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
    }
}
