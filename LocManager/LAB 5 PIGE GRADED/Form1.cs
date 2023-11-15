using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ZipArchive zipArchive;
        private List<TreeNode> entries = new List<TreeNode>();
        private List<LocEntry> locEntries = new List<LocEntry>();

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Zip Files(*.ZIP)|*.ZIP;";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string zipFilePath = openFileDialog.FileName;

                zipArchive = ZipFile.OpenRead(zipFilePath);

                Dictionary<string, object> tree = new Dictionary<string, object>();

                treeView1.Nodes.Clear();

                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    using (StreamReader reader = new StreamReader(entry.Open()))
                    {
                        LocEntry? jsonString = System.Text.Json.JsonSerializer.Deserialize<LocEntry>(reader.ReadToEnd());
                        locEntries.Add(jsonString);
                        var path = jsonString?.HierarchyPath.Split("-");
                        TreeNode? currentNode = null;
                        for (int i = 0; i < path?.Length; i++)
                        {
                            string nodeName = path[i];
                            TreeNode? node = null;
                            if (i == 0)
                            {
                                // Create or get root node
                                node = treeView1.Nodes.ContainsKey(nodeName)
                                    ? treeView1.Nodes[nodeName]
                                    : treeView1.Nodes.Add(nodeName, nodeName);
                            }
                            else
                            {
                                // Create or get child node
                                node = currentNode.Nodes.ContainsKey(nodeName)
                                    ? currentNode.Nodes[nodeName]
                                    : currentNode.Nodes.Add(nodeName, nodeName);
                            }
                            currentNode = node;

                            if (i == (path.Length - 1))
                            {
                                var entryNode = new TreeNode(jsonString?.EntryName);
                                currentNode.Nodes.Add(entryNode);
                                entries.Add(entryNode);
                                entryNode.Tag = jsonString;
                                entryNode.ImageKey = "file";
                                entryNode.SelectedImageKey = "file";
                            }
                        }
                    }
                }
            }
        }
        TreeNode selectedNode;
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            selectedNode = e.Node;
            LocEntry? locEntry = selectedNode?.Tag as LocEntry;
            if (locEntry != null)
            {
                if (selectedNode != null)
                {
                    textBox1.Text = selectedNode.FullPath.Replace("\\", "-");
                }

                listView1.Items.Clear();

                foreach (KeyValuePair<Language, string> a in locEntry.Translations)
                {
                    textBox2.Text = a.Value;
                    ListViewItem listviewitem = new ListViewItem(new string[] { a.Key.ToString(), a.Value });
                    listView1.Items.Add(listviewitem);
                }
            }
            else
            {
                return;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            var locEntry = selectedNode.Tag as LocEntry;
            if (locEntry != null)
            {
                foreach (var l in locEntries)
                {
                    if (l == locEntry)
                    {
                        if (listView1.Items.Count > 0)
                        {
                            var language = (Language)Enum.Parse(typeof(Language), listView1.Items[0].SubItems[0].Text);
                            if (locEntry.Translations.ContainsKey(language))
                            {
                                var newValue = textBox2.Text;
                                var oldValue = locEntry.Translations[language];
                                if (newValue != oldValue)
                                {
                                    l.Translations[language] = newValue;
                                    listView1.Items[0].SubItems[1].Text = newValue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private ImageList icons = new ImageList();

        ToolStripButton TranslateButton;
        ToolStripProgressBar progressBar;
        ToolStripMenuItem clickedItem;
        private void Form1_Load(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
            icons.Images.Add("folder", LAB_5_PIGE_GRADED.Properties.Resources.folder.ToBitmap());
            icons.Images.Add("file", LAB_5_PIGE_GRADED.Properties.Resources.file.ToBitmap());

            treeView1.ImageList = icons;

            TreeNode root = new TreeNode("<ROOT>");
            root.ImageKey = "folder";
            root.SelectedImageKey = "folder";
            treeView1.ImageList = icons;
            treeView1.Nodes.Add(root);


            StatusStrip statusStrip = new StatusStrip();

            TranslateButton = new ToolStripButton();
            TranslateButton.Text = "Translate";
            TranslateButton.Click += Translate_Click;
            statusStrip.Items.Add(TranslateButton);

            ToolStripDropDownButton dropDownButton = new ToolStripDropDownButton();
            dropDownButton.DropDownItems.Add("English");
            dropDownButton.DropDownItems.Add("Portuguese");
            dropDownButton.DropDownItems.Add("Polish");
            dropDownButton.DropDownItems.Add("Chinese");
            dropDownButton.DropDownItems.Add("Spanish");
            statusStrip.Items.Add(dropDownButton);

            dropDownButton.DropDownItemClicked += (sender, e) =>
            {
                foreach (ToolStripMenuItem item in dropDownButton.DropDownItems)
                {
                    item.Checked = false;
                }

                clickedItem = (ToolStripMenuItem)e.ClickedItem;
                clickedItem.Checked = true;
            };

            ToolStripStatusLabel spring = new ToolStripStatusLabel();
            spring.Spring = true;
            statusStrip.Items.Add(spring);

            progressBar = new ToolStripProgressBar();
            progressBar.Alignment = ToolStripItemAlignment.Right;
            progressBar.Maximum = 100;
            statusStrip.Items.Add(progressBar);

            // Add the status strip to the form
            this.Controls.Add(statusStrip);

            textBox2.TextChanged += textBox2_TextChanged;

        }

        //private void Translate_Click(object sender, EventArgs e)
        //{
        //    TranslateButton.Enabled = false;

        //    for (int i = 0; i <= 50; i++)
        //    {
        //        progressBar.Value = i;
        //        Thread.Sleep(20);
        //    }
        //    progressBar.Value = 0;
        //    TranslateButton.Enabled = true;
        //}
        private void Translate_Click(object sender, EventArgs e)
        {
            if (selectedNode != null && entries.Contains(selectedNode))
            {
                backgroundWorker1.WorkerReportsProgress = true;
                backgroundWorker1.RunWorkerAsync();
            }
        }
        string translatedText;
        string description;
        string text;
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            FieldInfo fieldInfo = typeof(Language).GetField(clickedItem.ToString());

            if (fieldInfo != null)
            {
                DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes != null && attributes.Length > 0)
                {
                    description = attributes[0].Description;
                }
            }
            string language = "EN";
            string translateto = description;

            LocEntry locEntry = (LocEntry)selectedNode.Tag;
            int i = 0;
            foreach (KeyValuePair<Language, string> translation in locEntry.Translations)
            {
                text = translation.Value;
                int progressPercentage = (int)((float)(i + 1) / locEntry.Translations.Count * 100);
                backgroundWorker1.ReportProgress(progressPercentage);
                i++;
            }
            translatedText = Translator.Translate(language, translateto, text);

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ListViewItem item = new ListViewItem(new string[] { clickedItem.Text, translatedText });
            listView1.Items.Add(item);
            progressBar.Value = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();

            string searchText = textBox3.Text;

            foreach (var entry in entries)
            {
                LocEntry? locEntry = entry.Tag as LocEntry;
                if (entry.Text.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (KeyValuePair<Language, string> a in locEntry.Translations)
                    {
                        ListViewItem listviewitem = new ListViewItem(new string[] { locEntry.ToString(), entry.FullPath, a.Value });
                        listView2.Items.Add(listviewitem);
                    }
                }
            }
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                button1.PerformClick();
                e.Handled = true;
            }
        }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            TreeNode selectedNode = treeView1.GetNodeAt(e.X, e.Y);
            if (selectedNode == null) return;

            treeView1.SelectedNode = selectedNode;

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem newGroupItem = new ToolStripMenuItem("New Group");
            newGroupItem.Click += NewGroupItem_Click;
            menu.Items.Add(newGroupItem);

            ToolStripMenuItem newSubgroupItem = new ToolStripMenuItem("New Subgroup");
            newSubgroupItem.Click += NewSubgroupItem_Click;
            menu.Items.Add(newSubgroupItem);

            ToolStripMenuItem deleteNodeItem = new ToolStripMenuItem("Delete Group");
            deleteNodeItem.Click += DeleteNodeItem_Click;
            menu.Items.Add(deleteNodeItem);

            menu.Show(treeView1, e.Location);
        }

        private void NewGroupItem_Click(object sender, EventArgs e)
        {
            TreeNode newNode = new TreeNode("<NEW GROUP>");
            if (treeView1.SelectedNode.Parent == null)
            {
                treeView1.Nodes.Add(newNode);
            }
            else
            {
                treeView1.SelectedNode.Parent.Nodes.Add(newNode);
            }
            treeView1.SelectedNode = newNode;
            treeView1.LabelEdit = true;
            newNode.BeginEdit();
        }

        private void NewSubgroupItem_Click(object sender, EventArgs e)
        {
            TreeNode newSubNode = new TreeNode("<NEW SUBGROUP>");
            treeView1.SelectedNode.Nodes.Add(newSubNode);
            treeView1.SelectedNode = newSubNode;
            treeView1.LabelEdit = true;
            newSubNode.BeginEdit();
        }

        private void DeleteNodeItem_Click(object sender, EventArgs e)
        {
            treeView1.SelectedNode.Remove();
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Zip Files(*.ZIP)|*.ZIP;";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (ZipArchive outputZip = ZipFile.Open(saveFileDialog.FileName, ZipArchiveMode.Create))
                {

                    foreach (var locEntry in locEntries)
                    {
                        var jsonFile = JsonConvert.SerializeObject(locEntry);
                        ZipArchiveEntry outputEntry = outputZip.CreateEntry($"Lockey#{locEntry.LocKey}.json");
                        using (StreamWriter writer = new StreamWriter(outputEntry.Open()))
                        {
                            writer.Write(jsonFile);
                        }
                    }
                }
            }
        }
        TreeNode newNode = new TreeNode();
        private void newEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            newNode.ImageKey = "file";
            newNode.SelectedImageKey = "file";
            newNode.Tag = treeView1.SelectedNode.Text;
            treeView1.SelectedNode.Nodes.Add(newNode);
            treeView1.SelectedNode = newNode;
            textBox1.Text = newNode.FullPath.Replace("\\", "-");
            textBox2.Clear();
            textBox1.ReadOnly = false;    
        }

        private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;
            treeView1.SelectedNode.Remove();
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            string[] path = textBox1.Text.Split("-");
            newNode.Text = path[path.Length - 1];
            textBox1.ReadOnly = true;

            string h = selectedNode.FullPath.Replace("\\", "-");
            string en = newNode.Text;
            LocEntry loc = new LocEntry(h, en);
            listView1.Items.Add("Debug", textBox2.Text);
            locEntries.Add(loc);
        }
    }
}
