using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace text
{
    class Program
    {
        static void Main(string[] args)
        {
            ArchiveManager am;
            //Streams.reader = StreamReaders.OpenMemoryStream(@"e:\Trance\p3fes\res\data.cvm\EVENT\E100\0004.e104_001.msg ", System.Text.Encoding.Default);
            //MsgTextArchive mta = new MsgTextArchive();
            //mta.LoadArchive();

            am = new ArchiveManager();
            am.LoadTable(@"e:\Trance\p3fes\jinn\Копия Main.tbl");
            string[] files = "*.bmd;*.bf;*.msg".Split(';').SelectMany(filter => System.IO.Directory.GetFiles(@"e:\Trance\p3fes\res\", filter, System.IO.SearchOption.AllDirectories)).ToArray();
            //string[] files = "DUNGEONAT00.BF".Split(';').SelectMany(filter => System.IO.Directory.GetFiles(@"E:\Trance\p3fes\res\", filter, System.IO.SearchOption.AllDirectories)).ToArray();
            //string[] files = "*.bmd;*.bf;*.msg".Split(';').SelectMany(filter => System.IO.Directory.GetFiles(@"E:\Trance\p3fes\res\", filter, System.IO.SearchOption.AllDirectories)).ToArray();
            //string[] files = System.IO.Directory.GetFiles(@"E:\Trance\p3fes\res\", "*.msg", System.IO.SearchOption.AllDirectories);
            //string[] files = "*.txt".Split(';').SelectMany(filter => System.IO.Directory.GetFiles(@"e:\Trance\p3fes\txt\DATA_TEXT\", filter, System.IO.SearchOption.AllDirectories)).ToArray();
            foreach (var file in files)
            {
                Streams.reader = StreamReaders.OpenMemoryStream(file);
                am.LoadArchive(file);
                Streams.reader.CloseMemoryStream();

                //Streams.reader = StreamReaders.OpenTextStream(@"e:\Trance\p3fes\txt\DATA_TEXT\0000040F.txt", System.Text.Encoding.UTF8);
                //am.LoadJinnText(file);
                //Streams.reader.CloseTextStream();

                //Streams.writer = StreamWriters.OpenMemoryStream();
                //am.archs[am.archs.Count - 1].SaveArchive();
                //Streams.writer.SaveMemoryStreamToFile(@"e:\test.bmd");
                //Streams.writer.CloseMemoryStream();
            }
            files = "*.txt".Split(';').SelectMany(filter => System.IO.Directory.GetFiles(@"e:\Trance\p3fes\txt\DATA_TEXT\", filter, System.IO.SearchOption.AllDirectories)).ToArray();
            Streams.writer = StreamWriters.OpenTextStream(@"e:\Trance\p3fes\txt\import.txt");
            foreach (var file in files)
            {
                Streams.reader = StreamReaders.OpenTextStream(file, System.Text.Encoding.UTF8);
                am.LoadJinnText(file);
                Streams.reader.CloseTextStream();
            }
            Streams.writer.CloseTextStream();
            //NPCCOMU.BF - глюк разрабов? с фразой Oh, I-I see... Sorry... (указатель ACDE)
        }
    }
}
