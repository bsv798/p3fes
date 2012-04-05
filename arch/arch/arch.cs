using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;
using stre = System.Text.RegularExpressions;

namespace arch
{
    public class Archiever
    {
        public string archPath;
        private sio.StreamWriter sw;
        private sio.StreamReader sr;
        private int gFileCount;

        private string parentDir;
        private int gIterCount;
        private sio.MemoryStream[] itMs;

        public Archiever(string archPath)
        {
            if (!sio.Directory.Exists(archPath))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", archPath));
            this.archPath = misc.AddEndSep(archPath);
        }

        public void Unpack()
        {
            AnalyzeDir(archPath);
        }

        private void AnalyzeDir(string dirName)
        {
            sio.DirectoryInfo[] dis;
            string[] files;
            sio.MemoryStream ms;

            dis = new sio.DirectoryInfo(dirName).GetDirectories();
            foreach (var di in dis)
            {
                AnalyzeDir(di.FullName);
            }

            files = "*.bin;*.cpk;*.pac;*.pak;*.fpc;*.se;*_DAT;*.spr;*.pm1;*.rmd;*.epl;*.bed;*.cin".Split(';').SelectMany(filter => sio.Directory.GetFiles(dirName, filter)).ToArray();
            //"*.bin;*.cpk;*.pac;*.pak;*.fpc;*.se;*_DAT;*.spr;*.pm1;*.rmd;*.epl;*.bed;*.cin"
            sw = null;
            gFileCount = 0;
            gIterCount = 0;
            dirName = misc.AddEndSep(dirName);
            Console.WriteLine("Extracting '{0}'", dirName);
            foreach (var file in files)
            {
                ms = new sio.MemoryStream(sio.File.ReadAllBytes(file));
                GetArchs(dirName, misc.GetFileName(file), ms, ms.Length);
                ms.Close();
                //sio.File.Delete(file);
            }
            if (sw != null)
                sw.Close();
        }

        private void GetArchs(string extrPath, string name, sio.MemoryStream ms, long len)
        {
            sio.BinaryReader br;
            int archType;
            long prevPos;
            long lastPos;
            int fileCount;

            gIterCount++;

            br = new sio.BinaryReader(ms);

            prevPos = ms.Position;
            lastPos = prevPos + len;
            archType = GetArchType(br, len, out fileCount);
            if (archType > 0)
                if (sw == null)
                    sw = new sio.StreamWriter(string.Format("{0}{1}$arch.type", extrPath, misc.dirSep));
            ms.Position = prevPos;

            switch (archType)
            {
                case 1:
                    sw.WriteLine("{{type={0},name={1},count={2}}}", archType, name, fileCount);
                    ExtractUnNamedBinary(extrPath, br, lastPos);
                    sw.WriteLine("{end}");
                    break;
                case 2:
                    sw.WriteLine("{{type={0},name={1}}}", archType, name);
                    ExtractNamedBinary(extrPath, br, len);
                    sw.WriteLine("{end}");
                    break;
                case 3:
                    sw.WriteLine("{{type={0},name={1},count={2}}}", archType, name, fileCount);
                    ExtractSpr0(extrPath, br, lastPos);
                    sw.WriteLine("{end}");
                    break;
                case 4:
                    sw.WriteLine("{{type={0},name={1},count={2}}}", archType, name, fileCount);
                    ExtractPmd1(extrPath, br, len);
                    sw.WriteLine("{end}");
                    break;
                case 5:
                    sw.WriteLine("{{type={0},name={1},count={2}}}", archType, name, fileCount);
                    ExtractTxp0(extrPath, br, len);
                    sw.WriteLine("{end}");
                    break;
                case 6:
                    sw.WriteLine("{{type={0},name={1}}}", archType, name);
                    ExtractPib0(extrPath, br, len);
                    sw.WriteLine("{end}");
                    break;
                case 7:
                    sw.WriteLine("{{type={0},name={1}}}", archType, name);
                    ExtractTmx0Ps2(extrPath, br, len);
                    sw.WriteLine("{end}");

                    ms.Position = prevPos;
                    if (gIterCount == 1)
                        ms.Position += len;
                    else
                        ms.Extract(extrPath + name, len);
                    break;
                case 8:
                    sw.WriteLine("{{type={0},name={1}}}", archType, name);
                    ExtractCin(extrPath, br, len);
                    sw.WriteLine("{end}");
                    break;
                default:
                    if (gIterCount == 1)
                        ms.Position += len;
                    else
                    {
                        sw.WriteLine(name);
                        ms.Extract(extrPath + name, len);
                    }
                    break;
            }

            gIterCount--;
        }

