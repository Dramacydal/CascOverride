using System;
using System.Collections.Generic;
using System.Diagnostics;
using WhiteMagic;
using WhiteMagic.Patterns;
using System.Text;
using System.Linq;
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
            public OffsData(int cascOffs, int portraitOffset = 0)
            {
                CastOffs = cascOffs;
                PortraitOffset = portraitOffset;
            }

            public int CastOffs { get; private set; }
            public int PortraitOffset { get; private set; }
        }

        Dictionary<int, OffsData> BuildDatas = new Dictionary<int, OffsData>()
        {
            // .text:00412FC0 74 78                          jz      short loc_41303A
            { 19865, new OffsData(0x00412FC0 - 0x400000) },
            // .text:00412B40 74 78                          jz      short loc_412BBA
            // .text:007DB625 83 BD 54 FB FF+                cmp     [ebp+var_4AC], 40h
            { 20779, new OffsData(0x00412B40 - 0x400000, 0x007DB625 - 0x400000) },
            // .text:00412B20                 jz      short loc_412B9A
            // .text:007DB5DF                 cmp     [ebp+var_4AC], 40h
            { 20886, new OffsData(0x00412B20 - 0x400000, 0x007DB5DF - 0x400000) },
            // .text:004131C6 74 78                             jz      short loc_413240
            // .text:007DB4C7 83 BD 54 FB FF FF+                cmp     [ebp+var_4AC], 40h
            { 21355, new OffsData(0x004131C6 - 0x400000, 0x007DB4C7 - 0x400000) },
        };

        class CascBreakpoint : WowBreakpoint
        {
            protected int JumpOffs { get; private set; }
            public CascBreakpoint(int offs)
                : base(offs)
            {
            }

            public override bool HandleException(ref CONTEXT ctx, ProcessDebugger pd)
            {
                ctx.Eip += 2;
                return true;
            }
        }

        class PortraitBreakpoint : WowBreakpoint
        {
            public PortraitBreakpoint(int offs)
                : base(offs)
            {
            }

            public override bool HandleException(ref CONTEXT ctx, ProcessDebugger pd)
            {
                ctx.Eip += 0x12;
                return true;
            }
        }

        class TestBp : WowBreakpoint
        {
            // Script_FocusUnit
            // .text:00D901BD 8B EC                          mov     ebp, esp

            // .text:00D48CD4 83 C4 10                             add     esp, 10h
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
                catch (Exception ex)
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
                    breakpoints.Add(new CascBreakpoint(data.CastOffs));
                    if (data.PortraitOffset != 0)
                        breakpoints.Add(new PortraitBreakpoint(data.PortraitOffset));
                    //breakpoints.Add(new LodBreakpoint(0x00982A9A - 0x400000));
                    breakpoints.Add(new TestBp());
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
