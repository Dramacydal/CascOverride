using System;

namespace CascBP
{
    static class PrettyLogger
    {
        public static void SetDefaultColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            var oldColor = Console.ForegroundColor;
            if (oldColor != color)
                Console.ForegroundColor = color;

            Console.WriteLine(format, args);

            if (oldColor != color)
                Console.ForegroundColor = oldColor;
        }

        public static void Write(ConsoleColor color, string format, params object[] args)
        {
            var oldColor = Console.ForegroundColor;
            if (oldColor != color)
                Console.ForegroundColor = color;

            Console.Write(format, args);

            if (oldColor != color)
                Console.ForegroundColor = oldColor;
        }
    }
}
