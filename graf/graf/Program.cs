using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Persona3Graphics
{
    class Program
    {
        static int Main(string[] args)
        {
            ImgListWorker iml;

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Debug version");
            Console.ForegroundColor = ConsoleColor.Gray;

            iml = new ImgListWorker(@"e:\Trance\p3fes\res\", @"e:\Trance\p3fes\res\graf\");
            iml.Extract();
            //iml.Import();
            //iml.Update();

            //TmxClass tmx;
            //tmx = new TmxClass(@"e:\02ac.tmx "); //args[0]
            //tmx.ToPng(@"e:\02ac.png ");

            return 0;
#endif

#if !DEBUG
            try
            {
#endif
                Console.WriteLine("{0} by bsv798. version: {1}", misc.programDescription, misc.programVersion);
                if (args.Length > 2)
                {
                    iml = new ImgListWorker(args[1], args[2]);
                    args[0] = args[0].ToLower();
                    if (args[0] == "-e")
                        iml.Extract();
                    else if (args[0] == "-i")
                        iml.Import();
                    else
                        iml.Update();
                }
                else
                    ShowUsage();
                return 0;
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.Gray;

                return -1;
            }
#endif
        }


        static void ShowUsage()
        {
            Console.WriteLine("Converts raw/png images to png/raw images:");
            Console.WriteLine("Usage:");
            Console.WriteLine("\tgraf.exe command path_with_raw_images path_with_png");
            Console.WriteLine("Commands are:");
            Console.WriteLine("\t-e extract");
            Console.WriteLine("\t-i import");
            Console.WriteLine("\t-u update");

            throw new ArgumentException("Parameters are not set");
        }
    }
}
