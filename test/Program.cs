using test;

try
{
    switch (args[0].ToLower())
    {
        case "test":
            LaucnherTest.Test();
            break;
        case "pass":
            Console.WriteLine($"pass");
            Console.WriteLine($"Exit (0)");
            Environment.Exit(0);
            break;
        case "fail":
            Console.WriteLine("fail");
            Console.WriteLine("error");
            Console.WriteLine("rejected");
            Console.WriteLine("exception");
            Console.WriteLine($"Exit (-100)");
            Environment.Exit(-100);
            break;

        default:
            
            Environment.Exit(-300);
            break;
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
    Environment.Exit(-200);
}

