using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MSDEditor
{
    public partial class MSDEditor : Form
    {
        MSDParser parser;
        string filePath;
        bool fileLoaded = false;
        public MSDEditor()
        {
            InitializeComponent();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            filePath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            ParseMSD(filePath);
        }

        private void ParseMSD(string filePath)
        {
            parser = new MSDParser(filePath);

            if (parser.IsValidFile())
            {
                dataGridView1.DataSource = parser.LoadEntries(FF4rb.Checked);
                fileLoaded = true;
                openedFileName.Text = Path.GetFileName(filePath);
            }
            else
            {
                MessageBox.Show("This is not a valid MSD file");
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void OpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ParseMSD(openFileDialog.FileName);
            }
        }

        private void SaveFile_Click(object sender, EventArgs e)
        {
            if (fileLoaded)
            {
                if (parser.Export(dataGridView1.DataSource as List<MSDEntry>, FF4rb.Checked))
                    modifiedState.Text = "";
                else
                    MessageBox.Show("Could not save file");
            }
        }

        private void CloseFile_Click(object sender, EventArgs e)
        {
            filePath = "";
            fileLoaded = false;
            dataGridView1.DataSource = new List<MSDEntry>();
            openedFileName.Text = "no file loaded";

        }

        private void DecodingFormatChanged(object sender, EventArgs e)
        {
            if (fileLoaded)
                dataGridView1.DataSource = parser.LoadEntries(FF4rb.Checked);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string newValue = (dataGridView1.DataSource as List<MSDEntry>)[e.RowIndex].Text;

            // Restore null byte padding if not present
            if (!newValue.EndsWith("\0\0"))
            {
                (dataGridView1.DataSource as List<MSDEntry>)[e.RowIndex].Text = newValue + "\0\0";
            }

            modifiedState.Text = "(modified)";
        }
    }
}
