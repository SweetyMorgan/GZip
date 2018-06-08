using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Collections;

namespace GZipTest
{
    class Decompressor : Archiver
    {
        public Decompressor(string input, string output) : base(input, output)
        {

        }
        int currentThreads = 0;
        public override void Do()
        {
            Console.CursorTop += 2;
            byte[] tmp;
            queue = new Queue();
            Thread[] decompressors = new Thread[threadCount];
            exitCompressionThread = new ManualResetEvent[threadCount];
            using (outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < threadCount; i++)
                {
                    decompressors[i] = new Thread(Decompress);
                    exitCompressionThread[i] = new ManualResetEvent(false);
                    currentThreads += 1;
                    decompressors[i].Start(i);
                }
                while ((currentThreads > 0) || (queue.Count > 0))
                {
                    if (queue.Count > 0)
                    {
                        lock (writelock)
                            tmp = (byte[])queue.Dequeue();
                        outputStream.Write(tmp, 0, tmp.Length);
                    }
                }
                WaitHandle.WaitAll(exitCompressionThread);
            }
        }
        public void Decompress(object number)
        {
            int readbytes;
            byte[] buffer = new byte[bufferSize];
            int thbufferSize = bufferSize;
            long curPos = 0;
            using (FileStream input = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                using (GZipStream gz = new GZipStream(input, CompressionMode.Decompress))
                {
                    while (((readbytes = gz.Read(buffer, 0, bufferSize)) > 0) && (!cancel))
                    {
                        
                        lock (readlock)
                        {
                            curPos += bufferSize;
                        }
                        while ((curPos - bufferSize < writepos)&&(readbytes != 0))
                        {
                            buffer = new byte[bufferSize];
                            if ((readbytes = gz.Read(buffer, 0, bufferSize)) > 0)
                                lock (readlock)
                                {
                                    curPos += bufferSize;
                                }
                        }
                        if (readbytes < bufferSize)
                            Array.Resize<byte>(ref buffer, readbytes);
                        lock (writelock)
                        {
                            while (writepos <= curPos - bufferSize)
                            {
                                if (writepos == curPos - bufferSize)
                                {
                                    byte[] tmp = new byte[buffer.Length];
                                    buffer.CopyTo(tmp, 0);
                                    queue.Enqueue(tmp);
                                    writepos += bufferSize;
                                    if (!cancel)
                                    {
                                        Console.SetCursorPosition(0, Console.CursorTop);
                                        Console.Write("Unpackage: " + Math.Round((double)(input.Position / (float)input.Length) * 100) + "%");
                                    }

                                    break;
                                }
                            }
                        
                        }
                        buffer = new byte[bufferSize];
                    }
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
