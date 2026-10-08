using System;
using System.Diagnostics;
using System.Threading;

namespace wizards
{
    static class Introduction
    {
        public static void PlayIntroduction()
        {
            Console.WriteLine("Welcome to the wizard duel! To determine your power, you will"
             + " need to solve a series of puzzles. Each correct answer will increase your power. Let's begin!"
             + " Press esc to skip");


            if (WaitOrSkip(6000)) return;
            Console.WriteLine("3");
            if (WaitOrSkip(1500)) return;
            Console.WriteLine("2");
            if (WaitOrSkip(1500)) return;
            Console.WriteLine("1!!");
        }

        static bool WaitOrSkip(int milliseconds)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < milliseconds)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    return true;
                }

                Thread.Sleep(20);
            }

            return false;
        }
    }
}
