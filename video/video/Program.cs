using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace video
{
    class Program
    {
        static int Main(string[] args)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Debug version");
            Console.ForegroundColor = ConsoleColor.Gray;
#endif

            try
            {
                if (args.Length < 2)
                    ShowUsage();
                else
                    SFD.Demux(args[0], args[1]);

                return 0;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.Gray;

                return -1;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Persona 3 video demuxer by bsv798");
            Console.WriteLine("video.exe <sfd_file> <extract_filename>");
            Console.WriteLine("To extract_filename respective extension for audio and video will be added");
        }
    }
}
