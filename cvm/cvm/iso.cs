using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using sirs = System.Runtime.InteropServices;

namespace cvm
{
    public class ISO9660
    {
        public const int logSize = 0x800;
        public const int fsStart = 0x10;

        public static int fsOff { get; private set; }
        public static int fsOffLog { get; private set; }

        private string isoPath;
        private string rebPath;

        public VolumePartition partition;

        public ISO9660(string isoPath, int fileSystemOffset)
        {
            sio.FileStream fs;

            fsOffLog = fileSystemOffset;
            fsOff = fsOffLog * logSize;

            this.isoPath = isoPath;
            Console.WriteLine(string.Format("Loading iso '{0}'", isoPath));

            fs = new sio.FileStream(isoPath, sio.FileMode.Open, sio.FileAccess.Read);
            fs.Position = fsStart * logSize + fsOff;
            partition = new VolumePartition(fs);
            fs.Close();

            Console.WriteLine("Done");
        }

        public ISO9660(string partIniPath)
        {
            PrimaryVolumeDescriptor pvd;
            PathTableRecords paths;

            Console.WriteLine("Reading file structure");

            partIniPath = misc.DICreateAndCheck(partIniPath).FullName;
            this.rebPath = partIniPath;

            FillDirFils(partIniPath, out pvd, out paths);
            paths.SortFiles();
            paths.SetRecSize();
            paths.CalcSizeLocs(partIniPath, pvd);
            paths.SetAttributes(partIniPath, pvd);

            partition = new VolumePartition(pvd, paths);
        }

        private void FillDirFils(string iniPath, out PrimaryVolumeDescriptor pvd, out PathTableRecords paths)
        {
            sio.FileInfo fi;
            sio.StreamReader sr;
            PathTableRecord pth;
            List<string> files;
            string[] file;
            string[] sep;
            int sepPos;
            string str;
            int i, j;

            fi = new sio.FileInfo(iniPath + "partition.inf");
            if (fi.Exists)
                sr = new sio.StreamReader(fi.FullName);
            else
                sr = new sio.StreamReader(new sio.MemoryStream(new byte[] { 0x0d, 0x0a, 0x0d, 0x0a, 0x0d, 0x0a, 0x0d, 0x0a, 0x0d, 0x0a, 0x0d, 0x0a }));

            pvd = new PrimaryVolumeDescriptor();
            pvd.hdr2.sysId = misc.GetPadResChars(sr.ReadLine(), pvd.hdr2.sysId.Length);
            pvd.hdr2.volId = misc.GetPadResChars(sr.ReadLine(), pvd.hdr2.volId.Length);
            pvd.hdr3.volSetId = misc.GetPadResChars(sr.ReadLine(), pvd.hdr3.volSetId.Length);
            pvd.hdr3.publId = misc.GetPadResChars(sr.ReadLine(), pvd.hdr3.publId.Length);
            pvd.hdr3.prepId = misc.GetPadResChars(sr.ReadLine(), pvd.hdr3.prepId.Length);
            pvd.hdr3.appId = misc.GetPadResChars(sr.ReadLine(), pvd.hdr3.appId.Length);
            pvd.hdr3.copFileId = misc.GetPadResChars("", pvd.hdr3.copFileId.Length);
            pvd.hdr3.absFileId = misc.GetPadResChars("", pvd.hdr3.absFileId.Length);
            pvd.hdr3.bibFileId = misc.GetPadResChars("", pvd.hdr3.bibFileId.Length);

            files = new List<string>();
            if (fi.Exists)
            {
                str = sr.ReadLine();
                while (str != null)
                {
                    if (str.Length > 0)
                        files.Add(str);
                    str = sr.ReadLine();
                }
            }
            else
            {
                files.AddRange(sio.Directory.GetDirectories(iniPath, "*", sio.SearchOption.AllDirectories));
                for (i = 0; i < files.Count; i++)
                    files[i] += config.dirSep;

                files.AddRange(sio.Directory.GetFiles(iniPath, "*.*", sio.SearchOption.AllDirectories));
                for (i = 0; i < files.Count; i++)
                    files[i] = string.Format("0, 0, 0, {0}", files[i].Remove(0, iniPath.Length));
            }

            sr.Close();

            paths = new PathTableRecords();

            paths.Add(new PathTableRecord("", false));
            sep = new string[] { ", " };
            for (i = 0; i < files.Count; i++)
            {
                file = files[i].Split(sep, StringSplitOptions.None);
                str = file[3];
                if (str[str.Length - 1] == config.dirSep)
                    if (sio.Directory.Exists(iniPath + str))
                        paths.Add(new PathTableRecord(str, Convert.ToBoolean(int.Parse(file[1]))));
                    else
                        throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", iniPath + str));
            }
            paths.SortDirs();
            paths.SetParentDirNums();

            for (i = 0; i < paths.Count; i++)
            {
                pth = paths[i];
                pth.files = new FileRecords();
                pth.files.Add(new FileDirectoryDescriptor("", true, 0, false, false));
                for (j = i + 1; j < paths.Count; j++)
                    if (paths[j].hdr.parDirNum == i)
                        pth.files.Add(new FileDirectoryDescriptor(paths[j].fullName, true, 0, false, false));
            }

            for (i = 0; i < files.Count; i++)
            {
                file = files[i].Split(sep, StringSplitOptions.None);
                str = file[3];
                if (str[str.Length - 1] != config.dirSep)
                    if (sio.File.Exists(iniPath + str))
                    {
                        sepPos = str.LastIndexOf(config.dirSep) + 1;
                        for (j = 0; j < paths.Count; j++)
                            if (paths[j].fullName.Length == sepPos)
                                if (str.IndexOf(paths[j].fullName) == 0)
                                {
                                    paths[j].files.Add(new FileDirectoryDescriptor(str, false, i, Convert.ToBoolean(int.Parse(file[1])), Convert.ToBoolean(int.Parse(file[2]))));
                                    break;
                                }
                        if (j == paths.Count)
                            throw new ArgumentException(string.Format("Parent directory not found for '{0}'", str));
                    }
                    else
                        throw new sio.FileNotFoundException(string.Format("File '{0}' not found", iniPath + str));
            }
        }

