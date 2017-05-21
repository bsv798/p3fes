using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    class Command_48_person_voice : CommandTextPart
    {
        public static readonly string name = "person_voice";

        public Command_48_person_voice(byte[] arr) : base(name, arr)
        {
        }
    }
}