        private int GetArchType(sio.BinaryReader br, long len, out int fileCount)
        {
            sio.MemoryStream ms;
            long prevPos;
            long lastPos;
            byte b;
            byte[] bb;
            int size;
            int i;
            bool tru;

            bb = new byte[0xfc];

            ms = (sio.MemoryStream)br.BaseStream;
            fileCount = 0;
            prevPos = ms.Position;
            lastPos = prevPos + len;
            if (lastPos < 0x4)
                return 0;

            if (br.ReadInt32() == 0x64)
            {
                fileCount = br.ReadInt32();
                size = (fileCount + 1) << 3;
                if (fileCount > 0)
                    if (br.ReadInt32() == size)
                    {
                        ms.Position += size - 0xc - 0x8;
                        size = br.ReadInt32() + br.ReadInt32();
                        if (size <= lastPos)
                            return 1;
                    }
            }

            fileCount = 0;
            ms.Position = prevPos;
            if (lastPos > 0xff)
            {
                while (ms.Position < lastPos)
                {
                    for (i = 0; i < bb.Length; i++)
                    {
                        b = br.ReadByte();
                        if (b == 0)
                            break;
                        else if ((b < 0x20) || (b > 0x7f))
                        {
                            i = 0;
                            break;
                        }
                        bb[i] = b;
                    }
                    if (i > 0)
                    {
                        if (!stre.Regex.IsMatch(ste.ASCII.GetString(bb, 0, i), "^(\\w*[/.]*){1,}.\\w+$"))
                            break;
                    }
                    else if (fileCount == 0)
                        break;
                    ms.Position += bb.Length - i - 1;
                    size = br.ReadInt32();
                    if (size < 0 || size > lastPos)
                        break;
                    ms.Position += size;
                    ms.AlignPos(0x40);
                    if (size == 0)
                        if (fileCount == 0)
                            break;
                        else
                            if (lastPos - ms.Position < 0x800)
                                return 2;
                    fileCount++;
                }
                if (fileCount > 0)
                    if (ms.Position == lastPos)
                        return 2;
            }

            fileCount = 0;
            ms.Position = prevPos;
            if (br.ReadInt32() == 1)
                if (br.ReadInt32() == 0)
                    if (br.ReadInt32() == 0x30525053)
                        if (br.ReadInt32() == 0x20)
                            if (br.ReadInt32() - len - 0x4 < 0x40)
                            {
                                fileCount = br.ReadInt16();
                                return 3;
                            }
#if DEBUG
                            else
                                throw new ArgumentException("spr0 size mismatch!");
                        else
                            throw new ArgumentException("spr0 header size mismatch!");
#endif

            fileCount = 0;
            ms.Position = prevPos;
            if (br.ReadInt32() == 0)
                if (br.ReadInt32() - len - 0x4 < 0x40)
                    if (br.ReadInt32() == 0x31444D50) //pmd1
                        if (br.ReadInt32() == 0)
                        {
                            fileCount = br.ReadInt32();
                            if (br.ReadInt32() == 0x3)
                                return 4;
#if DEBUG
                            else
                                throw new ArgumentException("pmd1 flag mismatch!");
#endif
                        }


            fileCount = 0;
            ms.Position = prevPos;
            if (br.ReadInt32() == 0x9)
                if (br.ReadInt32() - len - 0x4 < 0x40)
                    if (br.ReadInt32() == 0x30505854) //txp0
                        if (br.ReadInt32() == 0)
                        {
                            fileCount = br.ReadInt32();
                            return 5;
                        }

            fileCount = 0;
            ms.Position = prevPos;
            if (br.ReadInt32() == 1)
            {
                br.ReadInt32();
                if (br.ReadInt32() == 0x30424950) //pib0
                    if (br.ReadInt32() == 0)
                        return 6;
            }

            if (len > 0x80)
            {
                fileCount = 0; //epl
                ms.Position = prevPos + 0x44;
                tru = (br.ReadInt32() & 0x40a00000) == 0x40a00000;
                ms.Position += 0x4;
                tru &= br.ReadInt32() == 0x3f800000;
                ms.Position += 0xc;
                tru &= br.ReadInt32() == 0x3f800000;
                ms.Position += 0x28;
                tru &= br.ReadInt32() == 0x90;
                if (tru)
                    return 7;
            }

            fileCount = 0; //rmd
            ms.Position = prevPos;
            if (br.ReadUInt32() == 0xf0f000f0)
            {
                if (br.ReadInt32() == 0x2)
                    if (br.ReadInt32() == 0x40000000)
                        return 7;
            }
            else
            {
                ms.Position = prevPos;
                tru = br.ReadInt32() == 0x16;
                ms.Position += 0x8;
                tru &= br.ReadInt32() == 1;
                i = br.ReadInt32();
                if ((i > -1) && ((i + ms.Position + 0x4) < lastPos))
                {
                    ms.Position += 0x4 + i;
                    if (ms.Position < lastPos)
                        tru &= br.ReadInt32() == 0x15;
                    else
                        tru = false;
                    if (tru)
                        return 7;
                }
            }

            fileCount = 0;
            ms.Position = prevPos;
            if (br.ReadInt32() == 0x34444542) //bed4
                if (br.ReadInt32() == 0x3030)
                {
                    br.ReadInt32();
                    if (br.ReadInt32() == 0)
                        return 7;
                }

            fileCount = 0;
            ms.Position = prevPos;
            if (br.ReadInt32() == 0x004E4943)
                if (br.ReadInt32() == 1)
                    return 8;

            return 0;
        }

        private void ExtractNamedBinary(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            long lastPos;
            string name;
            byte b;
            byte[] bb;
            int size;
            int i;

            bb = new byte[0xfc];
            ms = (sio.MemoryStream)br.BaseStream;
            lastPos = ms.Position + len;
            while (ms.Position < lastPos)
            {
                for (i = 0; i < bb.Length; i++)
                {
                    b = br.ReadByte();
                    if (b == 0)
                        break;
                    else if ((b < 0x20) || (b > 0x7f))
                    {
                        i = 0;
                        break;
                    }
                    bb[i] = b;
                }
                ms.Position += bb.Length - i - 1;
                size = br.ReadInt32();
                if ((size == 0) || (i == 0))
                    if (lastPos - ms.Position < 0x800)
                        break;
                name = misc.InsertCounter(ste.ASCII.GetString(bb, 0, i), gFileCount++);
                GetArchs(extrPath, name, ms, size);
                //sw.WriteLine(name);
                ms.AlignPos(0x40);
            }
        }

