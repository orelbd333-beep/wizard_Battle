using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
           

            //get thier power through solving puzzles
            
            //tell the user that theier going to play puzzles to get power
            Introduction.PlayIntroduction();

            //get dumbbledores power
            Console.WriteLine("Dumbledores turn:");
            //get a string of random numbers
            Random rand = new Random();
            string randomPuzzle  = rand.Next(1000,10000).ToString();
            //Console.WriteLine("dumbledore's number: " + randomPuzzle); IT WORKS!!!


            int DumbledorePower = 0;
            int RiddlesPower = 0;

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
