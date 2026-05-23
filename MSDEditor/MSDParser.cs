using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MSDEditor
{
    class MSDParser
    {
        private static readonly byte[] MSD_Magic = { 0x4D, 0x53, 0x44, 0x41, 0x00, 0x00, 0x01, 0x00 }; // "MSDA" + unknown flags
        private const UInt32 FF3_Flag = 0xCCCC0101;
        private const UInt32 FF4_Flag = 0xCCCC0001;
        private const int Header_Length = 16;

        // MSD files are sufficiently small enough to fit in memory
        // We can read into the memory stream and just decode from that as necessary.
        private MemoryStream msdContentStream;
        private int entryCount;

        public MSDParser()
        {
            msdContentStream = new MemoryStream();
        }

        /// <summary>
        /// Attempts to load an MSD file into memory.
        /// Must be called before any other method.
        /// </summary>
        /// <param name="filePath">The absolute path to the file to try loading.</param>
        /// <returns>true if the file contains the expected magic and the MemoryStream is prepared, false if otherwise</returns>
        public bool TryLoadFile(string filePath)
        {
            try
            {
                using (var file = File.OpenRead(filePath))
                {
                    var reader = new BinaryReader(file);
                    
                    // Abort if the file doesn't begin with an appropriate magic
                    if (!reader.ReadBytes(8).SequenceEqual(MSD_Magic)) 
                        throw new FileFormatException("Magic missing or malformed");

                    entryCount = reader.ReadInt32();

                    reader.BaseStream.Position = 0;

                    // Copy the all data into the MemoryStream
                    reader.BaseStream.CopyTo(msdContentStream);

                    return true;
                }
            }
            catch (Exception)
            {
                // Swallow the error; TODO: log to file
                return false;
            }
        }

        /// <summary>
        /// Decodes an existing MSD MemoryStream into MSD Entries using the provided encoding
        /// </summary>
        /// <param name="encoding">The encoding to use when decoding bytes to strings</param>
        /// <param name="entries">The List to store MSDEntries in</param>
        public void DecodeEntries(Encoding encoding, ref List<MSDEntry> entries)
        {
            // Skip past the header
            msdContentStream.Position = Header_Length;

            var binReader = new BinaryReader(msdContentStream);

            List<uint> ids = new List<uint>(entryCount);
            List<int> offsets = new List<int>(entryCount);

            // Read items table
            for (int i = 0; i < entryCount; i++)
            {
                ids.Add(binReader.ReadUInt32());

                // It's unclear what these bytes are used for, but both games do some form of error checking
                // with them it would appear. We write them back later (see: FF3_Flag and FF4_Flag)
                binReader.BaseStream.Seek(4, SeekOrigin.Current);

                // We don't need to use offsets to read the file since we're reading sequentially, but
                // collecing them makes calculations of the text length simpler.
                offsets.Add(binReader.ReadInt32());
            }

            for (int j = 0; j < entryCount; j++)
            {
                int textLength;

                if (j + 1 < offsets.Count)
                    textLength = (offsets[j + 1] - offsets[j]);
                else
                    textLength = (int)(binReader.BaseStream.Length - offsets[j]);

                var rawBytes = binReader.ReadBytes(textLength);

                entries.Add(new MSDEntry(ids[j], encoding.GetString(rawBytes)));
            }
        }

        /// <summary>
        /// Encodes the supplied entries into bytes, writing them to the loaded MSD MemoryStream.
        /// Call this before calling TryWriteFile.
        /// </summary>
        /// <param name="entries">The entries to write back to the memoryStream</param>
        /// <param name="encoding">The encoding to use when converting strings to raw bytes</param>
        /// <param name="target">Enum representing which flag to write for the items table</param>
        public void EncodeEntries(in List<MSDEntry> entries, Encoding encoding, TargetGame target)
        {
            // We are assuming that the Count in 'entries' remains the same as 'entryCount'

            // Nothing in the header changes, skip past it
            msdContentStream.Position = Header_Length;

            int textPtr = Header_Length + (entryCount * 12); // each item in the items table requires 12 bytes

            var binWriter = new BinaryWriter(msdContentStream);

            // Write the items table
            for (int i = 0; i < entryCount; i++)
            {
                binWriter.Write(entries[i].Id);

                switch (target)
                {
                    case TargetGame.FF3:
                        binWriter.Write(FF3_Flag);
                        break;
                    default:
                        binWriter.Write(FF4_Flag);
                        break;
                }

                binWriter.Write(textPtr);

                textPtr += encoding.GetByteCount(entries[i].Text);
            }

            foreach (var entry in entries)
            {
                binWriter.Write(encoding.GetBytes(entry.Text));
            }

            binWriter.Flush();
        }

        /// <summary>
        /// Attempts to write the contents of the loaded MemoryStream to the specified file
        /// </summary>
        /// <param name="filePath">The absolute path of the file to write to</param>
        /// <returns>true if the file was written successfully, false otherwise</returns>
        public bool TryWriteFile(string filePath)
        {
            msdContentStream.Position = 0;

            try
            {
                using (var file = File.Open(filePath, FileMode.Truncate))
                {
                    msdContentStream.CopyTo(file);

                    return true;
                }
            }
            catch (Exception)
            {
                // TODO: log to file, don't swallow
                return false;
            }
        }
    }
}
