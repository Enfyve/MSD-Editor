using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDEditor
{
    class MSDEntry
    {
        public string Text { get; set; }

        public UInt32 Id { get; set; }

        public MSDEntry(UInt32 id, string text)
        {
            Id = id;
            Text = text;
        }
    }

    class MSDParser
    {
        private static readonly byte[] MSD_Magic = { 0x4D, 0x53, 0x44, 0x41 }; // "MSDA"
        private const UInt32 FF3Flag = 0xCCCC0101;
        private const UInt32 FF4Flag = 0xCCCC0001;
        private string filePath;
        private List<MSDEntry> entries;

        public MSDParser(string filePath)
        {
            this.filePath = filePath;
        }

        public bool IsValidFile()
        {
            using (var file = File.OpenRead(filePath))
            {
                var br = new BinaryReader(file);
                return br.ReadBytes(4).SequenceEqual(MSD_Magic);
            }
        }

        public List<MSDEntry> LoadEntries(bool useUnicode = false)
        {
            entries = new List<MSDEntry>();
            using (var fs = File.OpenRead(filePath))
            {
                var br = new BinaryReader(fs);
                // Skip header and unknown flags
                br.BaseStream.Seek(8, SeekOrigin.Begin);

                // next 4 bytes is the number of sections
                uint count = br.ReadUInt32();
                List<uint> offsets = new List<uint>((int)count);
                List<UInt32> indexes = new List<UInt32>((int)count);

                // Skip 4 bytes
                br.BaseStream.Seek(4, SeekOrigin.Current);

                // Read items table
                for (int i = 0; i < count; i++)
                {
                    indexes.Add(br.ReadUInt32());               // index
                    br.BaseStream.Seek(4, SeekOrigin.Current);  // skip separator (01 00 CC CC for FF4, 01 01 CC CC for FF3)

                    offsets.Add(br.ReadUInt32());               // string position
                }

                // Read strings
                for (int j = 0; j < offsets.Count; j++)
                {
                    int length;

                    if (j < offsets.Count - 1)
                        length = (int)(offsets[j + 1] - offsets[j]);
                    else
                        length = (int)(br.BaseStream.Length - offsets[j]);

                    if (useUnicode)
                        entries.Add(new MSDEntry(indexes[j], Encoding.Unicode.GetString(br.ReadBytes(length))));
                    else
                        entries.Add(new MSDEntry(indexes[j], Encoding.ASCII.GetString(br.ReadBytes(length))));
                }
            }

            return entries;
        }

        public bool Export(List<MSDEntry> entries, bool isUnicode)
        {
            int offset = 16 + (entries.Count * 12); // 16 bytes header + 12 bytes per entry
            try 
            { 
                using (var file = File.Open(filePath, FileMode.Truncate))
                {
                    BinaryWriter bw = new BinaryWriter(file);
                    bw.Seek(0, SeekOrigin.Begin);

                    // Write header
                    bw.Write(MSD_Magic);                            // Magic
                    bw.Write(new byte[]{0x00, 0x00, 0x01, 0x00});   // Unknown flags
                    bw.Write((UInt32)entries.Count);                // number of entries
                    bw.Write(0x00000000);                           // zeroes

                    for (int i = 0; i < entries.Count; i++)
                    {
                        bw.Write(entries[i].Id);                    // ID
                        bw.Write(isUnicode ? FF4Flag : FF3Flag);    // Write flags
                        bw.Write(offset);
                        int strLength = isUnicode ? Encoding.Unicode.GetByteCount(entries[i].Text) : entries[i].Text.Length;
                        offset += strLength; // increment offset by the length of the text entry
                    }

                    // Write data
                    foreach (var entry in entries)
                    {
                        byte[] bytes;
                        if (isUnicode)
                            bytes = Encoding.Unicode.GetBytes(entry.Text);
                        else
                            bytes = Encoding.ASCII.GetBytes(entry.Text);
                        bw.Write(bytes);

                    }

                    bw.Flush();
                    bw.Close();
                }
                return true;
            } 
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
