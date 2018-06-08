using System;
using System.IO;

namespace GZipTest
{
    class Program
    {
        static Archiver Arch;
        static int Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(_cancelHandler);
            Parse(args);
            if (args[0].ToLower() == "compress")
            {
                Arch = new Compressor(args[1], args[2]);
            }
            else
            {
                Arch = new Decompressor(args[1], args[2]);
            }
            try
            {
                Arch.Do();
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        private static void Parse(string[] args)
        {
            if (args.Length < 3)
                throw new ArgumentException("Number of arguments must be 3!");
            if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress")
            {
                throw new ArgumentException("Command line must include 'compress' or 'decompress'!");
            }
            if (!File.Exists(args[1]))
            {
                throw new ArgumentException("File " + args[1] + " doesn't exist!");
            }
            if (args[0].ToLower() == "compress" && ((args[1].Substring(args[1].Length - 3)) == ".gz"))
            {
                throw new ArgumentException("Can't compress the archive " + args[1] + "!");
            }
            if (args[0].ToLower() == "decompress" && ((args[2].Substring(args[2].Length - 3)) == ".gz"))
            {
                throw new ArgumentException("Can't decompress to archive!");
            }
        }

        static void _cancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("Program canceled by user");
            args.Cancel = true;
            Arch.Cancel();
        }
    }
}
