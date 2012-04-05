using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace cvm
{
    public class CvmClass
    {
        private class VfsParams
        {
            private static string[] exeNames = new string[] { "SLUS_216.21" };
            private static string[,] vfsNames = new string[,] {
                {"data.cvm", "bgm.cvm", "btl.cvm"} };
            private static int[,] vfsPoses = new int[,] {
                {0x4E51C8, 0x541958, 0x5443A8} };

            public string exeName;
            public int vfsPos;

            public VfsParams(string gameDir, string archName)
            {
                sio.FileInfo fi;
                int i, j;

                fi = new sio.FileInfo(archName);
                archName = fi.Name.ToLower();

                j = 0;
                for (i = 0; i < exeNames.Length; i++)
                {
                    fi = new sio.FileInfo(gameDir + exeNames[i]);
                    if (!fi.Exists)
                        continue;

                    for (j = 0; j < vfsNames.Length; j++)
                        if (archName == vfsNames[i, j])
                            break;
                    if (j < vfsNames.Length)
                        break;
                }

                if (i == exeNames.Length)
                    throw new sio.FileNotFoundException("Supported exe not found");
                if (j == vfsNames.Length)
                    throw new sio.FileNotFoundException("Supported arch not found");

                exeName = exeNames[i];
                vfsPos = vfsPoses[i, j];
            }
        }

        private string gameDir;
        private string archPath;
        private string resDir;

        public CvmClass(string gameDir, string archPath, string resDir)
        {
            if (!sio.Directory.Exists(gameDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", gameDir));
            this.gameDir = misc.DICreateAndCheck(gameDir).ToString();
            this.archPath = this.gameDir + archPath;
            this.resDir = resDir;
        }

        public void Unpack()
        {
            sio.FileInfo fi;
            ISO9660 iso;

            fi = new sio.FileInfo(archPath);
            if (!fi.Exists)
                throw new sio.FileNotFoundException(string.Format("File '{0}' not found", fi.FullName));

            iso = new ISO9660(archPath, 3);
            iso.ExtractPartition(resDir + fi.Name);
        }

        public void Pack()
        {
            sio.FileInfo fi;
            ISO9660 iso;

            if (!sio.Directory.Exists(resDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", gameDir));
            fi = new sio.FileInfo(archPath);
            resDir = misc.DICreateAndCheck(resDir).ToString() + fi.Name;
            if (!sio.Directory.Exists(resDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", gameDir));
            resDir = misc.DICreateAndCheck(resDir).ToString();

            iso = new ISO9660(resDir);
            UpdateVfs(iso);
            iso.RebuildPartition(fi.FullName);
        }

        private void UpdateVfs(ISO9660 iso)
        {
            sio.FileStream fs;
            sio.BinaryWriter bw;
            PathTableRecord pth;
            VfsParams vfs;
            int i, j;

            vfs = new VfsParams(gameDir, archPath);

            fs = new sio.FileStream(gameDir + vfs.exeName, sio.FileMode.Open, sio.FileAccess.Write);
            bw = new sio.BinaryWriter(fs);

            fs.Position = vfs.vfsPos;
            for (i = 0; i < iso.partition.paths.Count; i++)
            {
                pth = iso.partition.paths[i];

                bw.Write((int)0);
                bw.Write((int)0);
                bw.Write(pth.files.Count + 1);
                bw.Write(pth.files.Count + 1);
                bw.Write(pth.hdr.locExtent);
                bw.Write(ste.ASCII.GetBytes("#DirLst#"));
                bw.Write((int)0);

                bw.Write(pth.files[0].hdr.datLenL);
                bw.Write((int)0);
                bw.Write(pth.hdr.locExtent);
                bw.Write((byte)0x2);
                bw.Write((byte)0);
                bw.Write(ste.ASCII.GetBytes(misc.GetZeroPaddedString(".", 0x20)));
                bw.Write((short)0);

                if (i > 0)
                    pth = iso.partition.paths[pth.hdr.parDirNum];
                bw.Write(pth.files[0].hdr.datLenL);
                bw.Write((int)0);
                bw.Write(pth.hdr.locExtent);
                bw.Write((byte)0x2);
                bw.Write((byte)0);
                bw.Write(ste.ASCII.GetBytes(misc.GetZeroPaddedString("..", 0x20)));
                bw.Write((short)0);

                pth = iso.partition.paths[i];
                for (j = 1; j < pth.files.Count; j++)
                {
                    bw.Write(pth.files[j].hdr.datLenL);
                    bw.Write((int)0);
                    bw.Write(pth.files[j].hdr.locL);
                    if (pth.files[j].isDirectory)
                        bw.Write((byte)0x2);
                    else
                        bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write(ste.ASCII.GetBytes(misc.GetZeroPaddedString(pth.files[j].name, 0x20)));
                    bw.Write((short)0);
                }
            }

            bw.Close();
            fs.Close();
        }
    }
}
