using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Collections;

namespace GZipTest
{
    class Compressor : Archiver
    {
        public Compressor(string input, string output) : base(input, output)
        {
            
        }

        int currentThreads = 0;
        public override void Do()
        {
            Console.CursorTop += 2;
            byte[] tmp;
            queue = new Queue();
            FileInfo f = new FileInfo(inputFile);
            long write = 0;
            Thread[] compressors = new Thread[threadCount];
            exitCompressionThread = new ManualResetEvent[threadCount];
            using (outputStream = new FileStream(outputFile + ".gz", FileMode.Create, FileAccess.Write))
            {
                using (gzip = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    for (int i = 0; i < threadCount; i++)
                    {
                        compressors[i] = new Thread(Read);
                        exitCompressionThread[i] = new ManualResetEvent(false);
                        currentThreads += 1;
                        compressors[i].Start(i);
                    }
                    while ((currentThreads > 0) || (queue.Count > 0))
                    {
                        if (queue.Count > 0)
                        {
                            lock (writelock)
                                tmp = (byte[])queue.Dequeue();
                            gzip.Write(tmp, 0, tmp.Length);
                            write += tmp.Length;
                            if (!cancel)
                            {
                                Console.SetCursorPosition(0, Console.CursorTop);
                                Console.Write("Package: " + Math.Round((double)(write / (float)f.Length) * 100) + "%");
                            }
                        }
                    }
                    WaitHandle.WaitAll(exitCompressionThread);
                }
            }
        }

        public void Read(object number)
        {
            int readbytes;
            long curPos = 0;
            byte[] buffer = new byte[bufferSize];
            using (FileStream input = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                while (((readbytes = input.Read(buffer, 0, bufferSize)) > 0) && (!cancel))
                {
                    if (readbytes < bufferSize)
                        Array.Resize<byte>(ref buffer, readbytes);
                    lock (readlock)
                    {
                        curPos += bufferSize;
                    }
                    while ((curPos - bufferSize < writepos)&&(writepos < input.Length))
                    {
                        if ((readbytes = input.Read(buffer, 0, bufferSize)) > 0)
                            lock (readlock)
                            {
                                curPos += bufferSize;
                            }
                    }
                    while (writepos <= curPos - bufferSize)
                    {
                        if (writepos == curPos - bufferSize)
                        {
                            byte[] tmp = new byte[buffer.Length];
                            buffer.CopyTo(tmp, 0);
                            while (queue.Count > 5000)
                            {
                                Thread.Sleep(1000);
                            }
                            lock (writelock)
                            {
                                queue.Enqueue(tmp);
                                writepos += bufferSize;
                            }
                            break;
                        }
                    }
                    buffer = new byte[bufferSize];
                }
            }
            lock (readlock)
            {
                currentThreads -= 1;
            }
            ManualResetEvent exit = exitCompressionThread[(int)number];
            exit.Set();
        }
    }
}
