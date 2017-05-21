using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    class HexTextPart : text.TextPart
    {
        public HexTextPart(byte val)
        {
            value = string.Format("<#{0:x2}>", val);
        }
    }
}
