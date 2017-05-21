using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    class Command_04_manual_close : CommandTextPart
    {
        public static readonly string name = "manual_close";

        public Command_04_manual_close(byte[] arr) : base(name, arr)
        {
        }
    }
}