        public void ExtractPartition(string extrPath)
        {
            sio.FileStream fs;

            if (partition == null)
                throw new NullReferenceException("No partitions in this iso");

            Console.WriteLine("Extracting partition");

            extrPath = misc.DICreateAndCheck(extrPath).FullName;
            fs = new sio.FileStream(isoPath, sio.FileMode.Open, sio.FileAccess.Read);

            if (fsOffLog > 0)
            {
                Console.WriteLine("Extracting partition header");
                fs.Extract(extrPath + "partition.hdr", 0, fsOff);
            }

            partition.ExtractAll(fs, extrPath);

            fs.Close();

            ExtractInfo(extrPath);

            Console.WriteLine("Done");
        }

        public void RebuildPartition(string newIsoPath)
        {
            sio.FileStream fs;
            sio.FileInfo fi;

            Console.WriteLine("Rebuilding");

            fs = new sio.FileStream(newIsoPath, sio.FileMode.Create, sio.FileAccess.Write);

            fi = new sio.FileInfo(rebPath + "partition.hdr");
            if (fi.Exists)
            {
                fs.Insert(fi.FullName, logSize);
                fsOffLog = (int)(fs.Position / logSize);
            }
            else
                fsOffLog = 0;
            fsOff = fsOffLog * logSize;

            fs.Position = (fsOffLog + fsStart) * logSize;
            partition.Save(fs);
            fs.Flush();
            partition.ImportAll(fs, rebPath);

            fs.Close();
        }

        private void ExtractInfo(string extrPath)
        {
            sio.StreamWriter sw;
            List<FileDirectoryDescriptor> files;
            int i, j;

            files = new List<FileDirectoryDescriptor>();

            for (i = 0; i < partition.paths.Count; i++)
                for (j = 1; j < partition.paths[i].files.Count; j++)
                    files.Add(partition.paths[i].files[j]);
            files.Sort(misc.CompareLocs);

            sw = new sio.StreamWriter(extrPath + "partition.inf");

            sw.WriteLine(string.Format("sysId={0}", partition.primary.hdr2.sysId.ToStr().Trim()));
            sw.WriteLine(string.Format("volId={0}", partition.primary.hdr2.volId.ToStr().Trim()));
            sw.WriteLine(string.Format("volSetId={0}", partition.primary.hdr3.volSetId.ToStr().Trim()));
            sw.WriteLine(string.Format("pubId={0}", partition.primary.hdr3.publId.ToStr().Trim()));
            sw.WriteLine(string.Format("datPrepId={0}", partition.primary.hdr3.prepId.ToStr().Trim()));
            sw.WriteLine(string.Format("appId={0}", partition.primary.hdr3.appId.ToStr().Trim()));

            foreach (var fl in files)
                sw.WriteLine(string.Format("{0:d8}, {1}, {2}, {3}", fl.hdr.locL, Convert.ToInt32(fl.isHidden), Convert.ToInt32(fl.isXa), fl.fullName));

            sw.Close();
        }
    }

    public class VolumePartition
    {
        public PrimaryVolumeDescriptor primary = null;
        public VolumePartitionDescriptor partition = null;
        public VolumeDescriptorSetTerminator terminator = null;
        public PathTableRecords paths = null;

