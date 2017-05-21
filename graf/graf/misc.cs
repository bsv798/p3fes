using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;
using srl = System.Reflection;

    public static class misc
    {
        public const string programVersion = "0.6";
        public const string assemblyVersion = programVersion + ".*";
        public const string programDescription = "Persona 3 graphics tool";

        public static char dirSep = sio.Path.DirectorySeparatorChar;

        public static string DirCreateAndCheck(string path)
        {
            sio.DirectoryInfo di;

            path = path.CheckEndSep();
            di = new sio.DirectoryInfo(path);
            if (!di.Exists)
                di.Create();

            return di.FullName;
        }

        public static string CheckEndSep(this string str)
        {
            if (str[str.Length - 1] != dirSep)
                return str + dirSep;
            else
                return str;
        }

        public static int ChangeEndian(this int i)
        {
            int res;

            res = i << 0x18;
            res |= (i & 0xff00) << 0x08;
            res |= (i >> 0x08) & 0xff00;
            res |= (i >> 0x18) & 0xff;

            return res;
        }

        public static string GetValue(string paramStr, string paramName)
        {
            string[] values;
            string[] parts;

            paramName = paramName.ToLower();
            values = paramStr.Split(',');
            foreach (var value in values)
            {
                parts = value.Split('=');
                if (parts[0].Length == paramName.Length)
                    if (parts[0].ToLower() == paramName)
                        return parts[1];
            }
            return "";
        }

        public static string ReadNullTerminatedString(this sio.Stream str, int maxBytesLen)
        {
            return str.ReadNullTerminatedString(maxBytesLen, ste.ASCII);
        }

        public static string ReadNullTerminatedString(this sio.Stream str, int maxBytesLen, ste enc)
        {
            byte[] buf;

            if (maxBytesLen < 1)
                return "";

            buf = new byte[maxBytesLen];
            str.Read(buf, 0, buf.Length);
            if (buf[0] == 0)
                return "";
            else
            {
                while (buf[--maxBytesLen] == 0) ; //empty body
                return enc.GetString(buf, 0, ++maxBytesLen);
            }
        }

        public static void WriteNullTerminatedString(this sio.Stream str, string txt, int maxBytesLen)
        {
            str.WriteNullTerminatedString(txt, maxBytesLen, ste.ASCII);
        }

        public static void WriteNullTerminatedString(this sio.Stream str, string txt, int maxBytesLen, ste enc)
        {
            byte[] buf;

            buf = enc.GetBytes(txt);
            if (buf.Length < maxBytesLen)
            {
                str.Write(buf, 0, buf.Length);
                maxBytesLen -= buf.Length;
                str.Write(new byte[maxBytesLen], 0, maxBytesLen);
            }
            else
                str.Write(buf, 0, maxBytesLen);
        }

        public static string GetProgramVersion()
        {
            return GetValue(srl.Assembly.GetExecutingAssembly().FullName.Replace(" ", ""), "version");
        }
    }
