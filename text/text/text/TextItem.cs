using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sio = System.IO;

namespace text.text
{
    public class TextItem
    {
        private List<TextPart> parts;

        public TextItem(sio.Stream stream, Table table)
        {
            parts = new List<text.TextPart>();

            readItem(stream, table);
        }

        private void readItem(sio.Stream stream, Table table)
        {
            TextPart textPart = new TextPart();

            using (sio.BinaryReader br = new sio.BinaryReader(stream))
            {
                byte b = br.ReadByte();

                while (b > 0)
                {
                    if (b < 0x80)
                    {
                        TableItem<byte> tableItem = table.One[b];

                        if (tableItem != null)
                        {
                            textPart.value += tableItem.value;
                        }
                        else
                        {
                            parts.Add(textPart);
                            textPart = new text.TextPart();

                            parts.Add(new command.HexTextPart(b));
                        }
                    }
                    else if (b < 0xf0)
                    {
                        byte b1 = br.ReadByte();
                        ushort u = (ushort)((b << 0x08) | b1);
                        TableItem<ushort> tableItemU = table.Two[u];
                        TableItem<byte> tableItemB = ((u - 0x8080 + 0x20) < 0x60) ? table.One[u - 0x8080 + 0x20] : null;

                        if (tableItemU != null)
                        {
                            textPart.value += tableItemU.value;
                        }
                        else if (tableItemB != null)
                        {
                            textPart.value += tableItemB.value;
                        }
                        else
                        {
                            parts.Add(textPart);
                            textPart = new text.TextPart();

                            parts.Add(new command.HexTextPart(b));
                            parts.Add(new command.HexTextPart(b1));
                        }
                    }
                    else
                    {
                        int hb = b & 0x0f;
                        int cnt = hb * 2;
                        byte[] arr = new byte[cnt];

                        arr[0] = b;
                        br.Read(arr, 1, cnt - 1);
                        parts.Add(command.CommandTextPart.Create(arr));
                    }
                    b = br.ReadByte();
                }
            }
        }

        public override string ToString()
        {
            string res = "";

            foreach (var part in parts)
            {
                res += part.value;
            }

            return res;
        }
    }
}
