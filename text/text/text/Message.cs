using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sio = System.IO;

namespace text.text
{
    public class Message
    {
        public List<TextItem> items;

        public Message(sio.FileInfo path, Table table)
        {
            items = new List<TextItem>();

            using (sio.FileStream fs = new sio.FileStream(path.FullName, sio.FileMode.Open, sio.FileAccess.Read))
            {
                readItems(fs, table);
            }
        }

        private void readItems(sio.Stream stream, Table table)
        {
            stream.Position = 0xc0;
            new TextItem(stream, table);
        }
    }
}
