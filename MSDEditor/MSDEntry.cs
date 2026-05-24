using CsvHelper.Configuration.Attributes;
using System;

namespace MSDEditor
{
    public class MSDEntry
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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return (obj as MSDEntry).Id == Id && 
                (obj as MSDEntry).Text == Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Text);
        }
    }
}
