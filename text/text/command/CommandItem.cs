using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text.command
{
    public class CommandTextPart : text.TextPart
    {
        private static readonly Type[] types = new Type[] {
            null, null, null, null, typeof(Command_04_manual_close), null, null, null, typeof(Command_08_no_operation), null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, typeof(Command_1f_person_avatar),
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, typeof(Command_3f_reset_text_speed),
            null, null, null, null, null, null, null, typeof(Command_47_close_pause), typeof(Command_48_person_voice), null, null, null, null, null, null, null,
        };

        public CommandTextPart(string name, byte[] arr)
        {
            string bytesString = string.Join("", arr.Skip(2).Select(x => string.Format("{0:x2}", x)));

            value = "<$" + name + ((bytesString.Length > 0) ? " " + bytesString : "") + ">";
        }

        public static CommandTextPart Create(byte[] arr)
        {
            Type type = types[arr[1]];

            if (type == null)
            {
                throw new Exception(string.Format("Unknown command: {0:x2}", arr[1]));
                //return new CommandTextPart(string.Format("command_{0:x2}", arr[1]), arr);
            }
            else
            {
                return (CommandTextPart)Activator.CreateInstance(type, arr);
            }
        }
    }
}
