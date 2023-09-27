namespace LptPortSniffer;

class Program
{
    enum Operation { Read, Write255 }

    static readonly Dictionary<Operation, Action<object?>> _actions = new()
    {
        { Operation.Read, Listen },
        { Operation.Write255, Write255 },
    };

    public static void Main()
    {
        if (!IsInpOutAvailable())
            return;

        Port[]? ports;
        if ((ports = GetPorts()) is null)
            return;

        Port? port;
        if ((port = ChoosePort(ports)) is null)
            return;

        Operation? operation;
        if ((operation = ChooseOperation()) is null)
            return;

        LoopPortOperation((Port)port, (Operation)operation);
    }

    // Functions

    static bool IsInpOutAvailable()
    {
        try
        {
            if (Port.IsAvailable() == 0)
                throw new Exception("LPT port IO is not available.\nIf this is the first time the app was launched, then the issue may be resolved after restart.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }

        return true;
    }

    static Port[]? GetPorts()
    {
        Port[] lptPorts = Port.GetAll();
        if (lptPorts.Length == 0)
        {
            Console.WriteLine("No LPT ports found.");
            return null;
        }

        Console.WriteLine("LPT ports:");
        int portId = 1;
        foreach (var lptPort in lptPorts)
        {
            Console.WriteLine($"  {portId}: {lptPort}");
            portId++;
        }

        return lptPorts;
    }

    static Port? ChoosePort(Port[] ports)
    {
        int portId = 0;
        if (ports.Length > 1)
        {
            Console.WriteLine("");
            Console.Write($"Select the port (1 - {ports.Length}): ");

            portId = ReadDigit(ports.Length);
        }

        return portId < 0 ? null : ports[portId];
    }

    static Operation? ChooseOperation()
    {
        Console.WriteLine("");
        Console.WriteLine($"Actions available:");

        int i = 1;
        foreach (var kv in _actions)
        {
            Console.WriteLine($"  {i++}: {kv.Key}");
        }

        Console.WriteLine("");
        Console.WriteLine($"Select the action (1 - {_actions.Count}): ");

        int operationId = ReadDigit(_actions.Count);
        return operationId < 0 ? null : (Operation)operationId;
    }

    static int ReadDigit(int max)
    {
        int result;
        do
        {
            var input = Console.ReadKey();
            if (input.Key == ConsoleKey.Escape || input.Key == ConsoleKey.Enter)
            {
                return -1;
            }
            if (int.TryParse(input.KeyChar.ToString(), out int digit) && digit > 0 && digit <= max)
            {
                result = digit - 1;
                Console.WriteLine("");
                break;
            }
            Console.CursorLeft = 0;
            Console.Write($"'{input.KeyChar}' is an invalid ID. ");
            Console.WriteLine($"Please try again, or press ESC or ENTER to exit: ");
        } while (true);

        return result;
    }

    static void LoopPortOperation(Port port, Operation operation)
    {
        var action = _actions[operation];
        if (action == null)
            return;

        Console.CursorTop--;
        Console.CursorLeft = 0;
        Console.WriteLine(" ");
        Console.WriteLine($"Executing '{operation}' operation on port {port.Name}");

        var thread = new Thread(new ParameterizedThreadStart(action));

        thread.Start(port);

        Console.WriteLine("Press any key to exit . . .");
        Console.WriteLine("");
        Console.ReadKey();

        thread.Interrupt();
        thread.Join();
    }

    static void Listen(object? param)
    {
        if (param is Port port)
        {
            try
            {
                ulong cycle = 0;
                short[] portValue = new short[port.To - port.From + 1];
                while (Thread.CurrentThread.ThreadState == ThreadState.Running)
                {
                    bool hasNewValue = false;
                    for (int i = port.From; i <= port.To; i++)
                    {
                        var currentPortValue = Port.Read(i);
                        if (portValue[i - port.From] != currentPortValue)
                        {
                            portValue[i - port.From] = currentPortValue;
                            hasNewValue = true;
                        }
                    }

                    if (hasNewValue)
                    {
                        if (cycle > 0)
                        {
                            Console.WriteLine($"=== New value(s) on cycle {cycle} ===");
                        }
                        for (int i = port.From; i <= port.To; i++)
                        {
                            Console.WriteLine($"{i:x4} => {portValue[i - port.From]}");
                        }
                    }

                    cycle++;
                    Thread.Sleep(10);
                }
            }
            catch (ThreadInterruptedException) { }
        }
    }

    static void Write255(object? param)
    {
        if (param is Port port)
        {
            try
            {
                short value = 0;
                Console.WriteLine("Writing to all ports:");
                while (Thread.CurrentThread.ThreadState == ThreadState.Running)
                {
                    for (int i = port.From; i <= port.To; i++)
                    {
                        Port.Write(i, value);
                    }
                    Console.WriteLine(value);
                    Thread.Sleep(200);

                    if (++value > 255)
                        break;
                }

                Console.WriteLine("Done. Press any key to exit . . .");
            }
            catch (ThreadInterruptedException) { }
        }
    }
}