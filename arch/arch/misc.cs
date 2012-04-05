using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace arch
{
    public static class misc
    {
        public static char dirSep = sio.Path.DirectorySeparatorChar;

        public static string AddEndSep(string path)
        {
            if (path.LastIndexOf(dirSep) != path.Length - 1)
                return path + dirSep;

            return path;
        }

        public static void AlignWrite(this sio.Stream fs, int alg)
        {
            long mod;
            byte[] buf;

            mod = fs.Position & (alg - 1);
            if (mod > 0)
            {
                buf = new byte[alg - mod];
                fs.Write(buf, 0, buf.Length);
            }
        }

        public static void AlignPos(this sio.Stream fs, int alg)
        {
            long mod;

            mod = fs.Position & (alg - 1);
            if (mod > 0)
                fs.Position += alg - mod;
        }

        public static long Align(long l, int alg)
        {
            long mod;

            mod = l & (alg - 1);
            if (mod > 0)
                l += alg - mod;
            return l;
        }

        public static void Extract(this sio.Stream str, string path, long length)
        {
            const int bufLen = 0x1000;
            int bufCnt;
            byte[] buf;
            sio.FileStream fw;
            sio.FileInfo fi;

            fi = new sio.FileInfo(path);
            if (!sio.Directory.Exists(fi.Directory.FullName))
                sio.Directory.CreateDirectory(fi.Directory.FullName);
            fw = new sio.FileStream(path, sio.FileMode.Create, sio.FileAccess.Write);

            buf = new byte[bufLen];
            bufCnt = bufLen;
            while (length > 0)
            {
                if (length < bufLen)
                    bufCnt = (int)length;
                str.Read(buf, 0, bufCnt);
                fw.Write(buf, 0, bufCnt);
                length -= bufCnt;
            }

            fw.Close();
        }

        public static string GetFileName(string fullName)
        {
            return new sio.FileInfo(fullName).Name;
        }

        public static string GetParentDir(string fullName)
        {
            return new sio.FileInfo(fullName).Directory.FullName + dirSep;
        }

        public static string InsertCounter(string fileName, int counter)
        {
            int i;

            i = fileName.LastIndexOf(sio.Path.AltDirectorySeparatorChar) + 1;
            if (i < 1)
                i = fileName.LastIndexOf(dirSep) + 1;
            return fileName.Insert(i, string.Format("{0:x4}.", counter));
        }

        public static string RemoveCounter(string fileName)
        {
            int i;

            i = fileName.LastIndexOf(sio.Path.AltDirectorySeparatorChar) + 1;
            if (i < 1)
                i = fileName.LastIndexOf(dirSep) + 1;
            return fileName.Remove(i, 5);
        }

        public static string GetValue(string paramStr, string paramName)
        {
            string[] values;
            string[] parts;

            values = paramStr.Split(',');
            foreach (var value in values)
            {
                parts = value.Split('=');
                if (parts[0].Length == paramName.Length)
                    if (parts[0] == paramName)
                        return parts[1];
            }
            return "";
        }

        public static string ReadNTString(this sio.Stream str, long len)
        {
            int b;
            byte[] bb;
            long lastPos;
            int i;

            bb = new byte[256];
            if (len < 1)
                len = long.MaxValue;
            lastPos = str.Position + len;
            for (i = 0; i < bb.Length; i++)
            {
                if (str.Position == lastPos)
                    break;
                b = str.ReadByte();
                bb[i] = (byte)b;
                if (b == 0)
                    break;
                if (i == bb.Length - 1)
                    Array.Resize<byte>(ref bb, bb.Length + 256);
            }

            if (i > 0)
                return ste.ASCII.GetString(bb, 0, i);
            else
                return "";
        }
    }
}