        public VolumePartition(sio.FileStream fs)
        {
            bool termFound;

            termFound = false;
            while (!termFound)
            {
                switch (fs.ReadByte())
                {
                    case 0:
                        throw new NotImplementedException("Boot records are not supported");
                    case 1:
                        primary = new PrimaryVolumeDescriptor(fs);
                        break;
                    case 2:
                        throw new NotImplementedException("Supplementary descriptors are not supported");
                    case 3:
                        partition = new VolumePartitionDescriptor(fs);
                        break;
                    case 255:
                        terminator = new VolumeDescriptorSetTerminator(fs);
                        termFound = true;
                        break;
                    default:
                        throw new NotImplementedException(string.Format("{0:x}: descriptor is not supported", fs.Position - 1));
                }
            }

            fs.Position = (long)primary.hdr3.locLTbl * ISO9660.logSize + ISO9660.fsOff;
            paths = new PathTableRecords(fs, primary.hdr3.pthTblSizeL);
            for (int i = 0; i < paths.Count; i++)
            {
                fs.Position = (long)paths[i].hdr.locExtent * ISO9660.logSize + ISO9660.fsOff;
                paths[i].files = new FileRecords(fs, paths[i].name, paths[i].fullName);
            }
        }

        public VolumePartition(PrimaryVolumeDescriptor pvd, PathTableRecords paths)
        {
            this.primary = pvd;
            this.paths = paths;
            terminator = new VolumeDescriptorSetTerminator();
        }

        public void ExtractAll(sio.FileStream fs, string extrPath)
        {
            sio.DirectoryInfo di;
            int i, j;

            if (primary == null)
                return;

            extrPath = misc.DICreateAndCheck(extrPath).FullName;
            for (i = 0; i < paths.Count; i++)
            {
                di = misc.DICreateAndCheck(extrPath + paths[i].fullName);
                for (j = 1; j < paths[i].files.Count; j++)
                    if (!paths[i].files[j].isDirectory)
                    {
                        Console.WriteLine(string.Format("Extracting file '{0}'", paths[i].fullName + paths[i].files[j].name));
                        fs.Extract(di.FullName + paths[i].files[j].name, (long)paths[i].files[j].hdr.locL * ISO9660.logSize + ISO9660.fsOff, (uint)paths[i].files[j].hdr.datLenL);
                    }
            }
        }

        public void Save(sio.FileStream fs)
        {
            primary.Save(fs);
            terminator.Save(fs);
            paths.Save(fs);
        }

        public void ImportAll(sio.FileStream fs, string path)
        {
            List<FileDirectoryDescriptor> files;
            int i, j;

            files = new List<FileDirectoryDescriptor>();

            for (i = 0; i < paths.Count; i++)
                for (j = 0; j < paths[i].files.Count; j++)
                    if (!paths[i].files[j].isDirectory)
                        files.Add(paths[i].files[j]);
            files.Sort(misc.CompareLocs);

            foreach (var file in files)
            {
                Console.WriteLine(string.Format("Importing file '{0}'", file.fullName));
                fs.Insert(path + file.fullName, ISO9660.logSize);
            }
        }
    }

    public class PathTableRecord
    {
        public static int DefaultSize = misc.GetStructSize<Header>();
        public int RecSize { get; private set; }

        [sirs.StructLayout(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public byte lenDirId;
            public byte exAttrRecLen;
            public int locExtent;
            public short parDirNum;
        }

        public Header hdr;
        public string name;
        public string fullName;
        public FileRecords files;
        public bool isHidden;

        public PathTableRecord(System.IO.FileStream fs)
        {
            hdr = misc.ReadStruct<Header>(fs);
            name = fs.ReadString(hdr.lenDirId);
            hdr.parDirNum--; //parent numbering starts with 1

            if ((fs.Position & 1) == 1)
                fs.ReadByte();
        }

        public PathTableRecord(string fullName, bool isHidden)
        {
            int i, j;

            this.isHidden = isHidden;
            this.fullName = fullName;
            i = fullName.LastIndexOf(config.dirSep);
            if (i > 0)
            {
                j = fullName.LastIndexOf(config.dirSep, i - 1) + 1;
                if (j > 0)
                    name = fullName.Substring(j, i - j);
                else
                    name = fullName.Substring(0, i);
            }
            else
                name = "";
        }

        public PathTableRecord(short parNum, string parName, string name)
        {
            hdr.exAttrRecLen = 0;
            hdr.parDirNum = parNum;
            fullName = parName + name + config.dirSep;
            this.name = name;
        }

        public void Save(sio.FileStream fs)
        {
            hdr.parDirNum++;
            misc.WriteStruct(fs, hdr);
            hdr.parDirNum--;
            fs.WriteString(name);

            if ((fs.Position & 1) == 1)
                fs.WriteByte(0);
        }

        public void SaveM(sio.FileStream fs)
        {
            sio.BinaryWriter bw;

            bw = new sio.BinaryWriter(fs);

            hdr.parDirNum++;
            fs.WriteByte(hdr.lenDirId);
            fs.WriteByte(hdr.exAttrRecLen);
            bw.Write(hdr.locExtent.ChangeEndian());
            bw.Write(hdr.parDirNum.ChangeEndian());
            hdr.parDirNum--;
            fs.WriteString(name);

            if ((fs.Position & 1) == 1)
                fs.WriteByte(0);
        }

        public void SortFiles()
        {
            files.SortFiles();
        }

        public void SetRecSize()
        {
            if (name.Length == 0)
                hdr.lenDirId = 1;
            else
                hdr.lenDirId = (byte)name.Length;
            RecSize = DefaultSize + hdr.lenDirId + (hdr.lenDirId & 1);
        }

        public void SetHiddenAttr(bool isHdden)
        {
            files.SetHiddenAttr(isHidden);
        }

        public override string ToString()
        {
            return fullName;
        }
    }