        private void ExtractUnNamedBinary(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            long pos;
            int size;
            long prevPos;
            long startPos;
            int count;
            string name;
            bool found;
            int i;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;
            name = null;
            found = false;

            if (br.ReadInt32() == 0x64)
            {
                count = br.ReadInt32();
                prevPos = ms.Position;
                for (i = 0; i < count; i++)
                {
                    ms.Position = prevPos;
                    pos = br.ReadInt32();
                    size = br.ReadInt32();

                    ms.Position = startPos + pos;
                    if (br.ReadInt32() == 0x2)
                    {
                        br.ReadInt32();
                        if (br.ReadInt32() == 0x30584D54)
                            if (br.ReadInt32() == 0)
                            {
                                name = misc.InsertCounter("tmx", gFileCount++);
                                found = true;
                            }
                    }
                    if (!found)
                    {
                        ms.Position = startPos + pos;
                        if (br.ReadInt32() == 0x7)
                        {
                            br.ReadInt32();
                            if (br.ReadInt32() == 0x3147534D)
                                if (br.ReadInt32() == 0)
                                {
                                    name = misc.InsertCounter("msg", gFileCount++);
                                    found = true;
                                }
                        }
                    }

                    ms.Position = startPos + pos;
                    if (found)
                    {
                        sw.WriteLine(name);
                        ms.Extract(extrPath + name, size);
                        found = false;
                    }
                    else
                    {
                        name = misc.InsertCounter("dat", gFileCount++);
                        GetArchs(extrPath, name, ms, size);
                    }
                    //sw.WriteLine(name);
                    prevPos += 0x8;
                }
            }
        }

        private void ExtractSpr0(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            long pos;
            int size;
            long prevPos;
            long startPos;
            int count;
            string name;
            int i;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;

            ms.Position += 0x14;
            count = br.ReadInt16();
            ms.Position += 0xa;
            prevPos = ms.Position;

            if (count == 0)
            {
                ms.Position = prevPos - 0x10;
                size = br.ReadInt32() - (int)(prevPos - startPos);
                ms.Position = prevPos;
            }
            else
            {
                br.ReadInt32();
                size = br.ReadInt32();
                ms.Position += (count - 1) << 0x3;
                size -= (int)(ms.Position - startPos);
            }
            name = misc.InsertCounter("dat", gFileCount++);
            GetArchs(extrPath, name, ms, size);

            for (i = 0; i < count; i++)
            {
                ms.Position = prevPos;
                br.ReadInt32();
                pos = br.ReadInt32();
                ms.Position = startPos + pos + 0x4;
                size = br.ReadInt32();
                ms.Position -= 0x8;
                name = misc.InsertCounter("tmx", gFileCount++);
                GetArchs(extrPath, name, ms, size);
                //sw.WriteLine(name);
                prevPos += 0x8;
            }
        }

        private void ExtractPmd1(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            long startPos;
            long prevPos, subPrevPos;
            int cnt;
            int cmd, blkSize, blkCnt, offset;
            int iNmIdx, iCode, iOffset, iSize, iEplSize;
            string[] names;
            string name;
            int nameIdx;
            int i, j;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;
            ms.Position += 0x10;

            cnt = br.ReadInt32();
            ms.Position += 0xc;

            names = null;
            nameIdx = 0;
            prevPos = ms.Position;
            for (i = 0; i < cnt; i++)
            {
                ms.Position = prevPos;
                cmd = br.ReadInt32();
                blkSize = br.ReadInt32();
                blkCnt = br.ReadInt32();
                offset = br.ReadInt32();
                ms.Position = startPos + offset;
                sw.WriteLine(string.Format("{0:x2},{1:x8},{2:x8}", cmd, blkSize, blkCnt));
                if ((blkSize > 0) && (blkCnt > 0))
                    switch (cmd)
                    {
                        case 0x01:
                            names = new string[blkCnt];
                            for (j = 0; j < blkCnt; j++)
                            {
                                names[j] = ms.ReadNTString(blkSize);
                                ms.Position += blkSize - names[j].Length - 1;
                                sw.WriteLine(names[j]);
                            }
                            break;
                        case 0x02:
                            name = misc.InsertCounter(names[nameIdx++], gFileCount++);
                            GetArchs(extrPath, name, ms, blkSize);
                            break;
                        case 0x03:
                            subPrevPos = ms.Position;
                            for (j = 0; j < blkCnt; j++)
                            {
                                ms.Position = subPrevPos;
                                iNmIdx = br.ReadInt32();
                                ms.Position += 0xc;
                                iOffset = br.ReadInt32();
                                iSize = br.ReadInt32();
                                iCode = br.ReadInt32();
                                sw.WriteLine(string.Format("{0:x8},{1:x8}", iNmIdx, iCode));
                                name = misc.InsertCounter(names[iNmIdx], gFileCount++);
                                ms.Position = startPos + iOffset;
                                GetArchs(extrPath, name, ms, iSize);
                                subPrevPos += blkSize;
                                nameIdx++;
                            }
                            break;
                        case 0x06:
                            name = misc.InsertCounter(names[nameIdx++], gFileCount++);
                            GetArchs(extrPath, name, ms, blkSize);
                            break;
                        case 0x07:
                            subPrevPos = ms.Position;
                            ms.Position = prevPos + 0x2c;
                            iEplSize = br.ReadInt32();
                            for (j = 0; j < blkCnt; j++)
                            {
                                ms.Position = subPrevPos;
                                iNmIdx = br.ReadInt32();
                                iOffset = br.ReadInt32();
                                if (j == blkCnt - 1)
                                {
                                    iSize = iEplSize - iOffset;
                                }
                                else
                                {
                                    ms.Position += 0xc;
                                    iSize = br.ReadInt32() - iOffset;
                                }
                                ms.Position = startPos + iOffset;
                                sw.WriteLine(string.Format("{0:x8}", iNmIdx));
                                name = misc.InsertCounter(names[iNmIdx], gFileCount++);
                                GetArchs(extrPath, name, ms, iSize);
                                subPrevPos += blkSize;
                                nameIdx++;
                            }
                            break;
                        case 0x08: //07 check
                            break;
                        case 0x09: //03 check
                            break;
                        case 0x0a:
                            name = misc.InsertCounter("fl1", gFileCount++);
                            GetArchs(extrPath, name, ms, blkSize);
                            break;
                        case 0x0b:
                            name = misc.InsertCounter("fl2", gFileCount++);
                            GetArchs(extrPath, name, ms, blkSize);
                            break;
                        case 0x0c:
                            name = misc.InsertCounter("txp", gFileCount++);
                            GetArchs(extrPath, name, ms, blkSize);
                            break;
                        case 0x16:
                            subPrevPos = ms.Position;
                            blkSize /= blkCnt;
                            for (j = 0; j < blkCnt; j++)
                            {
                                ms.Position = subPrevPos;
                                iNmIdx = br.ReadInt32();
                                iOffset = br.ReadInt32();
                                ms.Position = startPos + iOffset + 0x4;
                                iSize = br.ReadInt32();
                                ms.Position -= 0x8;
                                sw.WriteLine(string.Format("{0:x8}", iNmIdx));
                                name = misc.InsertCounter(names[iNmIdx], gFileCount++);
                                GetArchs(extrPath, name, ms, iSize);
                                subPrevPos += blkSize;
                                nameIdx++;
                            }
                            break;
                        case 0x17: //16 check
                            break;
                        case 0x1b:
                            name = misc.InsertCounter(names[nameIdx++], gFileCount++);
                            GetArchs(extrPath, name, ms, blkSize);
                            break;
                        default:
                            throw new ArgumentException(string.Format("Unknown pmd1 cmd: {0}, path: '{1}'", cmd, extrPath));
                    }
                prevPos += 0x10;
            }
        }

