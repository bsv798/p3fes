#define JINN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace text
{
    public class BaseStreams
    {
        public const int tblLen = 0x10000;

        public sio.Stream str;

        protected BaseStreams()
        {
        }

        public void CloseMemoryStream()
        {
            if (str != null)
                str.Close();
        }

        public void SaveMemoryStreamToFile(string fileName)
        {
            long pos = str.Position;
            sio.MemoryStream ms = (sio.MemoryStream)str;
            sio.FileStream fs = new sio.FileStream(fileName, sio.FileMode.Create, sio.FileAccess.Write);

            fs.Write(ms.GetBuffer(), 0, (int)ms.Position);
            fs.Close();
        }

        //private static Streams OpenStream<T>() where T : sio.Stream, new()
        //{
        //    Streams str = new Streams();

        //    str.str = new T();

        //    return str;
        //}

        private static BaseStreams OpenFileStream(string fileName, sio.FileAccess access, sio.FileMode mode)
        {
            BaseStreams str = new BaseStreams();

            str.str = new sio.FileStream(fileName, mode, access);

            return str;
        }

        public static BaseStreams OpenMemoryStream(string fileName)
        {
            return OpenMemoryStream(sio.File.ReadAllBytes(fileName));
        }

        public static BaseStreams OpenMemoryStream(byte[] buffer)
        {
            BaseStreams str = OpenMemoryStream();

            str.str.Write(buffer, 0, buffer.Length);
            str.str.Position = 0;

            return str;
        }

        public static BaseStreams OpenMemoryStream()
        {
            BaseStreams str = new BaseStreams();

            str.str = new sio.MemoryStream();

            return str;
        }

        public static BaseStreams OpenFileStreamForReading(string fileName)
        {
            return OpenFileStream(fileName, sio.FileAccess.Read, sio.FileMode.Open);
        }

        public static BaseStreams OpenFileStreamForWriting(string fileName)
        {
            return OpenFileStream(fileName, sio.FileAccess.Write, sio.FileMode.Open);
        }

        public static BaseStreams OpenNewFileStreamForWriting(string fileName)
        {
            return OpenFileStream(fileName, sio.FileAccess.Write, sio.FileMode.Create);
        }
    }

    public class StreamReaders : BaseStreams
    {
        public sio.BinaryReader binRead;
        public sio.StreamReader strRead;
        public static char[] tblExp = new char[tblLen];

        protected StreamReaders()
        {

        }

        public new void CloseMemoryStream()
        {
            if (binRead != null)
            {
                binRead.Close();
                base.CloseMemoryStream();
            }
        }

        public void CloseTextStream()
        {
            if (strRead != null)
            {
                strRead.Close();
                base.CloseMemoryStream();
            }
        }

        public static StreamReaders OpenFileStream(string fileName)
        {
            return OpenFileStream(fileName, ste.Default);
        }

        public static StreamReaders OpenFileStream(string fileName, ste encoding)
        {
            StreamReaders sr = new StreamReaders();

            sr.str = BaseStreams.OpenFileStreamForReading(fileName).str;
            sr.binRead = new sio.BinaryReader(sr.str, encoding);

            return sr;
        }

        public static new StreamReaders OpenMemoryStream(string fileName)
        {
            return OpenMemoryStream(fileName, ste.Default);
        }

        public static StreamReaders OpenMemoryStream(string fileName, ste encoding)
        {
            StreamReaders sr = new StreamReaders();

            sr.str = BaseStreams.OpenMemoryStream(fileName).str;
            sr.binRead = new sio.BinaryReader(sr.str, encoding);

            return sr;
        }

        public static StreamReaders OpenTextStream(string fileName)
        {
            return OpenTextStream(fileName, ste.Default);
        }

        public static StreamReaders OpenTextStream(string fileName, ste encoding)
        {
            StreamReaders sr = new StreamReaders();

            sr.strRead = new sio.StreamReader(fileName, encoding);

            return sr;
        }

#if JINN
        public string ReadStringUsingTable(byte[] bb, int pos, int len)
        {
            byte[] str = new byte[len * 2];
            int strLen = 0;
            int idx;
            int i;

            len += pos;
            for (i = pos; i < len; i++)
            {
                idx = bb[i];
                if (idx > 0x7f)
                    idx = JinnConvertChar((idx << 0x8) | bb[++i]);
                str[strLen++] = (byte)(idx & 0xff);
                str[strLen++] = (byte)((idx >> 0x8) & 0xff);
            }

            return Streams.uncEnc.GetString(str, 0, strLen);
        }

        private int JinnConvertChar(int chr)
        {
            chr -= 0x8080;
            chr = ((chr >> 8) << 7) + (chr & 0x7F);
            if (chr >= 0x1FA) chr = 0x25A1;
            else //Bad char
                if (chr >= 0x154) chr = 0xFFFF;
                else //Bad char
                    if (chr >= 0x153) chr = 0x3013;
                    else //Geta mark
                        if (chr >= 0x152) chr = 0x2193;
                        else //Downwards arrow
                            if (chr >= 0x151) chr = 0x2191;
                            else //Upwards arrow
                                if (chr >= 0x150) chr = 0x2190;
                                else //Leftwards arrow
                                    if (chr >= 0x14F) chr = 0x2192;
                                    else //Rightwards arrow
                                        if (chr >= 0x14E) chr = 0x00AE;
                                        else //Registered sign
                                            if (chr >= 0x14D) chr = 0x203B;
                                            else //Reference mark
                                                if (chr >= 0x14C) chr = 0x25BC;
                                                else //Black down triangle
                                                    if (chr >= 0x14B) chr = 0x25BD;
                                                    else //White down triangle
                                                        if (chr >= 0x14A) chr = 0x25B2;
                                                        else //Black up triangle
                                                            if (chr >= 0x149) chr = 0x25B3;
                                                            else //White up triangle
                                                                if (chr >= 0x148) chr = 0x25A0;
                                                                else //Black square
                                                                    if (chr >= 0x147) chr = 0x25A1;
                                                                    else //White square
                                                                        if (chr >= 0x146) chr = 0x25C6;
                                                                        else //Black diamond
                                                                            if (chr >= 0x145) chr = 0x25C7;
                                                                            else //White diamond
                                                                                if (chr >= 0x144) chr = 0x25CE;
                                                                                else //Bullseye
                                                                                    if (chr >= 0x143) chr = 0x25CF;
                                                                                    else //Black circle
                                                                                        if (chr >= 0x142) chr = 0x25CB;
                                                                                        else //White circle
                                                                                            if (chr >= 0x141) chr = 0x2605;
                                                                                            else //Black star
                                                                                                if (chr >= 0x140) chr = 0x2606;
                                                                                                else //White star
                                                                                                    if (chr >= 0x13F) chr = 0x00A7;
                                                                                                    else //Section sign
                                                                                                        if (chr >= 0x13E) chr = 0xFFE1;
                                                                                                        else //Fullwidth Pound sign
                                                                                                            if (chr >= 0x13D) chr = 0xFFE0;
                                                                                                            else //Fullwidth Cent sign
                                                                                                                if (chr >= 0x13C) chr = 0xFFE5;
                                                                                                                else //Fullwidth Yen sign
                                                                                                                    if (chr >= 0x13B) chr = 0x2103;
                                                                                                                    else //Degree celsius
                                                                                                                        if (chr >= 0x139) chr += 0x1EF9;
                                                                                                                        else //Primes
                                                                                                                            if (chr >= 0x138) chr = 0x00B0;
                                                                                                                            else //Degree sign
                                                                                                                                if (chr >= 0x137) chr = 0x2640;
                                                                                                                                else //Female sign
                                                                                                                                    if (chr >= 0x136) chr = 0x2642;
                                                                                                                                    else //Male sign
                                                                                                                                        if (chr >= 0x135) chr = 0x2234;
                                                                                                                                        else //Therefore
                                                                                                                                            if (chr >= 0x134) chr = 0x221E;
                                                                                                                                            else //Infinity
                                                                                                                                                if (chr >= 0x133) chr = 0x2267;
                                                                                                                                                else //Greater-than over equal to
                                                                                                                                                    if (chr >= 0x132) chr = 0x2266;
                                                                                                                                                    else //Less-than over equal to
                                                                                                                                                        if (chr >= 0x131) chr = 0x2260;
                                                                                                                                                        else //Not equal to
                                                                                                                                                            if (chr >= 0x130) chr = 0x00F7;
                                                                                                                                                            else //Division sign
                                                                                                                                                                if (chr >= 0x12F) chr = 0xFFFF;
                                                                                                                                                                else //Empty
                                                                                                                                                                    if (chr >= 0x12E) chr = 0x00D7;
                                                                                                                                                                    else //Multiplication sign
                                                                                                                                                                        if (chr >= 0x12D) chr = 0x00B1;
                                                                                                                                                                        else //Plus-minus sign
                                                                                                                                                                            if (chr >= 0x12C) chr = 0x2212;
                                                                                                                                                                            else //Fullwidth hyphen-minus
                                                                                                                                                                                if (chr >= 0x122) chr += 0x2EE6;
                                                                                                                                                                                else //Japanese brackets
                                                                                                                                                                                    if (chr >= 0x120) chr += 0x2EF4;
                                                                                                                                                                                    else //Shell brackets
                                                                                                                                                                                        if (chr >= 0x11F) chr = 0x201C;
                                                                                                                                                                                        else //Left double quotation mark
                                                                                                                                                                                            if (chr >= 0x11E) chr = 0x2025;
                                                                                                                                                                                            else //Two dot leader
                                                                                                                                                                                                if (chr >= 0x11D) chr = 0x2026;
                                                                                                                                                                                                else //Horisontal ellipsis
                                                                                                                                                                                                    if (chr >= 0x11C) chr = 0x2225;
                                                                                                                                                                                                    else //Parallel to
                                                                                                                                                                                                        if (chr >= 0x11B) chr = 0xFF5E;
                                                                                                                                                                                                        else //Fullwidth tilde
                                                                                                                                                                                                            if (chr >= 0x11A) chr = 0x2010;
                                                                                                                                                                                                            else //Hyphen
                                                                                                                                                                                                                if (chr >= 0x119) chr = 0x30FC;
                                                                                                                                                                                                                else //Prolonged sound mark
                                                                                                                                                                                                                    if (chr >= 0x116) chr += 0x2EEF;
                                                                                                                                                                                                                    else //Ideographic iteration,
                                                                                                                                                                                                                        //closing mark and number zero
                                                                                                                                                                                                                        if (chr >= 0x115) chr = 0x4EDD;
                                                                                                                                                                                                                        else //CJK unified ideograph
                                                                                                                                                                                                                            if (chr >= 0x114) chr = 0x3003;
                                                                                                                                                                                                                            else //Ditto mark
                                                                                                                                                                                                                                if (chr >= 0x112) chr += 0x2F8B;
                                                                                                                                                                                                                                else //Hiragana iteration marks
                                                                                                                                                                                                                                    if (chr >= 0x110) chr += 0x2FED;
                                                                                                                                                                                                                                    else //Katakana iteration marks
                                                                                                                                                                                                                                        if (chr >= 0x10F) chr = 0x00A8;
                                                                                                                                                                                                                                        else //Diaeresis
                                                                                                                                                                                                                                            if (chr >= 0x10E) chr = 0xFF40;
                                                                                                                                                                                                                                            else //Fullwidth grave accent
                                                                                                                                                                                                                                                if (chr >= 0x10D) chr = 0x00B4;
                                                                                                                                                                                                                                                else //Accute accent
                                                                                                                                                                                                                                                    if (chr >= 0x10B) chr += 0x2F90;
                                                                                                                                                                                                                                                    else //Voiced sound marks
                                                                                                                                                                                                                                                        if (chr >= 0x10A) chr = 0x30FB;
                                                                                                                                                                                                                                                        else //Katakana middle dot
                                                                                                                                                                                                                                                            if (chr >= 0x108) chr += 0x2EF9;
                                                                                                                                                                                                                                                            else //Idiographic comma and
                                                                                                                                                                                                                                                                //full stop
                                                                                                                                                                                                                                                                if (chr >= 0xB2) chr += 0x2FEF;
                                                                                                                                                                                                                                                                else //Katakana
                                                                                                                                                                                                                                                                    if (chr >= 0x5F) chr += 0x2FE2;
                                                                                                                                                                                                                                                                    else //Hiragana
                                                                                                                                                                                                                                                                        if (chr >= 0x01) chr += 0x20;
                                                                                                                                                                                                                                                                        else
                                                                                                                                                                                                                                                                            if (chr == 0x00) chr = 0x3000; //Fullwidth space

            return chr;
        }

#else
        public string ReadStringUsingTable(byte[] bb, int pos, int len)
        {
            byte[] str = new byte[len * 2];
            int strLen = 0;
            int idx;
            int i;

            len += pos;
            for (i = pos; i < len; i++)
            {
                idx = bb[i];
                if (idx > 0x7f)
                    idx = (idx << 0x8) | bb[++i];
                if (tblExp[idx] > 0)
                {
                    str[strLen++] = (byte)(tblExp[idx] & 0xff);
                    str[strLen++] = (byte)((tblExp[idx] >> 0x8) & 0xff);
                }
                else
                {
                    str[strLen++] = bb[i];
                    str[strLen++] = bb[i - 1];
                }
            }

            return Streams.uncEnc.GetString(str, 0, strLen);
        }
#endif
    }

    public class StreamWriters : BaseStreams
    {
        public sio.BinaryWriter binWrite;
        public sio.StreamWriter strWrite;
        public static ushort[] tblImp = new ushort[tblLen];

        protected StreamWriters()
        {

        }

        public new void CloseMemoryStream()
        {
            if (binWrite != null)
            {
                binWrite.Close();
                base.CloseMemoryStream();
            }
        }

        public void CloseTextStream()
        {
            if (strWrite != null)
            {
                strWrite.Close();
                base.CloseMemoryStream();
            }
        }

        public static StreamWriters OpenFileStream(string fileName)
        {
            return OpenFileStream(fileName, ste.Default);
        }

        public static StreamWriters OpenFileStream(string fileName, ste encoding)
        {
            StreamWriters sr = new StreamWriters();

            sr.str = BaseStreams.OpenFileStreamForWriting(fileName).str;
            sr.binWrite = new sio.BinaryWriter(sr.str, encoding);

            return sr;
        }

        public static StreamWriters OpenNewFileStream(string fileName)
        {
            return OpenNewFileStream(fileName, ste.Default);
        }

        public static StreamWriters OpenNewFileStream(string fileName, ste encoding)
        {
            StreamWriters sr = new StreamWriters();

            sr.str = BaseStreams.OpenNewFileStreamForWriting(fileName).str;
            sr.binWrite = new sio.BinaryWriter(sr.str, encoding);

            return sr;
        }

        public new static StreamWriters OpenMemoryStream()
        {
            return OpenMemoryStream(ste.Default);
        }

        public static StreamWriters OpenMemoryStream(ste encoding)
        {
            StreamWriters sr = new StreamWriters();

            sr.str = BaseStreams.OpenMemoryStream().str;
            sr.binWrite = new sio.BinaryWriter(sr.str, encoding);

            return sr;
        }

        public static new StreamWriters OpenMemoryStream(string fileName)
        {
            return OpenMemoryStream(fileName, ste.Default);
        }

        public static StreamWriters OpenMemoryStream(string fileName, ste encoding)
        {
            StreamWriters sr = new StreamWriters();

            sr.str = BaseStreams.OpenMemoryStream(fileName).str;
            sr.binWrite = new sio.BinaryWriter(sr.str, encoding);

            return sr;
        }

        public static StreamWriters OpenTextStream(string fileName)
        {
            return OpenTextStream(fileName, ste.Default);
        }

        public static StreamWriters OpenTextStream(string fileName, ste encoding)
        {
            StreamWriters sr = new StreamWriters();

            sr.strWrite = new sio.StreamWriter(fileName, false, encoding);

            return sr;
        }

        public byte[] WriteStringUsingTable(string str)
        {
            byte[] buf = new byte[str.Length * 2];
            int bufLen = 0;
            int idx;

            for (int i = 0; i < str.Length; i++)
            {
                idx = tblImp[(ushort)str[i]];
                if (idx == 0)
                    idx = (ushort)str[i];
                if (idx < 0x100)
                    buf[bufLen++] = (byte)idx;
                else
                {
                    buf[bufLen++] = (byte)((idx >> 0x8) & 0xff);
                    buf[bufLen++] = (byte)idx;
                }
            }
            Array.Resize<byte>(ref buf, bufLen);

            return buf;
        }
    }

    public class Streams
    {
        public static int offset;
        public static StreamReaders reader;
        public static StreamWriters writer;
        public static ste japEnc = ste.GetEncoding(932);
        public static ste uncEnc = ste.GetEncoding(1200);
    }
}