    public class PathTableRecords
    {
        public int Count { get { return recs.Count; } }
        public int RecSize { get; private set; }

        List<PathTableRecord> recs;

        public PathTableRecord this[int idx]
        {
            get { return recs[idx]; }
        }

        public PathTableRecords(sio.FileStream fs, int recLen)
        {
            sio.BinaryReader br;
            PathTableRecord lastRec;
            long recEnd;

            br = new sio.BinaryReader(fs);
            recs = new List<PathTableRecord>();

            recEnd = fs.Position + recLen;
            recs.Add(new PathTableRecord(fs) { fullName = "" });
            while (fs.Position < recEnd)
            {
                recs.Add(lastRec = new PathTableRecord(fs));
                lastRec.fullName = recs[lastRec.hdr.parDirNum].fullName + lastRec.name + config.dirSep;
            }
        }

        public PathTableRecords()
        {
            recs = new List<PathTableRecord>();
        }

        public void Add(PathTableRecord pth)
        {
            recs.Add(pth);
        }

        public void SortDirs()
        {
            //foreach (var rec in recs)
            //{
            //    rec.fullName.Replace('_', 'z');
            //}
            recs.Sort(misc.CompareDirs);
            //foreach (var rec in recs)
            //{
            //    rec.fullName.Replace('z', '_');
            //}
        }

        public void SetParentDirNums()
        {
            string s;
            int i, j;

            for (i = recs.Count - 1; i > 1; i--)
            {
                s = recs[i].fullName.Substring(0, recs[i].fullName.LastIndexOf(recs[i].name));
                for (j = 0; j < recs.Count; j++)
                    if (recs[j].fullName.Length == s.Length)
                        if (recs[j].fullName.CompareTo(s) == 0)
                        {
                            recs[i].hdr.parDirNum = (short)j;
                            break;
                        }
                if (j == recs.Count)
                    throw new ArgumentException(string.Format("Parent directory not found for '{0}'", recs[i].fullName));
            }
        }

        public void SortFiles()
        {
            foreach (var rec in recs)
                rec.SortFiles();
        }

        public void SetRecSize()
        {
            int logSize;
            int mod;

            logSize = ISO9660.logSize - 1;
            RecSize = 0;
            foreach (var rec in recs)
            {
                rec.SetRecSize();
                if ((mod = (RecSize + rec.RecSize) & logSize) < rec.RecSize)
                    RecSize += rec.RecSize - mod;
                RecSize += rec.RecSize;

                rec.files.SetRecSize();
            }
        }

