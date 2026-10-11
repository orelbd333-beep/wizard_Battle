using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 //get dumbledores power
            //Console.WriteLine("Enter Dumbledores power:");
            //int DumbledorePower = int.Parse(Console.ReadLine());

            //get riddles power
            //Console.WriteLine("Enter Riddles power:");
            //int RiddlesPower = int.Parse(Console.ReadLine());
namespace wizards

{
    class Wizards
    {
        static void Main(string[] args)
        {
            if (LanGame.ShowMenu())
            {
                return;
            }
           

            //get thier power through solving puzzles
            
            //tell the user that theier going to play puzzles to get power
            Introduction.PlayIntroduction();

            //get dumbbledores power

            // Both players use the same puzzle, timer, and power calculation.
            int RunTurn(string playerName)
            {
            //get a string of random numbers
            Random rand = new Random();
            string randomPuzzle  = rand.Next(1000,10000).ToString();
            if (playerName == "Dumbledore")
            {
            Console.WriteLine("Dumbledores turn:" + randomPuzzle);       
            }
            else
            {
                Console.WriteLine(playerName + "'s turn:" + randomPuzzle);
            }
            // Keep the answer as text so a leading zero is preserved.
            string correctAnswer = string.Concat(randomPuzzle.OrderBy(digit => digit));
            string playerAnswer = "";
            double timeLimit = 5; // 5 seconds
            bool solved = false;

            Stopwatch timer = Stopwatch.StartNew();

            while (timer.Elapsed.TotalSeconds < timeLimit)
            {
                double remaining = Math.Max(0, timeLimit - timer.Elapsed.TotalSeconds);

                // Return to the beginning of the line to update the countdown.
                Console.Write($"\rTime: {remaining:0.00}s | Answer: {playerAnswer,-4}");

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (timer.Elapsed.TotalSeconds >= timeLimit)
                        break;

                    if (key.KeyChar >= '0' && key.KeyChar <= '9')
                    {
                        playerAnswer += key.KeyChar;
                    }
                    else if (key.Key == ConsoleKey.Backspace && playerAnswer.Length > 0)
                    {
                        playerAnswer = playerAnswer.Remove(playerAnswer.Length - 1);
                    }

                    // Automatically submit when all four digits have been entered.
                    if (playerAnswer.Length == randomPuzzle.Length)
                    {
                        solved = playerAnswer == correctAnswer;
                        break;
                    }
                }

                Thread.Sleep(10);
            }

            timer.Stop();
            double finalRemaining = Math.Max(0, timeLimit - timer.Elapsed.TotalSeconds);
            Console.WriteLine($"\rTime: {finalRemaining:0.00}s | Answer: {playerAnswer,-4}");
            Console.WriteLine(solved
                ? $"Correct! You gained {timer.Elapsed.TotalSeconds/5 * 100:0} power."
                : $"Failed! The correct answer was {correctAnswer}.");

            int DumbledorePower = solved ? (int)(timer.Elapsed.TotalSeconds/5 * 100) : 0;
            if (playerName == "Dumbledore")
            {
            System.Console.WriteLine($"Dumbledore's power: {DumbledorePower}");
            }
            else
            {
                Console.WriteLine($"{playerName}'s power: {DumbledorePower}");
            }

            return DumbledorePower;
            }

            int DumbledorePower = RunTurn("Dumbledore");
            int RiddlesPower = 0;

            Console.WriteLine("Switching turns to Riddle! Get ready...");
            for (int seconds = 5; seconds > 0; seconds--)
            {
                Console.WriteLine($"Riddle's turn starts in {seconds}...");
                Thread.Sleep(1000);
            }

            RiddlesPower = RunTurn("Riddle");

            if (DumbledorePower == RiddlesPower)
            {
                Console.WriteLine("tie!!!");
            }
            else if (DumbledorePower > RiddlesPower)
            {
                Console.WriteLine("Dumbledore wins!!!");
            }
            else
            {
                Console.WriteLine("Riddle wins!!!");
            }
        }

    }
}
