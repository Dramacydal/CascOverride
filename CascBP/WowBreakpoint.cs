using System;
using WhiteMagic;
using WhiteMagic.Pointers;
using WhiteMagic.WinAPI;

namespace CascBP
{
    public class WowBreakpoint : CodeBreakpoint
    {
        public WowBreakpoint(int Offset)
            : base(new ModulePointer(Offset))
        {
        }
    }
}
