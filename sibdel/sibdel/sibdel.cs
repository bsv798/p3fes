using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ssc = System.Security.Cryptography;
using sio = System.IO;

namespace sibdel
{
    public class FileList
    {
        private List<FileListItem> list;

        public FileList(string path)
        {
            string[] files;

            files = sio.Directory.GetFiles(path, "*", sio.SearchOption.AllDirectories);
            list = new List<FileListItem>();

            foreach (var file in files)
                list.Add(new FileListItem(file));
            list.Sort(CompareFileListItemsMd5);
        }

        private static int CompareFileListItemsMd5(FileListItem a, FileListItem b)
        {
            for (int i = 0; i < FileListItem.hashLength; i++)
                if (a.md5[i] > b.md5[i])
                    return 1;
                else if (a.md5[i] < b.md5[i])
                    return -1;

            return a.fileName.CompareTo(b.fileName);
        }

        public void DeleteSiblings()
        {
            FileListItem item;
            int i, j;

            Console.WriteLine("Total file count: {0}", list.Count);

            for (i = 0; i < list.Count; i++)
            {
                item = list[i];
                for (j = i + 1; j < list.Count; j++)
                    if (list[j] == item)
                    {
                        sio.File.Delete(list[j].fileName);
                        list.RemoveAt(j--);
                    }
            }

            Console.WriteLine("Result file count: {0}", list.Count);
        }
    }

    public class FileListItem
    {
        public static readonly int hashLength = 0x10;
        private static ssc.MD5 hasher = ssc.MD5.Create();
        public string fileName;
        public byte[] md5;

        public FileListItem(string fileName)
        {
            this.fileName = fileName;
            md5 = hasher.ComputeHash(sio.File.ReadAllBytes(fileName));
        }

        public static bool operator ==(FileListItem a, FileListItem b)
        {
            for (int i = 0; i < hashLength; i++)
                if (a.md5[i] != b.md5[i])
                    return false;

            return true;
        }

        public static bool operator !=(FileListItem a, FileListItem b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }

}
