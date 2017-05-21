using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sio = System.IO;

namespace text
{
    class Program
    {
        static void Main(string[] args)
        {
            sio.FileInfo tablePath = new System.IO.FileInfo(@"e:\Trance\p3fes\git\Persona_3_FES\en\font\table.tbl");
            Table table = new Table(tablePath);
            sio.FileInfo messageePath = new System.IO.FileInfo(@"e:\Trance\p3fes\res\data.cvm\EVENT\E100\0005.e105_001.msg");
            text.Message message = new text.Message(messageePath, table);
        }
    }
}
