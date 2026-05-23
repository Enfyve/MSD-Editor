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
        
        bool fileLoaded = false;
        string loadedFilePath;

        List<MSDEntry> msdEntryDataSource = new List<MSDEntry>();
        
        public MSDEditor()
        {
            InitializeComponent();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            ParseMSD(droppedFiles[0]);
        }

        private void ParseMSD(string filePath)
        {
            parser = new MSDParser();

            if (parser.TryLoadFile(filePath))
            {
                loadedFilePath = filePath;
                openedFileName.Text = Path.GetFileName(filePath);

                DecodeAndShow();

                fileLoaded = true;
            }
            else
            {
                MessageBox.Show("This is not a valid MSD file");
            }
        }

        private Encoding GetEncodingFromSelection()
        {
            if (FF4rb.Checked)
            {
                return Encoding.Unicode;
            }
            else
            {
                // All eureka_xyz files are UTF-8
                if (Path.GetFileName(loadedFilePath).StartsWith("eureka"))
                {
                    return Encoding.UTF8; 
                }
                else
                {
                    switch ((LanguageMode)langComboBox.SelectedIndex)
                    {
                        case LanguageMode.English:
                        case LanguageMode.German:
                        case LanguageMode.French:
                        case LanguageMode.Italian:
                        case LanguageMode.Spanish:
                            return Encoding.GetEncoding("Windows-1252");
                        case LanguageMode.Japanese:
                            return Encoding.GetEncoding("SHIFT_JIS");
                        case LanguageMode.Korean:
                        case LanguageMode.Traditional_Chinese:
                        case LanguageMode.Simplified_Chinese:
                        case LanguageMode.Thai:
                        default:
                            return Encoding.UTF8;
                    }
                }
            }
        }

        private void DecodeAndShow()
        {
            msdEntryDataSource.Clear();
            parser.DecodeEntries(GetEncodingFromSelection(), ref msdEntryDataSource);

            dataGridView1.DataSource = new BindingList<MSDEntry>(msdEntryDataSource);
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
            if (!fileLoaded) return;
            
            var encodeTarget = FF4rb.Checked ? TargetGame.FF4 : TargetGame.FF3;

            parser.EncodeEntries(msdEntryDataSource, GetEncodingFromSelection(), encodeTarget);

            if (parser.TryWriteFile(loadedFilePath))
            {
                modifiedState.Text = "";
            }                    
            else
            {
                MessageBox.Show("Could not save file");
            }   
        }

        private void CloseFile_Click(object sender, EventArgs e)
        {
            loadedFilePath = "";
            fileLoaded = false;
            dataGridView1.DataSource = new List<MSDEntry>();
            openedFileName.Text = "no file loaded";
            parser = null;
        }

        private void DecodingFormatChanged(object sender, EventArgs e)
        {
            // language specific encode/decode support for FF3 only
            langGroupBox.Enabled = FF3rb.Checked;

            if (fileLoaded)
            {
                DecodeAndShow();
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string newValue = (dataGridView1.DataSource as BindingList<MSDEntry>)[e.RowIndex].Text;

            // Restore null byte padding if not present
            if (!newValue.EndsWith("\0\0"))
            {
                (dataGridView1.DataSource as BindingList<MSDEntry>)[e.RowIndex].Text = newValue + "\0\0";
            }

            modifiedState.Text = "(modified)";
        }

        private void ExportAs_Click(object sender, EventArgs e)
        {
            exportFileDialog.FileName = Path.GetFileNameWithoutExtension(loadedFilePath);

            if (exportFileDialog.ShowDialog() == DialogResult.OK)
            {
                var ext = Path.GetExtension(exportFileDialog.FileName).ToLower();
                bool exportSuccess = false;

                switch (ext)
                {
                    case ".csv":
                        CSVHandler csvHandler = new CSVHandler();
                        exportSuccess = csvHandler.Export(exportFileDialog.FileName, msdEntryDataSource);
                        break;
                    case ".xlsx":
                        ExcelHandler excelHandler = new ExcelHandler();
                        exportSuccess = excelHandler.Export(exportFileDialog.FileName, msdEntryDataSource);
                        break;
                    default:
                        MessageBox.Show("File format not supported");
                        break;
                }

                if (!exportSuccess)
                    MessageBox.Show("There was a problem exporting the file.");
            }
        }

        private void ImportFile_Click(object sender, EventArgs e)
        {
            if (importFileDialog.ShowDialog() == DialogResult.OK)
            {
                var ext = Path.GetExtension(importFileDialog.FileName).ToLower();

                bool importSuccess = false;
                List<MSDEntry> entries = new List<MSDEntry>();

                switch (ext)
                {
                    case ".csv":
                        CSVHandler csvHandler = new CSVHandler();
                        importSuccess = csvHandler.Import(importFileDialog.FileName, out entries);
                        break;
                    case ".xlsx":
                        ExcelHandler excelHandler = new ExcelHandler();
                        importSuccess = excelHandler.Import(importFileDialog.FileName, out entries);
                        break;
                    default:
                        MessageBox.Show("File format not supported");
                        break;
                }

                
                if (!importSuccess)
                    MessageBox.Show("There was a problem importing the file.");
                else
                {
                    msdEntryDataSource = entries;
                    dataGridView1.DataSource = new BindingList<MSDEntry>(msdEntryDataSource);
                }                    
            }
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            ExportAs.Enabled = fileLoaded;
            ImportFile.Enabled = fileLoaded;
        }

        private void langComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Re-parse the strings
            DecodingFormatChanged(sender, e);
        }

        private void MSDEditor_Load(object sender, EventArgs e)
        {
            // default the dropdown to English (there's no way we can infer this from the files themselves)
            langComboBox.SelectedIndex = (int)LanguageMode.English;
        }
    }
}
