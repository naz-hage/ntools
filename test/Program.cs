namespace LaunchTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0].ToLower() == "pass")
            {
                Console.WriteLine($"pass");
                Console.WriteLine($"Exit (0)");
                Environment.Exit(0);
            }
            else if (args[0].ToLower() == "fail")
            {
                Console.WriteLine("fail");
                Console.WriteLine("error");
                Console.WriteLine("rejected");
                Console.WriteLine("exception");
                Console.WriteLine($"Exit (-100)");
                Environment.Exit(-100);
            }
        }
    }
}
