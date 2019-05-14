using System;
using System.Net.Http;

namespace LoadTester
{
    class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Starting Load Test ...");
            Console.WriteLine("Press 1 to Quit or 2 to fire X requests or Enter to fire single case.");

            String response = string.Empty;
            while (!response.Equals("1"))
            {

                var tester = new Tester();
                var result = "";
                if (response == "2")
                {
                    tester.RunTestNoWait();
                }
                else
                {
                    tester.RunTest();
                    
                }

                //Console.WriteLine("Result: " + result);

                Console.ReadLine();

                Console.WriteLine($"Got2: {response}");
                response = Console.ReadLine();
                Console.WriteLine($"Got3: {response}");
            }
            Console.WriteLine($"Final: {response}");
        }
    }
}
