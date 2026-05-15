using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDEditor
{
    internal interface IExportable
    {
        bool Export(string filename, in List<MSDEntry> data);
    }
}
