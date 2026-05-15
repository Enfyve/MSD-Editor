using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MSDEditor
{
    internal class CSVHandler : IExportable, IImportable
    {
        public bool Export(string filename, in List<MSDEntry> data)
        {
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteHeader<MSDEntry>();
                        csv.NextRecord();

                        foreach (var entry in data)
                        {
                            csv.WriteRecord<MSDEntry>(new MSDEntry(
                                entry.Id,
                                entry.Text.Replace("\n", "\\n").TrimEnd('\0')
                            ));
                            csv.NextRecord();
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
        public bool Import(string filename, out List<MSDEntry> data)
        {
            data = null;

            try
            {
                using (var reader = new StreamReader(filename))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<MSDEntry>();
                    data = new List<MSDEntry>();

                    foreach (var record in records)
                    {
                        record.Text = record.Text.Replace("\\n", "\n") + "\0\0";
                        data.Add(record);
                    }
                }
            }
            catch
            {
                data = null;
                return false;
            }

            return true;
        }
    }
}
