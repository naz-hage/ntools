using Launcher;

namespace test
{
    public class LaucnherTest
    {
        public static ResultHelper Test()
        {
            var result = Launcher.Launcher.Start(
                new()
                {
                    WorkingDir = Directory.GetCurrentDirectory(),
                    Arguments = "/?",
                    FileName = "robocopy",
                    RedirectStandardOutput = true
                }
            );
            if (result.IsSuccess())
            {
                Console.WriteLine("Success");
            }
            else
            {
                Console.WriteLine($"Code: {result.Code}");
                foreach (var line in result.Output)
                {
                    Console.WriteLine(line);
                }
            }
            return new ResultHelper();
        }
    }
}
