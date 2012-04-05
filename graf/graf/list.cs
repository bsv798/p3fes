using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace Persona3Graphics
{
    public class ImgListWorker
    {
        private const string ImageListName = "$image.list";
        private ImgList gList, rList;
        private string imgDir;
        private string extractDir;
        private sio.StreamReader sr;
        private sio.StreamWriter sw;
        private int globalCounter;

        private void ExtractImg(string parentDir)
        {
            string[] files;
            string[] dirs;
            ImgListItem item;
            RawImgClass img;
            RawImgTypes type;

            img = null;

            files = sio.Directory.GetFiles(parentDir, "*.tmx");
            if (files.Length > 0)
                Console.WriteLine("Extracting directory '{0}'", parentDir);
            foreach (var file in files)
            {
                switch (type = RawImgClass.GetImgType(file))
                {
                    case RawImgTypes.p3tmx:
                        img = new TmxClass(file);
                        break;
                }
                if (type != RawImgTypes.unknown)
                {
                    item = new ImgListItem(imgDir, file, img);
                    item.type = (int)type;
                    if (!gList.ExistsMd5(item))
                    {
                        item.pngAlias = string.Format("{0:x4}.{1}", globalCounter++, item.pngAlias);
                        img.ToPng(extractDir + item.pngAlias);
                        item.md5png = img.png.md5string;
                        gList.AddUnique(item);
                    }
                    else
                    {
                        item.md5png = gList.FindMd5(item).md5png;
                        item.pngAlias = gList.FindMd5(item).pngAlias;
                        gList.Add(item);
                    }
                }
            }

            dirs = sio.Directory.GetDirectories(parentDir);
            foreach (var dir in dirs)
            {
                ExtractImg(dir);
            }
        }

        public void Extract()
        {
            if (!sio.Directory.Exists(imgDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", imgDir));
            imgDir = misc.DirCreateAndCheck(imgDir);
            extractDir = misc.DirCreateAndCheck(extractDir);

            gList = new ImgList();

            globalCounter = 0;
            ExtractImg(imgDir);

            if (gList.Count > 0)
            {
                for (int i = 0; i < gList.Count; i++)
                    gList[i].SetMd5(gList[i].md5png);
                gList.Sort();

                SaveList(gList);

                Console.WriteLine("Export done");
            }
            else
                Console.WriteLine("Nothing to export");
        }

        private void ReadList()
        {
            string str;

            sr = new sio.StreamReader(extractDir + ImageListName, ste.UTF8);
            rList = new ImgList();

            while (!string.IsNullOrEmpty(str = sr.ReadLine()))
                rList.AddUnsorted(new ImgListItem(str, true));

            sr.Close();

            rList.Sort();
        }

        public ImgListWorker(string imgDir, string extractDir)
        {
            this.extractDir = extractDir;
            this.imgDir = imgDir;
        }

        public void Import()
        {
            if (!sio.Directory.Exists(extractDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", extractDir));
            extractDir = misc.DirCreateAndCheck(extractDir);
            if (!sio.File.Exists(extractDir + ImageListName))
                throw new sio.FileNotFoundException(string.Format("File '{0}' not found", extractDir + ImageListName));
            imgDir = misc.DirCreateAndCheck(imgDir);

            ReadList();
            if (rList.Count > 0)
            {
                ImportImg();
                Console.WriteLine("Import done");
            }
            else
                Console.WriteLine("Nothing to import");
        }

        private void ImportImg()
        {
            RawImgClass img;
            ImgListItem item;
            string infoLine;
            int i, j;

            for (i = 0; i < rList.Count; i++)
                if (!sio.File.Exists(extractDir + rList[i].pngAlias))
                    throw new sio.FileNotFoundException(string.Format("File '{0}' not found. May be you should update?", extractDir + rList[i].pngAlias));

            img = null;
            infoLine = "";
            for (i = 0; i < rList.Count; i++)
            {
                item = rList[i];
                switch ((RawImgTypes)item.type)
                {
                    case RawImgTypes.p3tmx:
                        img = TmxClass.FromPng(extractDir + item.pngAlias);
                        img.name = item.name;
                        img.Save(imgDir + item.fullName);
                        break;
                    case RawImgTypes.p3ps2:
                        break;
                    default:
                        throw new ArgumentException(string.Format("Unknown image type: {0}", item.md5string));
                }
                for (j = i + 1; j < rList.Count; j++)
                    if (rList[j] == item)
                    {
                        img.Save(imgDir + rList[j].fullName);
                        i++;
                    }
                    else
                        break;
                if (i % 10 == 0)
                    Console.Write(infoLine = string.Format("\rImporting item {0} of {1}", i, rList.Count));
            }
            Console.Write("\r{0}\r", new string(' ', infoLine.Length));
        }

        public void Update()
        {
            if (!sio.Directory.Exists(extractDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", extractDir));
            if (!sio.Directory.Exists(imgDir))
                throw new sio.DirectoryNotFoundException(string.Format("Directory '{0}' not found", imgDir));
            extractDir = misc.DirCreateAndCheck(extractDir);
            imgDir = misc.DirCreateAndCheck(imgDir);

            ReadList();
            CheckDuplicates();
            CheckRenamedFiles();
            CheckChangedFiles();
            CheckDeletedFiles();
            SaveList(rList);

            Console.WriteLine("Update done");
        }


        private void CheckDuplicates()
        {
            ImgListItem item;
            int i, j;

            for (i = 0; i < rList.Count; i++)
            {
                item = rList[i];
                for (j = i + 1; j < rList.Count; j++)
                    if (rList[j] == item)
                    {
                        if (rList[j].pngAlias.CompareTo(item.pngAlias) != 0)
                        {
                            if (sio.File.Exists(extractDir + rList[j].pngAlias))
                                sio.File.Delete(extractDir + rList[j].pngAlias);
                            Console.WriteLine("File '{0}' is equal to '{1}'", rList[j].pngAlias, item.pngAlias);
                            rList[j].pngAlias = item.pngAlias;
                            i++;
                        }
                    }
                    else
                        break;
            }
        }

        private void CheckDeletedFiles()
        {
            int i;

            for (i = 0; i < rList.Count; i++)
                if (!sio.File.Exists(extractDir + rList[i].pngAlias))
                {
                    Console.WriteLine("Line '{0}' was removed", rList[i].pngAlias);
                    rList.RemoveItemAndSimilar(rList[i]);
                    i--;
                }
        }

        private void CheckRenamedFiles()
        {
            ImgListItem newItem, oldItem;
            string[] files;

            files = sio.Directory.GetFiles(extractDir, "*.png", sio.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                newItem = new ImgListItem(file);
                if (rList.ExistsMd5(newItem))
                {
                    oldItem = rList.FindMd5(newItem);
                    newItem.CopyValues(oldItem);
                    newItem.pngAlias = file.Remove(0, extractDir.Length);
                    if (newItem.pngAlias.Length == oldItem.pngAlias.Length)
                        if (newItem.pngAlias.ToLower() == oldItem.pngAlias.ToLower())
                            continue;

                    if (sio.File.Exists(extractDir + oldItem.pngAlias))
                        sio.File.Delete(extractDir + newItem.pngAlias);
                    else
                    {
                        Console.WriteLine("File '{0}' was renamed to '{1}'", oldItem.pngAlias, newItem.pngAlias);
                        rList.UpdateItemAndSimilar(newItem);
                    }
                }
            }
        }

        private void CheckChangedFiles()
        {
            ImgListItem item;
            string[] files;
            string fileName;
            int i;

            files = sio.Directory.GetFiles(extractDir, "*.png", sio.SearchOption.AllDirectories);

            foreach (var file in files)
            {
                item = new ImgListItem(file);
                if (!rList.ExistsMd5(item))
                {
                    fileName = file.Remove(0, extractDir.Length);
                    for (i = 0; i < rList.Count; i++)
                        if (rList[i].pngAlias.Length == fileName.Length)
                            if (rList[i].pngAlias == fileName)
                            {
                                rList[i].SetMd5(item.md5string);
                                rList[i].md5png = item.md5string;
                                rList.UpdateItemAndSimilar(rList[i]);
                                Console.WriteLine("File '{0}' was changed", rList[i].pngAlias);
                            }
                }
            }
        }

        public void SaveList(ImgList list)
        {
            ImgListItem item;

            list.Sort();

            sw = new sio.StreamWriter(extractDir + ImageListName, false, ste.UTF8);
            for (int i = 0; i < list.Count; i++)
            {
                item = list[i];
                sw.WriteLine("type={0},depth={1},width={2},height={3},name={4},png_alias={5},rel_name={6},hash={7}", new object[] { item.type, item.bitCount, item.width, item.height, item.name, item.pngAlias, item.fullName, item.md5png });
            }
            sw.Close();
        }
    }

    public class ImgList
    {
        private List<ImgListItem> list;
        public int Count { get { return list.Count; } }

        public ImgList()
        {
            list = new List<ImgListItem>();
        }

        public ImgListItem this[int idx]
        {
            get
            {
                return list[idx];
            }

            set
            {
                list[idx] = value;
            }
        }

        public void Add(ImgListItem item)
        {
            AddUnsorted(item);
            list.Sort(CompareImgListItemsMd5Name);
        }

        public void AddUnsorted(ImgListItem item)
        {
            list.Add(item);
        }

        public void AddUnique(ImgListItem item)
        {
            if (!ExistsMd5(item))
                Add(item);
        }

        public void RemoveItemAndSimilar(ImgListItem item)
        {
            int i, j;

            for (i = 0; i < Count; i++)
                if (this[i] == item)
                    break;

            if (i == Count)
                return;

            list.RemoveAt(i);

            for (j = i; j < Count; j++)
                if (this[j] == item)
                    list.RemoveAt(j);
                else
                    break;
        }

        public void UpdateItemAndSimilar(ImgListItem item)
        {
            int i, j;

            for (i = 0; i < Count; i++)
                if (this[i] == item)
                    break;

            for (j = i; j < Count; j++)
                if (this[j] == item)
                    this[j].CopyValues(item);
                else
                    break;
        }

        public bool ExistsMd5(ImgListItem item)
        {
            foreach (var li in list)
                if (item == li)
                    return true;
            return false;
        }

        public ImgListItem FindMd5(ImgListItem item)
        {
            foreach (var li in list)
                if (item == li)
                    return li;
            return null;
        }

        //public int GetMd5Index(ImgListItem item)
        //{
        //    int step;
        //    int pos;
        //    int prevPos;
        //    int dir;

        //    if (Count == 0)
        //        return -1;

        //    pos = Count >> 1;
        //    step = (pos > 0) ? pos : 1;

        //    prevPos = -1;
        //    while (true)
        //    {
        //        if (prevPos == pos)
        //            return -1;
        //        dir = CompareImgListItemsMd5(item, list[pos]);
        //        if (dir == 0)
        //            break;

        //        prevPos = pos;
        //        if (step > 1)
        //            step >>= 1;
        //        if (dir < 0)
        //            pos -= step;
        //        else
        //            pos += step;

        //        if ((pos < 0) || (pos >= Count))
        //            return -1;
        //    }

        //    if (pos == 0)
        //        return 0;
        //    else
        //    {
        //        while (CompareImgListItemsMd5(item, list[--pos]) == 0) ;
        //        return ++pos;
        //    }
        //}

        public void Sort()
        {
            list.Sort(CompareImgListItemsMd5Name);
        }

        private static int CompareImgListItemsMd5Name(ImgListItem a, ImgListItem b)
        {
            for (int i = 0; i < ImgListItem.hashLength; i++)
                if (a.md5bytes[i] > b.md5bytes[i])
                    return 1;
                else if (a.md5bytes[i] < b.md5bytes[i])
                    return -1;

            return a.fullName.CompareTo(b.fullName);
        }

        private static int CompareImgListItemsMd5(ImgListItem a, ImgListItem b)
        {
            for (int i = 0; i < ImgListItem.hashLength; i++)
                if (a.md5bytes[i] > b.md5bytes[i])
                    return 1;
                else if (a.md5bytes[i] < b.md5bytes[i])
                    return -1;

            return 0;
        }
    }

    public class ImgListItem : Md5Class
    {
        public int type;
        public int bitCount;
        public int width, height;
        public string name;
        public string pngAlias;
        public string fullName;
        public string md5png;

        public ImgListItem(string imgDir, string fileName, RawImgClass img)
        {
            type = (int)img.Type;
            bitCount = img.BitCount;
            width = img.Width;
            height = img.Height;
            name = img.name;
            if (name.Length == 0)
                pngAlias = sio.Path.GetFileNameWithoutExtension(fileName) + ".png";
            else
                pngAlias = name + ".png";
            fullName = fileName.Replace(imgDir, "");
            img.CalcMd5();
            md5bytes = img.md5bytes;
        }

        public ImgListItem(string paramStr, bool notUsed)
        {
            type = Convert.ToInt32(misc.GetValue(paramStr, "type"));
            bitCount = Convert.ToInt32(misc.GetValue(paramStr, "depth"));
            width = Convert.ToInt32(misc.GetValue(paramStr, "width"));
            height = Convert.ToInt32(misc.GetValue(paramStr, "height"));
            name = misc.GetValue(paramStr, "name");
            pngAlias = misc.GetValue(paramStr, "png_alias");
            fullName = misc.GetValue(paramStr, "rel_name");
            SetMd5(misc.GetValue(paramStr, "hash"));
            md5png = md5string;
        }

        public ImgListItem(string fileName)
        {
            pngAlias = sio.Path.GetFileName(fileName);
            CalcMd5(sio.File.ReadAllBytes(fileName));
        }

        public void CopyValues(ImgListItem item)
        {
            type = item.type;
            bitCount = item.bitCount;
            width = item.width;
            height = item.height;
            name = item.name;
            pngAlias = item.pngAlias;
            //fullName = item.fullName;
            SetMd5(item.md5string);
            md5png = item.md5string;
        }

        public static bool operator ==(ImgListItem a, ImgListItem b)
        {
            for (int i = 0; i < hashLength; i++)
                if (a.md5bytes[i] != b.md5bytes[i])
                    return false;
            return true;
        }

        public static bool operator !=(ImgListItem a, ImgListItem b)
        {
            for (int i = 0; i < hashLength; i++)
                if (a.md5bytes[i] == b.md5bytes[i])
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            try
            {
                return (bool)(this == (ImgListItem)obj);
            }
            catch
            {
                return false;
            }

        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", pngAlias, md5string);
        }
    }
}
