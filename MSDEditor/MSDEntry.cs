using CsvHelper.Configuration.Attributes;
using System;

namespace MSDEditor
{
    class MSDEntry
    {
        [Name("Id"), Index(0)]
        public UInt32 Id { get; set; }

        [Name("Text"), Index(1)]
        public string Text { get; set; }

        public MSDEntry(UInt32 Id, string Text)
        {
            this.Id = Id;
            this.Text = Text;
        }
    }
}
