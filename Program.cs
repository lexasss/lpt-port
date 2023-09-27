using LPTReader;

Port[] lptPorts = PortReader.GetParallelPorts();
if (lptPorts.Length == 0)
{
    Console.WriteLine("No LPT ports found. Exiting.");
    return;
}

Console.WriteLine("LPT ports:");
int portId = 1;
foreach (var lptPort in lptPorts)
{
    Console.WriteLine($"{portId}: {lptPort}");
    portId++;
}

portId = 0;
if (lptPorts.Length > 1)
{
    Console.WriteLine("");
    Console.Write($"Select the port (1 - {lptPorts.Length}): ");
    do
    {
        var input = Console.ReadKey();
        if (input.Key == ConsoleKey.Escape || input.Key == ConsoleKey.Enter)
        {
            return;
        }
        if (int.TryParse(input.KeyChar.ToString(), out int digit) && digit > 0 && digit <= lptPorts.Length)
        {
            portId = digit - 1;
            Console.WriteLine("");
            break;
        }
        Console.WriteLine("");
        Console.Write($"'{input.KeyChar}' is an invalid port ID.");
        Console.Write($"Please try again, or press ESC or ENTER to exit: ");
    } while (true);
}

var port = lptPorts[portId];
short portValue = 0;

var thread = new Thread(() =>
{
    while (Thread.CurrentThread.ThreadState == ThreadState.Running)
    {
        var currentPortValue = PortReader.Inp32(port.From);
        if (currentPortValue != portValue)
        {
            portValue = currentPortValue;
            Console.WriteLine($"Value: {portValue}");
        }
        Thread.Sleep(10);
    }
});

Console.WriteLine("Press any key to exit . . .");
Console.ReadKey();

thread.Join();
