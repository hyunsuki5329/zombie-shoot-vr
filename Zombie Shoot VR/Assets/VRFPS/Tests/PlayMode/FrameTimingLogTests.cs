using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class FrameTimingLogTests
    {
        string path;
        [SetUp] public void Setup()=>path=Path.Combine(Application.temporaryCachePath,"vrfps-frames-"+Guid.NewGuid().ToString("N")+".vrft");
        [TearDown] public void Cleanup(){if(File.Exists(path))File.Delete(path);}
        [Test] public void UniqueFramesPreserveRawTimesAndMarkUnsupportedGpuWithoutCountingDuplicates()
        {
            using(var log=new FrameTimingLog(path,"{}"))
            {
                var frame=new FrameTiming{frameStartTimestamp=100,cpuFrameTime=14,cpuMainThreadFrameTime=3,
                    cpuRenderThreadFrameTime=2,cpuMainThreadPresentWaitTime=10,gpuFrameTime=0};
                Assert.That(log.Append(frame,1,10,1,RunPhase.Active),Is.True);
                Assert.That(log.Append(frame,2,11,1,RunPhase.Active),Is.False);
                frame.frameStartTimestamp=0;Assert.That(log.Append(frame,3,12,1,RunPhase.Active),Is.False);
                frame.frameStartTimestamp=101;frame.gpuFrameTime=8;frame.cpuFrameTime=double.NaN;
                Assert.That(log.Append(frame,4,13,2,RunPhase.Paused),Is.True);
            }
            using(var reader=new BinaryReader(File.OpenRead(path)))
            {
                Assert.That(Encoding.ASCII.GetString(reader.ReadBytes(8)),Is.EqualTo("VRFT0001"));
                Assert.That(Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())),Is.EqualTo("{}"));
                Assert.That(reader.ReadByte(),Is.EqualTo(1));Assert.That(reader.ReadUInt64(),Is.EqualTo(100));
                Assert.That(reader.ReadDouble(),Is.EqualTo(1));Assert.That(reader.ReadInt32(),Is.EqualTo(10));
                Assert.That(reader.ReadInt32(),Is.EqualTo(1));Assert.That(reader.ReadInt32(),Is.EqualTo((int)RunPhase.Active));
                Assert.That(reader.ReadUInt32(),Is.EqualTo(15),"GPU zero is unavailable, not fast.");
                Assert.That(reader.ReadDouble(),Is.EqualTo(14));Assert.That(reader.ReadDouble(),Is.EqualTo(3));
                Assert.That(reader.ReadDouble(),Is.EqualTo(2));Assert.That(reader.ReadDouble(),Is.EqualTo(10));
                Assert.That(reader.ReadDouble(),Is.Zero,"Raw unsupported value is retained with validity mask.");
                Assert.That(reader.ReadByte(),Is.EqualTo(1));reader.ReadBytes(28);
                Assert.That(reader.ReadUInt32(),Is.EqualTo(30),"NaN CPU total is invalid; GPU is valid.");
                Assert.That(double.IsNaN(reader.ReadDouble()),Is.True);reader.ReadBytes(32);
                Assert.That(reader.ReadByte(),Is.EqualTo(2));Assert.That(reader.ReadInt64(),Is.EqualTo(2));
                Assert.That(reader.ReadInt64(),Is.EqualTo(1));Assert.That(reader.ReadInt64(),Is.EqualTo(1));
            }
        }
        [Test] public void DisabledOrEmptyPollsDoNotCreateZeroMillisecondFramesOrOverwriteLogs()
        {
            using(var log=new FrameTimingLog(path,"{}")){log.Unavailable(false);log.Unavailable(true);}
            var bytes=File.ReadAllBytes(path);
            Assert.Throws<IOException>(()=>{using(var log=new FrameTimingLog(path,"{}")){};});
            Assert.That(File.ReadAllBytes(path),Is.EqualTo(bytes));
            using(var reader=new BinaryReader(File.OpenRead(path)))
            {
                reader.ReadBytes(8);reader.ReadBytes(reader.ReadInt32());
                Assert.That(reader.ReadByte(),Is.EqualTo(2));Assert.That(reader.ReadInt64(),Is.Zero);
                reader.ReadInt64();reader.ReadInt64();
                Assert.That(reader.ReadInt64(),Is.EqualTo(1));Assert.That(reader.ReadInt64(),Is.EqualTo(1));
            }
        }
    }
}
