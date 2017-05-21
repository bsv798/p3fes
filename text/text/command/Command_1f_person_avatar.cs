using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    class Command_1f_person_avatar : CommandTextPart
    {
        public static readonly string name = "person_avatar";

        public Command_1f_person_avatar(byte[] arr) : base(name, arr)
        {
        }
    }
}
