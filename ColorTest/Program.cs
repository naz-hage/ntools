Console.WriteLine("Testing Console Colors:");
Console.WriteLine();

// Test without color
Console.WriteLine("This line has no color");

// Test green
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("[SUCCESS] This should be GREEN");
Console.ResetColor();

// Test red
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("[ERROR] This should be RED");
Console.ResetColor();

// Test yellow
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("[WARNING] This should be YELLOW");
Console.ResetColor();

Console.WriteLine();
Console.WriteLine("Back to default color");
