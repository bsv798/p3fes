using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace arch
{
    class Program
    {
        static int Main(string[] args)
        {
            Archiever ar;

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Debug version");
            Console.ForegroundColor = ConsoleColor.Gray;
            ar = new Archiever(@"e:\Trance\p3fes\res\");
            ar.Pack();
            //ar.Unpack();
            return 0;
#endif

#if !DEBUG
            try
            {
#endif
            if (args.Length > 1)
            {
                ar = new Archiever(args[1]);
                args[0] = args[0].ToLower();
                if (args[0] == "-e")
                    ar.Unpack();
                else
                    ar.Pack();
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
            Console.WriteLine("Usage:");
            Console.WriteLine("\tarch.exe path_to_search_for_archives");
            throw new ArgumentException("Parameters are not set");
        }
    }
}
