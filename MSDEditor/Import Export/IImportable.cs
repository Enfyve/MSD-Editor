using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDEditor
{
    internal interface IImportable
    {
        bool Import(string filename, out List<MSDEntry> data);
    }
}
