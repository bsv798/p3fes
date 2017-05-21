#define JINN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace text
{
    public class ArchiveManager
    {
        public List<MsgTextArchive> archs;

        public ArchiveManager()
        {
            archs = new List<MsgTextArchive>();
        }

        public void LoadTable(string tblname)
        {
            System.IO.StreamReader sr = StreamReaders.OpenTextStream(tblname, Streams.uncEnc).strRead;
            int idx;
            char chr;
            string s = sr.ReadLine();

            while (s != null)
            {
                if (s.Length > 0)
                    if (s[0] != ';')
                        if ((idx = s.IndexOf('=')) > 0)
                        {
                            chr = s[idx + 1];
                            idx = Convert.ToInt32(s.Substring(0, idx), 16);
                            StreamReaders.tblExp[idx] = chr;
                            if (StreamWriters.tblImp[(ushort)chr] == 0)
                                StreamWriters.tblImp[(ushort)chr] = (ushort)idx;
                        }
                        else
                        {
                            idx = Convert.ToInt32(s, 16);
                            StreamReaders.tblExp[idx] = (char)idx;
                            if (StreamWriters.tblImp[idx] == 0)
                                StreamWriters.tblImp[idx] = (ushort)idx;
                        }
                s = sr.ReadLine();
            }

            sr.Close();
        }

        public void LoadArchive(string fileName)
        {
            MsgTextArchive arch;
            ArchiveDescriptor desc;

            desc = GetArchiveDescriptor(fileName);
            if (desc == null)
                return;
            switch (desc.type)
            {
                case 0:
                    arch = new MsgTextArchive();
                    arch.LoadArchive();
                    break;
                case 1:
                    goto case 0;
                default:
                    throw new NotSupportedException(string.Format("Unknown archive type '{0}'", fileName));
            }
            if (!Exists(arch))
            {
                archs.Add(arch);
                arch.AddDescriptor(desc);
            }
            else
                Find(arch).AddDescriptor(desc);
        }

#if JINN
        public void LoadJinnText(string fileName)
        {
            MsgTextArchive arch;

            arch = new MsgTextArchive();
            arch.JinnLoadText();

            if (!Exists(arch))
            {
                throw new NullReferenceException(string.Format("jinn file '{0}' doesn't match any loaded file", fileName));
            }
            else
            {
                Streams.writer.strWrite.WriteLine(fileName);
                arch = (MsgTextArchive)Find(arch);
                foreach (var desc in arch.descs)
                    Streams.writer.strWrite.WriteLine(desc.fileName);
                Streams.writer.strWrite.WriteLine();
            }
        }
#endif

        public bool Exists(MsgTextArchive arch)
        {
            foreach (var item in archs)
                if (item == arch)
                    return true;

            return false;
        }

        public TextArchive Find(MsgTextArchive arch)
        {
            foreach (var item in archs)
                if (item == arch)
                    return item;

            return null;
        }

        public ArchiveDescriptor GetArchiveDescriptor(string fileName)
        {
            int len;
            int cnt;
            long pos;
            int type;

            switch (TextArchive.reader.binRead.ReadInt32())
            {
                case 0x0:
                    int i;

                    len = TextArchive.reader.binRead.ReadInt32();
                    if (TextArchive.reader.binRead.ReadInt32() != 0x30574c46) //flw0
                        goto default;
                    TextArchive.reader.binRead.ReadInt32();

                    cnt = TextArchive.reader.binRead.ReadInt32();
                    TextArchive.reader.str.Position += 0xc;
                    pos = TextArchive.reader.str.Position;
                    for (i = 0; i < cnt; i++)
                    {
                        TextArchive.reader.str.Position = pos;
                        TextArchive.reader.str.Position += 0x8;
                        len = TextArchive.reader.binRead.ReadInt32();
                        TextArchive.reader.str.Position = TextArchive.reader.binRead.ReadInt32();
                        if (TextArchive.reader.binRead.ReadInt32() == 0x7)
                            if (TextArchive.reader.binRead.ReadInt32() > 0)
                                if (TextArchive.reader.binRead.ReadInt32() == 0x3147534D) //MSG1
                                    break;
                        pos += 0x10;
                    }
                    if (i < cnt)
                    {
                        type = 1;
                        TextArchive.reader.str.Position -= 0xc;
                        break;
                    }
                    goto default;
                case 0x7:
                    len = TextArchive.reader.binRead.ReadInt32();
                    if (TextArchive.reader.binRead.ReadInt32() != 0x3147534D) //MSG1
                        goto default;
                    type = 0;
                    TextArchive.reader.str.Position = 0;
                    break;
                default:
                    return null;//throw new NotSupportedException(string.Format("File '{0}' is not supported", fileName));
            }
            TextArchive.offset = (int)TextArchive.reader.str.Position;

            return new ArchiveDescriptor(type, fileName, TextArchive.offset);
        }
    }
}
