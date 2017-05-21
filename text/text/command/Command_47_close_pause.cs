using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    /// <summary>
    /// Number of frames the game waits after text rendering is complete, before closing window. Only applicable when dialog mode set to auto (83A24C mod 20).
    /// </summary>
    class Command_47_close_pause : CommandTextPart
    {
        public static readonly string name = "close_pause";

        public Command_47_close_pause(byte[] arr) : base(name, arr)
        {
        }
    }
}