        private void ExtractTxp0(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            int size;
            long startPos;
            long pos;
            int count;
            string name;
            int i;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;
            pos = 0;

            ms.Position += 0x10;
            count = br.ReadInt32();
            pos += 0x14;
            ms.Position += count << 0x2;
            pos += count << 0x2;
            pos = misc.Align(pos, 0x40);
            ms.Position = startPos + pos;

            for (i = 0; i < count; i++)
            {
                ms.Position += 0x4;
                size = br.ReadInt32();
                if (br.ReadInt32() != 0x30584D54) //tmx0
                    throw new ArgumentException("txp0 contains wrong tmx0");
                ms.Position -= 0xc;
                name = misc.InsertCounter("tmx", gFileCount++);
                GetArchs(extrPath, name, ms, size);
                pos += size;
                pos = misc.Align(pos, 0x40);
                ms.Position = startPos + pos;
                //sw.WriteLine(name);
            }
        }

        private void ExtractPib0(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            int cmd;
            int size;
            string name;
            long startPos;
            long datPos;
            long pos;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;

            br.ReadInt32();
            size = br.ReadInt32() - 0x10;
            ms.Position += 0x8;
            GetArchs(extrPath, misc.InsertCounter("dat", gFileCount++), ms, size);
            pos = size + 0x10;
            pos = misc.Align(pos, 0x40);
            ms.Position = startPos + pos;

            name = null;
            while (true)
            {
                cmd = br.ReadInt32();
                size = br.ReadInt32();
                switch (cmd)
                {
                    case 0x01:
                        if (br.ReadInt32() != 0x00503344) //d3p
                            throw new ArgumentException("pib0 command and type mismatch");
                        name = misc.InsertCounter("d3p", gFileCount++);
                        break;
                    case 0x06:
                        if (br.ReadInt32() != 0x3030444D) //md00
                            throw new ArgumentException("pib0 command and type mismatch");
                        name = misc.InsertCounter("md0", gFileCount++);
                        break;
                    case 0x08:
                        if (br.ReadInt32() != 0x3030544D) //mt00
                            throw new ArgumentException("pib0 command and type mismatch");
                        name = misc.InsertCounter("mt0", gFileCount++);
                        break;
                    case 0x09:
                        if (br.ReadInt32() != 0x30505854) //txp0
                            throw new ArgumentException("pib0 command and type mismatch");
                        name = misc.InsertCounter("txp", gFileCount++);
                        break;
                    case 0xff:
                        if (br.ReadInt32() != 0x30444E45) //end0
                            throw new ArgumentException("pib0 command and type mismatch");
                        break;
                    default:
                        throw new ArgumentException("pib0 unknown command");
                }
                if (cmd == 0xff)
                    break;

                ms.Position -= 0xc;
                datPos = ms.Position;
                GetArchs(extrPath, name, ms, size);
                pos += (int)(ms.Position - datPos);
                pos = misc.Align(pos, 0x40);
                ms.Position = startPos + pos;
                //sw.WriteLine(name);
            }
        }

