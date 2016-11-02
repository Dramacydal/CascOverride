using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using WhiteMagic;
using WhiteMagic.WinAPI;

namespace CascBP
{
    class Program
    {
        static int? ExtractBuildFromArgs(string[] args)
        {
            var buildMatch = args.Where(it => Regex.IsMatch(it, "--build=[0-9]+"));
            if (buildMatch.Count() != 0)
            {
                var arg = buildMatch.First();
                var tokens = arg.Split('=');
                if (tokens.Count() == 2)
                {
                    int build;
                    if (int.TryParse(tokens[1], out build))
                        return build;
                }
            }

            return null;
        }

        static void Main(string[] args)
        {
            CascBP cascBP = null;
            Process process;
            try
            {
                process = MagicHelpers.SelectProcess("world of warcraft");
                var debugging = false;
                Kernel32.CheckRemoteDebuggerPresent(process.Handle, ref debugging);
                if (debugging)
                    throw new Exception("Already being debugged.");

                cascBP = new CascBP(process);
            }
            catch (Exception e)
            {
                PrettyLogger.WriteLine(ConsoleColor.Red, "Failed to select process: \"{0}\"", e.Message);
                return;
            }

            if (!MagicHelpers.SetDebugPrivileges())
            {
                PrettyLogger.WriteLine(ConsoleColor.Red, "Failed to set debug privileges");
                return;
            }

            try
            {
                var build = ExtractBuildFromArgs(args);
                if (build != null)
                    cascBP.ForceBuild((int)build);

                cascBP.Start();
                cascBP.Join();
            }
            catch (Exception e)
            {
                PrettyLogger.WriteLine(ConsoleColor.Red, "Exception: {0}", e.Message);
                return;
            }
        }
    }
}