        public void CalcSizeLocs(string path, PrimaryVolumeDescriptor pvd)
        {
            List<FileDirectoryDescriptor> files, dirs;
            sio.FileInfo fi;
            int filStart;
            int logSize;
            int mod;
            int i;
            long l;

            logSize = ISO9660.logSize - 1;

            files = new List<FileDirectoryDescriptor>();
            foreach (var rec in recs)
                for (i = 0; i < rec.files.Count; i++)
                    if (!rec.files[i].isDirectory)
                        files.Add(rec.files[i]);
            files.Sort(misc.CompareLocs);

            filStart = ISO9660.fsStart;
            filStart++; //prim desc
            filStart++; //terminator

            i = RecSize;
            if ((mod = i & logSize) > 0)
                i += ISO9660.logSize - mod;
            i = i / ISO9660.logSize;

            pvd.hdr3.locLTbl = filStart;
            pvd.hdr3.locOptLTbl = 0;
            filStart += i;
            pvd.hdr3.locMTbl = filStart.ChangeEndian();
            pvd.hdr3.locOptMTbl = 0;
            filStart += i;

            foreach (var rec in recs)
            {
                i = rec.files.RecSize;
                if ((mod = i & logSize) > 0)
                    i += ISO9660.logSize - mod;
                rec.hdr.locExtent = filStart;
                rec.files[0].hdr.locL = filStart;
                rec.files[0].hdr.locM = filStart.ChangeEndian();
                rec.files[0].hdr.datLenL = i;
                rec.files[0].hdr.datLenM = i.ChangeEndian();
                filStart += (i / ISO9660.logSize); //fil/dir descs
            }

            dirs = new List<FileDirectoryDescriptor>();
            foreach (var rec in recs)
                for (i = 0; i < rec.files.Count; i++)
                    if (rec.files[i].isDirectory)
                        dirs.Add(rec.files[i]);
            foreach (var dir in dirs)
                foreach (var rec in recs)
                    if (rec.fullName.Length > 0)
                        if (rec.fullName.Length == dir.fullName.Length)
                            if (rec.fullName == dir.fullName)
                            {
                                mod = rec.files.RecSize & logSize;
                                dir.hdr.datLenL = (mod > 0) ? rec.files.RecSize + (ISO9660.logSize - mod) : rec.files.RecSize;
                                dir.hdr.datLenM = dir.hdr.datLenL.ChangeEndian();
                                dir.hdr.locL = rec.hdr.locExtent;
                                dir.hdr.locM = dir.hdr.locL.ChangeEndian();
                            }

            pvd.hdr3.rootDir.locL = recs[0].files[0].hdr.locL;
            pvd.hdr3.rootDir.locM = pvd.hdr3.rootDir.locL.ChangeEndian();
            pvd.hdr3.rootDir.datLenL = recs[0].files[0].hdr.datLenL;
            pvd.hdr3.rootDir.datLenM = pvd.hdr3.rootDir.datLenL.ChangeEndian();

            foreach (var fil in files)
            {
                fi = new sio.FileInfo(path + fil.fullName);
                if (fi.Exists)
                {
                    fil.hdr.datLenL = (int)((fi.Length << 0x20) >> 0x20);
                    fil.hdr.datLenM = fil.hdr.datLenL.ChangeEndian();
                    fil.hdr.locL = filStart;
                    fil.hdr.locM = filStart.ChangeEndian();
                    l = fi.Length;
                    if ((mod = (int)(l & logSize)) > 0)
                        l += ISO9660.logSize - mod;
                    filStart += (int)(l / ISO9660.logSize); //fil/dir descs
                }
                else
                    throw new sio.FileNotFoundException(string.Format("File '{0}' not found", fi.FullName));
            }

            pvd.hdr3.volSizeL = filStart;
            pvd.hdr3.volSizeM = pvd.hdr3.volSizeL.ChangeEndian();
            pvd.hdr3.volSetSizeL = 1;
            pvd.hdr3.volSetSizeM = pvd.hdr3.volSetSizeL.ChangeEndian();
            pvd.hdr3.volSeqNumL = 1;
            pvd.hdr3.volSeqNumM = pvd.hdr3.volSeqNumL.ChangeEndian();
            pvd.hdr3.logBlkSizeL = ISO9660.logSize;
            pvd.hdr3.logBlkSizeM = pvd.hdr3.logBlkSizeL.ChangeEndian();
            pvd.hdr3.pthTblSizeL = RecSize;
            pvd.hdr3.pthTblSizeM = pvd.hdr3.pthTblSizeL.ChangeEndian();
        }

