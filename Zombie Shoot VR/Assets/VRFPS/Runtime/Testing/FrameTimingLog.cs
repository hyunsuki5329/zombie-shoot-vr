using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace VRFPS
{
    public sealed class FrameTimingLog : IDisposable
    {
        BinaryWriter writer;
        ulong lastTimestamp;
        long accepted,duplicates,zeroTimestamp,disabled,empty,validCpuTotal,validGpu;
        public FrameTimingLog(string path,string metadata)
        {
            if(metadata==null)throw new ArgumentNullException(nameof(metadata));
            var bytes=Encoding.UTF8.GetBytes(metadata);
            if(bytes.Length>65536)throw new ArgumentException("Frame metadata exceeds 64 KiB.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            writer=new BinaryWriter(new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read,65536));
            try{writer.Write(Encoding.ASCII.GetBytes("VRFT0001"));writer.Write(bytes.Length);writer.Write(bytes);writer.Flush();}
            catch{writer.Dispose();writer=null;throw;}
        }
        static bool Positive(double value)=>value>0&&!double.IsNaN(value)&&!double.IsInfinity(value);
        public bool Append(FrameTiming timing,double elapsed,int retrievalFrame,int attempt,RunPhase phase)
        {
            if(writer==null)throw new ObjectDisposedException(nameof(FrameTimingLog));
            if(timing.frameStartTimestamp==0){zeroTimestamp++;return false;}
            if(timing.frameStartTimestamp<=lastTimestamp){duplicates++;return false;}
            uint valid=0;
            if(Positive(timing.cpuFrameTime))valid|=1;
            if(Positive(timing.cpuMainThreadFrameTime))valid|=2;
            if(Positive(timing.cpuRenderThreadFrameTime))valid|=4;
            if(timing.cpuMainThreadPresentWaitTime>=0&&!double.IsInfinity(timing.cpuMainThreadPresentWaitTime))valid|=8;
            if(Positive(timing.gpuFrameTime))valid|=16;
            writer.Write((byte)1);writer.Write(timing.frameStartTimestamp);writer.Write(elapsed);
            writer.Write(retrievalFrame);writer.Write(attempt);writer.Write((int)phase);writer.Write(valid);
            writer.Write(timing.cpuFrameTime);writer.Write(timing.cpuMainThreadFrameTime);
            writer.Write(timing.cpuRenderThreadFrameTime);writer.Write(timing.cpuMainThreadPresentWaitTime);writer.Write(timing.gpuFrameTime);
            lastTimestamp=timing.frameStartTimestamp;accepted++;
            if((valid&1)!=0)validCpuTotal++;if((valid&16)!=0)validGpu++;
            return true;
        }
        public void Unavailable(bool featureEnabled){if(featureEnabled)empty++;else disabled++;}
        public void Flush()=>writer?.Flush();
        public void Dispose()
        {
            if(writer==null)return;
            var current=writer;writer=null;
            try
            {
                current.Write((byte)2);current.Write(accepted);current.Write(duplicates);current.Write(zeroTimestamp);
                current.Write(disabled);current.Write(empty);current.Write(validCpuTotal);current.Write(validGpu);
            }
            finally{current.Dispose();}
        }
    }
}