        private void ExtractTmx0Ps2(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            long startPos;
            long lastPos;
            long curPos;
            byte b;
            int size;
            bool found;
            string name;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;
            lastPos = startPos + len;
            curPos = ms.Position;
            size = 0;
            name = null;
            found = false;

            while (curPos < lastPos)
            {
                ms.Position = curPos;
                b = br.ReadByte();
                if (b == 0x2)
                {
                    if (br.ReadByte() == 0)
                        if (br.ReadInt16() == 0)
                        {
                            size = br.ReadInt32();
                            if (br.ReadInt32() == 0x30584D54) //tmx0
                                if (br.ReadInt32() == 0)
                                {
                                    name = misc.InsertCounter("tmx", gFileCount++);
                                    found = true;
                                }
                        }
                }
                else if (b == 0x15)
                {
                    if (br.ReadByte() == 0)
                        if (br.ReadInt16() == 0)
                        {
                            size = br.ReadInt32() + 0xc;
                            br.ReadInt32();
                            if (br.ReadInt32() == 1)
                                if (br.ReadInt32() == 0x8)
                                {
                                    ms.Position += 0x4;
                                    if (br.ReadInt32() == 0x325350) //ps2
                                    {
                                        name = misc.InsertCounter("ps2", gFileCount++);
                                        found = true;
                                    }
                                }
                        }
                }
                if (found)
                {
                    ms.Position = curPos;
                    sw.WriteLine(string.Format("{0:x8},{1:x8}", curPos - startPos, size));
                    sw.WriteLine(name);
                    ms.Extract(extrPath + name, size);
                    curPos += size - 1;
                    found = false;
                }
                curPos++;
            }
        }

        private void ExtractCin(string extrPath, sio.BinaryReader br, long len)
        {
            sio.MemoryStream ms;
            int count;
            long startPos;
            long pos;
            string name;

            ms = (sio.MemoryStream)br.BaseStream;
            startPos = ms.Position;

            ms.Position += 0x8;
            count = br.ReadInt16();
            ms.Position += 0x10;
            while (count > 0)
            {
                ms.Position += 0x11;
                if (br.ReadByte() == 0xfe)
                    count--;
            }
            ms.Position += 0xd;
            pos = ms.Position - startPos;
            pos = misc.Align(pos, 0x40);

            ms.Position = startPos;
            name = misc.InsertCounter("dat", gFileCount++);
            sw.WriteLine(name);
            ms.Extract(extrPath + name, pos);
            if (pos < (len - 0x100))
                GetArchs(extrPath, misc.InsertCounter("dat", gFileCount++), ms, len - (int)pos);
            else
                ms.Position = startPos + len;
        }

        public void Pack()
        {
            string[] files;
            string prms;
            long length;

            itMs = new sio.MemoryStream[0x8];

            files = sio.Directory.GetFiles(archPath, "$arch.type", sio.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                parentDir = misc.GetParentDir(file);
                Console.WriteLine("Rebuilding '{0}'", parentDir);
                sr = new sio.StreamReader(file);
                gIterCount = 0;
                while (!sr.EndOfStream)
                {
                    prms = sr.ReadLine();
                    AnalyzeDescFile(prms);
                    length = itMs[1].Position;
                    itMs[1].Position = 0;
                    itMs[1].Extract(parentDir + misc.GetValue(prms.Substring(1, prms.Length - 2), "name"), length);
                    itMs[1].Close();
                }
                sr.Close();
            }
        }

        private void AnalyzeDescFile(string prms)
        {
            int type;
            string name;
            int fileCount;

            gIterCount++;

            prms = prms.Substring(1, prms.Length - 2);
            type = Convert.ToInt32(misc.GetValue(prms, "type"));
            name = misc.GetValue(prms, "name");

            switch (type)
            {
                case 1:
                    fileCount = Convert.ToInt32(misc.GetValue(prms, "count"));
                    RebuildUnNamedBinary(name, fileCount);
                    break;
                case 2:
                    RebuildNamedBinary(name);
                    break;
                case 3:
                    fileCount = Convert.ToInt32(misc.GetValue(prms, "count"));
                    RebuildSpr0(name, fileCount);
                    break;
                case 4:
                    fileCount = Convert.ToInt32(misc.GetValue(prms, "count"));
                    RebuildPmd1(name, fileCount);
                    break;
                case 5:
                    fileCount = Convert.ToInt32(misc.GetValue(prms, "count"));
                    RebuildTxp0(name, fileCount);
                    break;
                case 6:
                    RebuildPib0(name);
                    break;
                case 7:
                    ImportTmx0Ps2(name);
                    break;
                case 8:
                    RebuildCin(name);
                    break;
                default:
                    throw new ArgumentException(string.Format("Wrong arch type at '{0}{1}'", parentDir, name));
            }
            gIterCount--;
        }

        private void RebuildNamedBinary(string archName)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            byte[] buf;
            byte[] bfc;

            bfc = new byte[0xfc];

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{type") == 0)
                {
                    AnalyzeDescFile(str);
                    str = misc.GetValue(str.Substring(1, str.Length - 2), "name");
                    buf = new byte[itMs[gIterCount + 1].Position];
                    Array.Copy(itMs[gIterCount + 1].GetBuffer(), 0, buf, 0, (int)itMs[gIterCount + 1].Position);
                    itMs[gIterCount + 1].Close();
                }
                else if (str.IndexOf("{end}") == 0)
                {
                    break;
                }
                else
                {
                    buf = sio.File.ReadAllBytes(parentDir + str);
                }
                str = misc.RemoveCounter(str);
                bw.Write(ste.ASCII.GetBytes(str));
                ms.Write(bfc, 0, bfc.Length - str.Length);
                bw.Write((int)buf.Length);
                ms.Write(buf, 0, buf.Length);
                ms.AlignWrite(0x40);

                str = sr.ReadLine();
            }
            bw.Write(bfc);
            bw.Write((int)0);
        }

