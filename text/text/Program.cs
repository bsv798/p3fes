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
            sio.FileInfo p = new System.IO.FileInfo(@"e:\Trance\p3fes\git\Persona_3_FES\en\font\table.tbl");
            Table t = new Table(p);
        }
    }
}
