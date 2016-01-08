using System;
using WhiteMagic;
using WhiteMagic.WinAPI.Types;

namespace CascBP
{
    public class WowBreakpoint : HardwareBreakPoint
    {
        public WowBreakpoint(int address)
            : base(new IntPtr(address), 1, BreakpointCondition.Code)
        {
        }
    }
}
