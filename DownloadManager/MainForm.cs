﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            DownloadFile frm = new DownloadFile(this);
            frm.Show();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure want to delete this record?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                for (int i = listView1.SelectedItems.Count; i > 0; i--)
                {
                    ListViewItem item = listView1.SelectedItems[i - 1];
                    DatabaseManager.Database.FilesData.Rows[item.Index].Delete();
                    listView1.Items[item.Index].Remove();
                }
                DatabaseManager.Database.AcceptChanges();
                DatabaseManager.Database.WriteXml(string.Format("{0}/data.dat", Application.StartupPath));
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string fileName = string.Format("{0}/data.dat", Application.StartupPath);
            if (File.Exists(fileName))
                DatabaseManager.Database.ReadXml(fileName);
            foreach (var row in DatabaseManager.Database.FilesData)
            {
                ListViewItem item = new ListViewItem(row.Id.ToString());
                item.SubItems.Add(row.Url);
                item.SubItems.Add(row.FileName);
                item.SubItems.Add(row.FileSize);
                item.SubItems.Add(row.DateTime.ToLongDateString());
                item.SubItems.Add(row.Path);
                listView1.Items.Add(item);
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            DownloadFiles frm3 = new DownloadFiles(this);
            frm3.Show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            DownloadFtp frm4 = new DownloadFtp(this);
            frm4.Show();
        }

        private void toolStripButton3_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
