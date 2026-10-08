using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wizards

{
    class Program
    {
        static void Main(string[] args)
        {
            //get dumbledores power
            //Console.WriteLine("Enter Dumbledores power:");
            //int DumbledorePower = int.Parse(Console.ReadLine());

            //get riddles power
            //Console.WriteLine("Enter Riddles power:");
            //int RiddlesPower = int.Parse(Console.ReadLine());

            //get thier power through solving puzzles

            //get dumbbledores power
            Console.WriteLine("Dumbledores turn:");
            


            if (DumbledorePower * 2 == RiddlesPower)
            {
                Console.WriteLine("Dumbledore wins!!!");
            }
            else if (RiddlesPower * 2 == DumbledorePower)
            {
                Console.WriteLine("Riddle wins!!!");
            }
            else if (Math.Abs(DumbledorePower - RiddlesPower) < 10)
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