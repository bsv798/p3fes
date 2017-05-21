using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    class Command_3f_reset_text_speed : CommandTextPart
    {
        public static readonly string name = "reset_text_speed";

        public Command_3f_reset_text_speed(byte[] arr) : base(name, arr)
        {
        }
    }
}
