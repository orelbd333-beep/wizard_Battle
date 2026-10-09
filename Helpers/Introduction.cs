using System;
using System.Diagnostics;
using System.Threading;

namespace wizards
{
    static class Introduction
    {
        public static void PlayIntroduction()
        {
            Console.WriteLine("Welcome to the wizard duel!");
            Console.WriteLine("To determine your power, you will"
             + " need to solve a series of puzzles.");
            Console.WriteLine("Each correct answer will increase your power. Let's begin!"
            + " (Press esc to skip the introduction.)");

            //describe the puzzle to the readers
            if (WaitOrSkip(6000)) return;
            Console.WriteLine("You will be given a string of numbers. rearange the numbers from lowest to highest in .5 seconds to gain power");

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