        public void SetAttributes(string path, PrimaryVolumeDescriptor pvd)
        {
            const int yearOffset = 1900;
            sio.DirectoryInfo di;
            sio.FileInfo fi;
            sio.FileSystemInfo fsi;
            FileDirectoryDescriptor fd;
            byte utc;
            int i;

            utc = misc.GetLocalUtc15();
            foreach (var rec in recs)
            {
                for (i = 0; i < rec.files.Count; i++)
                {
                    fd = rec.files[i];
                    if (fd.isDirectory)
                    {
                        di = new sio.DirectoryInfo(path + fd.fullName);
                        if (di.Exists)
                            fsi = di;
                        else
                            throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found"));
                        fd.hdr.filFlags = 0x2;
                    }
                    else
                    {
                        fi = new sio.FileInfo(path + fd.fullName);
                        if (fi.Exists)
                            fsi = fi;
                        else
                            throw new sio.FileNotFoundException(string.Format("File '{0}' not found"));
                    }
                    fd.hdr.recdDatTim.year = (byte)(fsi.LastWriteTimeUtc.Year - yearOffset);
                    fd.hdr.recdDatTim.month = (byte)fsi.LastWriteTimeUtc.Month;
                    fd.hdr.recdDatTim.day = (byte)fsi.LastWriteTimeUtc.Day;
                    fd.hdr.recdDatTim.hour = (byte)fsi.LastWriteTimeUtc.Hour;
                    fd.hdr.recdDatTim.min = (byte)fsi.LastWriteTimeUtc.Minute;
                    fd.hdr.recdDatTim.sec = (byte)fsi.LastWriteTimeUtc.Second;
                    fd.hdr.recdDatTim.gmt = utc;
                }

                SetHiddenAttr(0, false);
            }
            pvd.hdr3.rootDir.recdDatTim = recs[0].files[0].hdr.recdDatTim;

            fsi = new sio.DirectoryInfo(path);

            pvd.hdr3.volCreDatTim = new VolumeDateTime(fsi.CreationTimeUtc).hdr;
            pvd.hdr3.volModDatTim = new VolumeDateTime(fsi.LastWriteTimeUtc).hdr;
            pvd.hdr3.volEffDatTim = new VolumeDateTime(new DateTime(0)).hdr;
            pvd.hdr3.volExpDatTim = new VolumeDateTime(new DateTime(0)).hdr;
        }

        private void SetHiddenAttr(int parnum, bool isHidden)
        {
            int i;

            for (i = 0; i < recs.Count; i++)
            {
                if (i == parnum)
                    continue;
                if (recs[i].hdr.parDirNum == parnum)
                {
                    if (parnum > 0) //root dir cannot be hidden
                        recs[i].isHidden = isHidden;
                    recs[i].files.SetHiddenAttr(recs[i].isHidden);
                    SetHiddenAttr(i, recs[i].isHidden);
                }
            }
        }

        public void CreateDirs()
        {
            sio.DirectoryInfo di;

            for (int i = 1; i < Count; i++)
            {
                di = new sio.DirectoryInfo(config.extrPath + recs[i].fullName);
                if (!di.Exists)
                    di.Create();
            }
        }

        public void Save(sio.FileStream fs)
        {
            int i;

            foreach (var rec in recs)
            {
                fs.CondAlign(ISO9660.logSize, rec.RecSize);
                rec.Save(fs);
            }
            fs.Align(ISO9660.logSize);

            foreach (var rec in recs)
            {
                fs.CondAlign(ISO9660.logSize, rec.RecSize);
                rec.SaveM(fs);
            }
            fs.Align(ISO9660.logSize);

            for (i = 0; i < recs.Count; i++)
            {
                if (i == 0)
                    recs[i].files.Save(fs, recs[0].files[0]);
                else
                    recs[i].files.Save(fs, recs[i - 1].files[0]);
            }
        }

    }

    public class VolumeDescriptor
    {
        private const string stdIds = "CD001";

