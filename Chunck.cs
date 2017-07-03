using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiThreadingGzipCompressor
{
    public  class Chunck
    {
        public byte[] Data;

        public ChunckState State;
    }

    public enum ChunckState
    {
        NoCompress = 0,
        InProgress = 1,
        Compressed = 2
    }
}
