using System;
using System.Collections.Generic;
using System.Diagnostics;
using WhiteMagic;
using WhiteMagic.WinAPI;

namespace CascBP
{
    public class CascBP
    {
        protected ProcessDebugger pd = null;
        protected Process process = null;
        protected int build = 0;

        public CascBP(Process process)
        {
            this.process = process;
            build = process.MainModule.FileVersionInfo.FilePrivatePart;
        }

        public void ForceBuild(int build)
        {
            this.build = build;
        }

        class OffsData
        {
            public OffsData(int cascOffs1, int cascOffs2, int portraitOffset = 0)
            {
                CascOffs1 = cascOffs1;
                CascOffs2 = cascOffs2;
            }

            public int CascOffs1 { get; private set; }
            public int CascOffs2 { get; private set; }
        }

        Dictionary<int, OffsData> BuildDatas = new Dictionary<int, OffsData>()
        {
            // .text:004913D1 74 6B                             jz      short loc_49143E
            // .text:004912D2 0F 84 90 00 00 00                 jz      loc_491368
            { 22293, new OffsData(0x004913D1 - 0x400000, 0x004912D2 - 0x400000) },
            // .text:004907AE 74 64                             jz      short loc_490814
            // .text:004906B8 0F 84 8A 00 00 00                 jz      loc_490748
            { 22423, new OffsData(0x004907AE - 0x400000, 0x004906B8 - 0x400000) },
            // .text:004908EB 74 64                             jz      short loc_490951
            // .text:004907F5 0F 84 8A 00 00 00                 jz      loc_490885
            { 22498, new OffsData(0x004908EB - 0x400000, 0x004907F5 - 0x400000) },
            // .text:00490930 74 64                             jz      short loc_490996
            // .text:0049083A 0F 84 8A 00 00 00                 jz      loc_4908CA
            { 22522, new OffsData(0x00490930 - 0x400000, 0x0049083A - 0x400000) },
            // .text:00490905 74 64                             jz      short loc_49096B
            // .text:0049080F 0F 84 8A 00 00 00                 jz      loc_49089F
            { 22566, new OffsData(0x00490905 - 0x400000, 0x0049080F - 0x400000) },
            // .text:004909E4 74 64                             jz      short loc_490A4A
            // .text:004908EE 0F 84 8A 00 00 00                 jz      loc_49097E
            { 22594, new OffsData(0x004909E4 - 0x400000, 0x004908EE - 0x400000) },
        };

        class CascBreakpoint1 : WowBreakpoint
        {
            protected int JumpOffs { get; private set; }
            public CascBreakpoint1(int offs)
                : base(offs)
            {
            }

            public override bool HandleException(ref CONTEXT ctx, ProcessDebugger pd)
            {
                //Console.WriteLine(pd.ReadASCIIString(pd.ReadPointer(new IntPtr(ctx.Ebp + 8))));
                ctx.Eip += 2;
                return true;
            }
        }

        class CascBreakpoint2 : WowBreakpoint
        {
            protected int JumpOffs { get; private set; }
            public CascBreakpoint2(int offs)
                : base(offs)
            {
            }

            public override bool HandleException(ref CONTEXT ctx, ProcessDebugger pd)
            {
                ctx.Eip += 6;
                return true;
            }
        }

        class TestBp : WowBreakpoint
        {
            // Script_FocusUnit
            // .text:00D48CD4 83 C4 10                          add     esp, 10h
            public TestBp()
                : base(0x00D48CD4 - 0x400000)
            {
            }
            public override bool HandleException(ref CONTEXT ctx, ProcessDebugger pd)
            {
                ctx.Esp += 16;
                ctx.Eip += 3;
                Console.WriteLine("Triggered");

                try
                {
                    var ptr = new IntPtr(ctx.Eax);
                    if (ptr != IntPtr.Zero)
                    {
                        ptr = pd.ReadPointer(ptr.Add(292));
                        if (ptr != IntPtr.Zero)
                        {
                            Console.WriteLine("OK got values array");
                            Console.WriteLine("Display Id: {0}", pd.ReadInt(ptr.Add(0x5C * 4)));
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Caught exception");
                }

                return true;
            }
        }

        public void Start()
        {
            try
            {
                pd = new ProcessDebugger(process.Id);
                var breakpoints = new List<HardwareBreakPoint>();
                var data = BuildDatas.ContainsKey(build) ? BuildDatas[build] : null;

                if (data != null)
                {
                    breakpoints.Add(new CascBreakpoint1(data.CascOffs1));
                    breakpoints.Add(new CascBreakpoint2(data.CascOffs2));
                }
                else
                {
                    throw new Exception("No offset data for build " + build);
                }

                pd.Run();

                var time = DateTime.Now;

                PrettyLogger.WriteLine(ConsoleColor.Yellow, "Attaching to {0}...", process.GetVersionInfo());
                PrettyLogger.WriteLine(ConsoleColor.Yellow, "Waiting 5 sec to come up...");
                while (!pd.WaitForComeUp(50) && time.MSecToNow() < 5000)
                { }

                if (!pd.IsDebugging)
                    throw new Exception("Failed to start logger");

                PrettyLogger.WriteLine(ConsoleColor.Yellow, "Installing breakpoints...");
                foreach (var bp in breakpoints)
                    pd.AddBreakPoint(bp, process.MainModule.BaseAddress);

                PrettyLogger.WriteLine(ConsoleColor.Magenta, "Successfully attached to {0} PID: {1}.",
                    process.GetVersionInfo(), process.Id);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Stop()
        {
            if (pd != null)
                pd.StopDebugging();
        }

        public void Join()
        {
            if (pd != null)
                pd.Join();
        }
    }
}