        [sirs.StructLayoutAttribute(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header1
        {
            //public byte descType;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 5)]
            public char[] stdIdb;
            public byte descVer;
        }
        public Header1 hdr1;

        public VolumeDescriptor(sio.FileStream fs)
        {
            hdr1 = misc.ReadStruct<Header1>(fs);

            if (hdr1.stdIdb.ToStr() != stdIds)
                throw new NotImplementedException("This is not iso9660 filesystem");
            if (fs.ReadByte() > 0)
                throw new NotImplementedException("Not supported format of iso9660 filesystem");
        }

        public VolumeDescriptor()
        {
            hdr1 = misc.AllocStruct<Header1>();
            hdr1.stdIdb = stdIds.ToCharArray();
            hdr1.descVer = 1;
        }

        public virtual void Save(sio.FileStream fs)
        {
            misc.WriteStruct(fs, hdr1);
            fs.WriteByte(0);
        }
    }

    public class VolumePartitionDescriptor : VolumeDescriptor
    {
        [sirs.StructLayout(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header2
        {
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] sysId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] volId;
            public int volLocL;
            public int volLocM;
        }
        public Header2 hdr2;

        public VolumePartitionDescriptor(sio.FileStream fs)
            : base(fs)
        {
            hdr2 = misc.ReadStruct<Header2>(fs);
        }

        public VolumePartitionDescriptor()
            : base()
        {
            hdr2 = misc.AllocStruct<Header2>();
        }

        public override void Save(sio.FileStream fs)
        {
            base.Save(fs);
            misc.WriteStruct(fs, hdr2);
        }
    }

    public class PrimaryVolumeDescriptor : VolumePartitionDescriptor
    {
        [sirs.StructLayout(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header3
        {
            public int volSizeL;
            public int volSizeM;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] unk2;
            public short volSetSizeL;
            public short volSetSizeM;
            public short volSeqNumL;
            public short volSeqNumM;
            public short logBlkSizeL;
            public short logBlkSizeM;
            public int pthTblSizeL;
            public int pthTblSizeM;
            public int locLTbl;
            public int locOptLTbl;
            public int locMTbl;
            public int locOptMTbl;
            public FileDirectoryDescriptor.Header rootDir;
            public char rootDirName;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] volSetId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] publId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] prepId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] appId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 37)]
            public char[] copFileId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 37)]
            public char[] absFileId;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 37)]
            public char[] bibFileId;
            public VolumeDateTime.Header volCreDatTim;
            public VolumeDateTime.Header volModDatTim;
            public VolumeDateTime.Header volExpDatTim;
            public VolumeDateTime.Header volEffDatTim;
            public byte fileStructVer;
            public byte res1;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] appUse;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 653)]
            public byte[] res2;
        }
        public Header3 hdr3;

        public PrimaryVolumeDescriptor(sio.FileStream fs)
            : base(fs)
        {
            hdr3 = misc.ReadStruct<Header3>(fs);
        }

        public PrimaryVolumeDescriptor()
            : base()
        {
            hdr3 = misc.AllocStruct<Header3>();

            hdr3.rootDir.lenDirRec = (byte)(FileDirectoryDescriptor.DefaultSize + 1);
            hdr3.rootDir.filFlags = 2;
            hdr3.rootDir.volSeqNumL = 1;
            hdr3.rootDir.volSeqNumM = hdr3.rootDir.volSeqNumL.ChangeEndian();
            hdr3.rootDir.lenFilId = 1;
            hdr3.fileStructVer = 1;
        }

        public override void Save(sio.FileStream fs)
        {
            fs.WriteByte(1);
            base.Save(fs);
            misc.WriteStruct(fs, hdr3);
        }
    }

    public class VolumeDescriptorSetTerminator : VolumeDescriptor
    {
        public VolumeDescriptorSetTerminator(sio.FileStream fs)
            : base(fs)
        {

        }

        public VolumeDescriptorSetTerminator()
            : base()
        {

        }

        public override void Save(sio.FileStream fs)
        {
            fs.WriteByte(255);
            base.Save(fs);
            fs.Align(ISO9660.logSize);
        }
    }

    public class FileDirectoryDescriptor
    {
        public static int DefaultSize = misc.GetStructSize<Header>();
        public int RecSize { get; private set; }

        [sirs.StructLayout(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public byte lenDirRec;
            public byte exAttrRecLen;
            public int locL;
            public int locM;
            public int datLenL;
            public int datLenM;
            public Header1 recdDatTim;
            public byte filFlags;
            public byte filUnitSize;
            public byte intGapSize;
            public short volSeqNumL;
            public short volSeqNumM;
            public byte lenFilId;
        }

        [sirs.StructLayout(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header1
        {
            public byte year;
            public byte month;
            public byte day;
            public byte hour;
            public byte min;
            public byte sec;
            public byte gmt;
        }

        public Header hdr;
        public string name;
        public string fullName;
        public bool isDirectory;
        public bool isHidden;
        public bool isXa;

        public FileDirectoryDescriptor(sio.FileStream fs)
        {
            int i;

            hdr = misc.ReadStruct<Header>(fs);
            name = fs.ReadString(hdr.lenFilId);
            isDirectory = (hdr.filFlags & 2) == 2;
            if (!isDirectory)
                if ((i = name.LastIndexOf(';')) > -1)
                    name = name.Remove(i, name.Length - i);

            for (i = DefaultSize + hdr.lenFilId; i < hdr.lenDirRec; i++) //skip 'System use' field
                fs.ReadByte();
        }

        public FileDirectoryDescriptor(string fullName, bool isDir, int tempLba, bool isHidden, bool isXa)
        {
            int i;

            this.name = "";
            this.fullName = fullName;
            this.isDirectory = isDir;
            this.isHidden = isHidden;
            this.isXa = isXa;
            this.hdr.locL = tempLba;
            this.hdr.volSeqNumL = 1;
            this.hdr.volSeqNumM = this.hdr.volSeqNumL.ChangeEndian();

            if (!isDir)
            {
                i = fullName.LastIndexOf(config.dirSep) + 1;
                if (i > 0)
                    name = fullName.Substring(i, fullName.Length - i);
                else
                    name = fullName;
            }
            else
            {
                i = fullName.LastIndexOf(config.dirSep, fullName.Length - 2) + 1;
                if (i > 0)
                    name = fullName.Substring(i, fullName.Length - i - 1);
                else if (fullName.Length > 0)
                    name = fullName.Substring(0, fullName.Length - 1);
            }
        }

        public void SetRecSize()
        {
            if (name.Length == 0)
                hdr.lenFilId = 1;
            else
                hdr.lenFilId = (byte)(name.Length + (isDirectory ? 0 : 2)); //file version
            RecSize = DefaultSize + hdr.lenFilId;
            RecSize += RecSize & 1;
            hdr.lenDirRec = (byte)RecSize;
        }

        public void Save(sio.FileStream fs)
        {
            misc.WriteStruct(fs, hdr);
            fs.WriteString(name.ToUpper());
            if (!isDirectory)
                fs.WriteString(";1");
            if ((fs.Position & 1) == 1)
                fs.WriteByte(0);
        }

        public override string ToString()
        {
            return fullName;
        }
    }

    public class VolumeDateTime
    {
        private const string frmStr = "{{0:d{0}}}";

        [sirs.StructLayout(sirs.LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] year;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] month;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] day;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] hour;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] min;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] sec;
            [sirs.MarshalAs(sirs.UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] hsec;
            public byte gmt;
        }
        public Header hdr;

        public VolumeDateTime(DateTime dt)
        {
            hdr = misc.AllocStruct<Header>();

            if (dt.Ticks > 0)
            {
                hdr.year = string.Format(string.Format(frmStr, hdr.year.Length), dt.Year).ToCharArray();
                hdr.month = string.Format(string.Format(frmStr, hdr.month.Length), dt.Month).ToCharArray();
                hdr.day = string.Format(string.Format(frmStr, hdr.day.Length), dt.Day).ToCharArray();
                hdr.hour = string.Format(string.Format(frmStr, hdr.hour.Length), dt.Hour).ToCharArray();
                hdr.min = string.Format(string.Format(frmStr, hdr.min.Length), dt.Minute).ToCharArray();
                hdr.sec = string.Format(string.Format(frmStr, hdr.sec.Length), dt.Second).ToCharArray();
                hdr.hsec = string.Format(string.Format(frmStr, hdr.hsec.Length), dt.Millisecond / 10).ToCharArray();
                hdr.gmt = misc.GetLocalUtc15();
            }
        }

    }

    public class FileRecords
    {
        public int Count { get { return files.Count; } }
        public int RecSize { get; private set; }

        List<FileDirectoryDescriptor> files;

        public FileDirectoryDescriptor this[int idx]
        {
            get
            {
                return files[idx];
            }
            set
            {
                files[idx] = value;
            }
        }

        public FileRecords(sio.FileStream fs, string dirName, string dirFullName)
        {
            FileDirectoryDescriptor fd;
            long recEnd;

            files = new List<FileDirectoryDescriptor>();

            recEnd = fs.Position;
            fd = new FileDirectoryDescriptor(fs); //curr dir
            fd.name = dirName;
            fd.fullName = dirFullName;
            files.Add(fd);
            recEnd += fd.hdr.datLenL;

            fs.Position += fs.ReadByte(); //skip parent dir
            while (fs.Position < recEnd)
            {
                fd = new FileDirectoryDescriptor(fs);
                if (fd.hdr.lenDirRec > FileDirectoryDescriptor.DefaultSize)
                {
                    fd.fullName = dirFullName + fd.name;
                    if (fd.isDirectory)
                        if (fd.fullName.Length > 0)
                            fd.fullName += config.dirSep;
                    files.Add(fd);
                }
                fs.CondAlign(ISO9660.logSize, FileDirectoryDescriptor.DefaultSize);
            }
        }

        public FileRecords()
        {
            files = new List<FileDirectoryDescriptor>();
        }

        public void Add(FileDirectoryDescriptor rec)
        {
            files.Add(rec);
        }

        public void SortFiles()
        {
            files.Sort(misc.CompareFiles);
        }

        public void SetRecSize()
        {
            RecSize = FileDirectoryDescriptor.DefaultSize + 1; //parent dir
            foreach (var rec in files)
            {
                rec.SetRecSize();
                RecSize = RecSize.CondAlign(ISO9660.logSize, rec.RecSize);
                RecSize += rec.RecSize;
            }
        }

        public void Save(sio.FileStream fs, FileDirectoryDescriptor parent)
        {
            int i;

            misc.WriteStruct(fs, files[0].hdr);
            fs.WriteByte(0);

            misc.WriteStruct(fs, parent.hdr);
            fs.WriteByte(1);

            for (i = 1; i < files.Count; i++)
            {
                fs.CondAlign(ISO9660.logSize, files[i].RecSize);
                files[i].Save(fs);
            }
            fs.Align(ISO9660.logSize);
        }

        public void SetHiddenAttr(bool isHidden)
        {
            foreach (var fil in files)
                fil.isHidden = isHidden;
        }
    }
}
