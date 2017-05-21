using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    class Command_08_no_operation : CommandTextPart
    {
        public static readonly string name = "no_operation";

        public Command_08_no_operation(byte[] arr) : base(name, arr)
        {
        }
    }
}
