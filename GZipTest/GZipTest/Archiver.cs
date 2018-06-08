using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Collections;

namespace GZipTest
{
    abstract class Archiver
    {
        protected static bool cancel = false;
        protected string inputFile;
        protected string outputFile;
        protected object readlock;
        protected object writelock;
        protected static int threadCount = (Environment.ProcessorCount - 2) > 0 ? Environment.ProcessorCount - 1 : 1;
        protected ManualResetEvent[] exitCompressionThread;
        protected FileStream outputStream;
        protected GZipStream gzip;
        protected long writepos = 0;
        protected long readpos = 0;
        protected int bufferSize = 4096;
        protected Queue queue;

        public Archiver(string input, string output)
        {
            inputFile = input;
            outputFile = output;
            readlock = new object();
            writelock = new object();
        }

        public void Cancel()
        {
            cancel = true;
        }
        abstract public void Do();
    }
}
