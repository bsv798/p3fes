using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sio = System.IO;
using ste = System.Text.Encoding;
using sri = System.Runtime.InteropServices;

namespace text
{
    public class Table
    {
        public TableItem<byte>[] One { get; set; }
        public TableItem<byte>[] OneReverse { get; set; }
        public TableItem<ushort>[] Two { get; set; }
        public TableItem<ushort>[] TwoReverse { get; set; }

        public Table(sio.FileInfo path)
        {
            using (sio.StreamReader sr = new System.IO.StreamReader(path.FullName, ste.UTF8))
            {
                string line = sr.ReadLine();

                One = new TableItem<byte>[byte.MaxValue];
                OneReverse = new TableItem<byte>[byte.MaxValue];
                Two = new TableItem<ushort>[ushort.MaxValue];
                TwoReverse = new TableItem<ushort>[ushort.MaxValue];
                while (line != null)
                {
                    if ((line.Length != 0) && (line[0] != '#'))
                    {
                        string[] arr = line.Split(new char[] { '=' }, 2);
                        string strCode = arr[0];
                        string value = arr[1];
                        int strCodeLen = strCode.Length >> 1;

                        if (strCodeLen == 2)
                        {
                            ushort code = (Convert.ToUInt16(strCode, 16));
                            TableItem<ushort> tableItem = new TableItem<ushort>(code, value[0]);

                            Two[code] = tableItem;
                            TwoReverse[tableItem.value] = tableItem;
                        }
                        else
                        {
                            byte code = (Convert.ToByte(strCode, 16));
                            TableItem<byte> tableItem = new TableItem<byte>(code, value[0]);

                            One[code] = tableItem;
                            OneReverse[ste.ASCII.GetBytes(tableItem.value.ToString())[0]] = tableItem;
                        }

                        line = sr.ReadLine();
                    }
                }
            }
        }
    }

    public class TableItem<T>
    {
        public T code;
        public char value;

        public TableItem(T code, char value)
        {
            this.code = code;
            this.value = value;
        }

        public override string ToString()
        {
            int len = sri.Marshal.SizeOf(default(T)) << 1;
            string strHex = string.Format("{{0:x{0}}}", len);

            return string.Format("{0}={1}", string.Format(strHex, code), value);
        }
    }
}