        private void RebuildUnNamedBinary(string archName, int fileCount)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            long fileLen, fileStart;
            long tblPos, dataPos;
            byte[] buf;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);

            bw.Write((int)0x64);
            bw.Write((int)fileCount);
            fileStart = (fileCount + 1) << 0x3;
            tblPos = ms.Position;
            dataPos = fileStart;

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{type") == 0)
                {
                    AnalyzeDescFile(str);
                    buf = new byte[itMs[gIterCount + 1].Position];
                    Array.Copy(itMs[gIterCount + 1].GetBuffer(), 0, buf, 0, (int)itMs[gIterCount + 1].Position);
                    itMs[gIterCount + 1].Close();
                }
                else if (str.IndexOf("{end}") == 0)
                {
                    break;
                }
                else
                {
                    buf = sio.File.ReadAllBytes(parentDir + str);
                }

                ms.Position = tblPos;
                bw.Write((int)fileStart);
                fileLen = buf.Length;
                bw.Write((int)fileLen);
                fileStart += misc.Align(fileLen, 0x4);
                tblPos += 0x8;

                ms.Position = dataPos;
                ms.Write(buf, 0, buf.Length);
                ms.AlignWrite(0x4);
                dataPos = ms.Position;

                str = sr.ReadLine();
            }
        }

        private void RebuildSpr0(string archName, int fileCount)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            long tblPos, datPos;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);
            if (fileCount == 0)
                tblPos = 0;

            bw.Write((int)1);
            bw.Write((int)0);
            bw.Write((int)0x30525053);
            bw.Write((int)0x20);
            bw.Write((int)0);
            bw.Write(fileCount);
            bw.Write((int)0x20);
            tblPos = (fileCount << 0x3) + 0x20;
            bw.Write((int)(tblPos));
            ms.Position = tblPos;
            str = sr.ReadLine();
            bw.Write(sio.File.ReadAllBytes(parentDir + str));
            ms.AlignWrite(0x40);
            datPos = ms.Position;
            ms.Position = tblPos + 0x4;
            fileCount = ms.ReadByte() + (ms.ReadByte() << 0x08) + (ms.ReadByte() << 0x10) + (ms.ReadByte() << 0x18);
            fileCount = (fileCount - (int)tblPos) >> 0x3;
            ms.Position = 0x16;
            bw.Write((short)fileCount);
            tblPos = 0x20;
            ms.Position = datPos;

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{end}") == 0)
                {
                    break;
                }
                ms.Position = tblPos;
                bw.Write((int)0);
                bw.Write((int)datPos);
                ms.Position = datPos;
                bw.Write(sio.File.ReadAllBytes(parentDir + str));
                ms.AlignWrite(0x40);
                datPos = ms.Position;
                tblPos += 0x8;

                str = sr.ReadLine();
            }
            datPos = ms.Position;
            ms.Position = 0x10;
            bw.Write((int)(datPos - 0x4));
            ms.Position = datPos;
        }

        private void RebuildPmd1(string archName, int fileCount)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;

            sio.FileInfo fi;
            long tblPos, subTblPos, datPos;
            string[] prms;
            int cmd, prm1, prm2;
            sio.MemoryStream rmdMs;
            sio.BinaryReader rmdBr;
            sio.BinaryWriter rmdBw;
            int rmdDataStart, rmdTblPos, rmdSubTblPos, rmdSize, rmdNameIdx, rmdIdx;
            int eplPos, eplSize;
            int tmxPos, tmxSize;
            int i, j;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);

            bw.Write((int)0);
            bw.Write((int)0);
            bw.Write((int)0x31444D50);
            bw.Write((int)0);
            bw.Write(fileCount);
            bw.Write((int)0x3);
            bw.Write((int)0);
            bw.Write((int)0);
            tblPos = ms.Position;
            datPos = (fileCount << 0x4) + 0x20;

            rmdMs = null;
            rmdBw = null;
            rmdSize = 0;
            rmdTblPos = 0;
            rmdSubTblPos = 0;
            eplPos = 0;
            eplSize = 0;
            tmxSize = 0;
            tmxPos = 0;

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{end}") == 0)
                {
                    break;
                }
                else
                {
                    prms = str.Split(',');
                    cmd = Convert.ToInt32(prms[0], 16);
                    prm1 = Convert.ToInt32(prms[1], 16);
                    prm2 = Convert.ToInt32(prms[2], 16);
                    ms.Position = tblPos;
                    bw.Write(cmd);
                    if ((prm1 < 1) || (prm2 < 1))
                    {
                        bw.Write(prm1);
                        bw.Write(prm2);
                        bw.Write((int)datPos);
                        ms.Position = datPos;
                    }
                    else
                        switch (cmd)
                        {
                            case 0x01: //+
                                bw.Write(prm1);
                                bw.Write(prm2);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                for (i = 0; i < prm2; i++)
                                {
                                    str = sr.ReadLine();
                                    bw.Write(ste.ASCII.GetBytes(str));
                                    if (str.Length < prm1)
                                        bw.Write(new byte[prm1 - str.Length]);
                                }
                                break;
                            case 0x02: //+
                                fi = new sio.FileInfo(parentDir + sr.ReadLine());
                                bw.Write((int)fi.Length);
                                bw.Write((int)1);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                bw.Write(sio.File.ReadAllBytes(fi.FullName));
                                break;
                            case 0x03: //+
                                rmdTblPos = (int)tblPos;
                                rmdSubTblPos = (int)datPos;
                                ms.Position = datPos + prm1 * prm2;

                                rmdMs = new sio.MemoryStream();
                                rmdBw = new sio.BinaryWriter(rmdMs);
                                rmdBw.Write(prm1);
                                rmdBw.Write(prm2);
                                rmdMs.Position += 0x8;

                                subTblPos = rmdMs.Position;
                                datPos = (prm1 * prm2) + subTblPos;
                                rmdDataStart = (int)datPos;
                                rmdSize = 0;
                                rmdIdx = 0;
                                for (i = 0; i < prm2; i++)
                                {
                                    prms = sr.ReadLine().Split(',');

                                    //fi = new sio.FileInfo(parentDir + sr.ReadLine());
                                    AnalyzeDescFile(sr.ReadLine());
                                    rmdMs.Position = subTblPos;
                                    rmdNameIdx = Convert.ToInt32(prms[0], 16);
                                    rmdBw.Write(rmdNameIdx);
                                    rmdBw.Write(rmdNameIdx);
                                    rmdBw.Write((int)3);
                                    rmdBw.Write(++rmdIdx);
                                    rmdBw.Write((int)datPos - rmdDataStart);
                                    rmdBw.Write((int)itMs[gIterCount + 1].Position);
                                    rmdBw.Write((int)Convert.ToInt32(prms[1], 16));
                                    rmdBw.Write((int)0);

                                    rmdMs.Position = datPos;
                                    rmdMs.Write(itMs[gIterCount + 1].GetBuffer(), 0, (int)itMs[gIterCount + 1].Length);
                                    itMs[gIterCount + 1].Close();
                                    rmdMs.AlignWrite(0x10);

                                    rmdSize += (int)(rmdMs.Position - datPos);
                                    datPos = rmdMs.Position;
                                    subTblPos += prm1;
                                }
                                break;
                            case 0x06: //+
                                fi = new sio.FileInfo(parentDir + sr.ReadLine());
                                bw.Write((int)fi.Length);
                                bw.Write((int)1);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                bw.Write(sio.File.ReadAllBytes(fi.FullName));
                                ms.AlignWrite(0x10);
                                break;
                            case 0x07: //+
                                bw.Write(prm1);
                                bw.Write(prm2);
                                bw.Write((int)datPos);
                                subTblPos = datPos;
                                datPos += prm1 * prm2;
                                eplPos = (int)datPos;
                                eplSize = 0;
                                for (i = 0; i < prm2; i++)
                                {
                                    ms.Position = subTblPos;
                                    bw.Write(Convert.ToInt32(sr.ReadLine(), 16));
                                    bw.Write((int)datPos);
                                    bw.Write((int)0x4);
                                    bw.Write((int)0);

                                    ms.Position = datPos;
                                    AnalyzeDescFile(sr.ReadLine());
                                    ms.Write(itMs[gIterCount + 1].GetBuffer(), 0, (int)itMs[gIterCount + 1].Position);
                                    itMs[gIterCount + 1].Close();
                                    //bw.Write(sio.File.ReadAllBytes(parentDir + sr.ReadLine()));
                                    ms.AlignWrite(0x10);

                                    eplSize += (int)(ms.Position - datPos);
                                    datPos = ms.Position;
                                    subTblPos += prm1;
                                }
                                break;
                            case 0x08: //+
                                bw.Write(eplSize);
                                bw.Write((int)1);
                                bw.Write(eplPos);
                                ms.Position = datPos;
                                break;
                            case 0x09: //+
                                bw.Write(rmdSize);
                                bw.Write((int)1);
                                bw.Write((int)datPos);

                                rmdBr = new sio.BinaryReader(rmdMs);
                                rmdMs.Position = 0;

                                ms.Position = rmdTblPos + 0x4;
                                bw.Write(rmdBr.ReadInt32());
                                bw.Write(prm2 = rmdBr.ReadInt32());
                                bw.Write(rmdSubTblPos);
                                rmdMs.Position += 0x8;

                                ms.Position = rmdSubTblPos;
                                for (i = 0; i < prm2; i++)
                                {
                                    bw.Write(rmdBr.ReadInt32());
                                    bw.Write(rmdBr.ReadInt32());
                                    bw.Write(rmdBr.ReadInt32());
                                    bw.Write(rmdBr.ReadInt32());
                                    bw.Write(rmdBr.ReadInt32() + (int)datPos);
                                    bw.Write(rmdBr.ReadInt32());
                                    bw.Write(rmdBr.ReadInt32());
                                    bw.Write(rmdBr.ReadInt32());
                                }

                                ms.Position = datPos;
                                bw.Write(rmdBr.ReadBytes(rmdSize));

                                rmdBr.Close();
                                rmdBw.Close();
                                rmdMs.Close();
                                break;
                            case 0x0a: //+
                                fi = new sio.FileInfo(parentDir + sr.ReadLine());
                                bw.Write((int)fi.Length);
                                bw.Write((int)1);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                bw.Write(sio.File.ReadAllBytes(fi.FullName));
                                break;
                            case 0x0b: //+
                                fi = new sio.FileInfo(parentDir + sr.ReadLine());
                                bw.Write((int)fi.Length);
                                bw.Write((int)1);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                bw.Write(sio.File.ReadAllBytes(fi.FullName));
                                break;
                            case 0x0c: //+
                                AnalyzeDescFile(sr.ReadLine());
                                bw.Write((int)itMs[gIterCount + 1].Length);
                                bw.Write((int)1);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                ms.Write(itMs[gIterCount + 1].GetBuffer(), 0, (int)itMs[gIterCount + 1].Position);
                                itMs[gIterCount + 1].Close();
                                break;
                            case 0x16: //+
                                bw.Write(prm1);
                                bw.Write(prm2);
                                bw.Write((int)datPos);
                                subTblPos = datPos;
                                datPos += prm1;
                                tmxPos = (int)datPos;
                                tmxSize = 0;
                                prm1 /= prm2;
                                for (i = 0; i < prm2; i++)
                                {
                                    ms.Position = subTblPos;
                                    bw.Write(Convert.ToInt32(sr.ReadLine(), 16));
                                    bw.Write((int)datPos);
                                    bw.Write((int)0);
                                    bw.Write((int)0);

                                    ms.Position = datPos;
                                    bw.Write(sio.File.ReadAllBytes(parentDir + sr.ReadLine()));
                                    ms.AlignWrite(0x10);

                                    tmxSize += (int)(ms.Position - datPos);
                                    datPos = ms.Position;
                                    subTblPos += prm1;
                                }
                                break;
                            case 0x17: //+
                                bw.Write(tmxSize);
                                bw.Write((int)1);
                                bw.Write(tmxPos);
                                ms.Position = datPos;
                                break;
                            case 0x1b: //+
                                fi = new sio.FileInfo(parentDir + sr.ReadLine());
                                bw.Write((int)fi.Length);
                                bw.Write((int)1);
                                bw.Write((int)datPos);
                                ms.Position = datPos;
                                bw.Write(sio.File.ReadAllBytes(fi.FullName));
                                break;
                            default:
                                throw new ArgumentException(string.Format("Wrong arch type at '{0}{1}'", parentDir, archName));
                        }
                }
                //ms.Align(0x40);
                datPos = ms.Position;
                tblPos += 0x10;

                str = sr.ReadLine();
            }
            ms.Position = 0x4;
            bw.Write((int)datPos);
            ms.Position = datPos;
        }

        private void RebuildTxp0(string archName, int fileCount)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            long tblPos, datPos;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);

            bw.Write((int)0x9);
            bw.Write((int)0);
            bw.Write((int)0x30505854);
            bw.Write((int)0);
            bw.Write(fileCount);
            tblPos = ms.Position;

            ms.Position += fileCount << 0x2;
            ms.AlignWrite(0x40);
            datPos = ms.Position;

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{end}") == 0)
                {
                    break;
                }
                ms.Position = tblPos;
                bw.Write((int)datPos);
                ms.Position = datPos;
                bw.Write(sio.File.ReadAllBytes(parentDir + str));
                ms.AlignWrite(0x40);
                datPos = ms.Position;
                tblPos += 0x4;

                str = sr.ReadLine();
            }
            datPos = ms.Position;
            ms.Position = 0x4;
            bw.Write((int)datPos);
            ms.Position = datPos;
        }

        private void RebuildPib0(string archName)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            long datPos;
            byte[] buf;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);

            bw.Write((int)1);
            bw.Write((int)0);
            bw.Write((int)0x30424950);
            bw.Write((int)0);
            bw.Write(sio.File.ReadAllBytes(parentDir + sr.ReadLine()));
            datPos = ms.Position;
            ms.Position = 0x4;
            bw.Write((int)datPos);
            ms.Position = datPos;
            ms.AlignWrite(0x40);

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{type") == 0)
                {
                    AnalyzeDescFile(str);
                    buf = new byte[itMs[gIterCount + 1].Position];
                    Array.Copy(itMs[gIterCount + 1].GetBuffer(), 0, buf, 0, (int)itMs[gIterCount + 1].Position);
                    itMs[gIterCount + 1].Close();
                }
                else if (str.IndexOf("{end}") == 0)
                {
                    bw.Write((int)0xff);
                    bw.Write((int)0x10);
                    bw.Write((int)0x30444e45);
                    ms.AlignWrite(0x40);
                    break;
                }
                else
                {
                    buf = sio.File.ReadAllBytes(parentDir + str);
                }
                ms.Write(buf, 0, buf.Length);
                ms.AlignWrite(0x40);

                str = sr.ReadLine();
            }
        }

        private void ImportTmx0Ps2(string archName)
        {
            string str;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            string[] prms;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);
            bw.Write(sio.File.ReadAllBytes(parentDir + archName));

            str = sr.ReadLine();
            while (!string.IsNullOrEmpty(str))
            {
                if (str.IndexOf("{end}") == 0)
                {
                    break;
                }
                prms = str.Split(',');
                ms.Position = Convert.ToInt32(prms[0], 16);
                ms.Write(sio.File.ReadAllBytes(parentDir + sr.ReadLine()), 0, Convert.ToInt32(prms[1], 16));

                str = sr.ReadLine();
            }
            ms.Position = ms.Length;
        }

        private void RebuildCin(string archName)
        {
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            string str;

            itMs[gIterCount] = new sio.MemoryStream();
            ms = itMs[gIterCount];
            bw = new sio.BinaryWriter(ms);

            bw.Write(sio.File.ReadAllBytes(parentDir + sr.ReadLine()));

            str = sr.ReadLine();
            if (str.IndexOf("{end}") < 0)
            {
                AnalyzeDescFile(str);
                ms.Write(itMs[gIterCount + 1].GetBuffer(), 0, (int)itMs[gIterCount + 1].Position);
                itMs[gIterCount + 1].Close();
                sr.ReadLine();
            }
        }
    }
}
