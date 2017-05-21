using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sibdel
{
    class Program
    {
        static int Main(string[] args)
        {
            FileList iml;

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Debug version");
            Console.ForegroundColor = ConsoleColor.Gray;

            iml = new FileList(@"e:\Temp\CAMP\test");
            iml.DeleteSiblings();

            return 0;
#endif

#if !DEBUG
            try
            {
#endif
            Console.WriteLine("{0} by bsv798. version: {1}", misc.programDescription, misc.programVersion);
            if (args.Length > 0)
            {
                iml = new FileList(args[0]);
                iml.DeleteSiblings();
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
            Console.WriteLine("\tsibdel.exe path");

            throw new ArgumentException("Parameters are not set");
        }
    }
}
