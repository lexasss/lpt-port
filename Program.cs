namespace LptPortSniffer;

class Program
{
    enum Operation { ReadWholeRange, ReadPins, WriteFrom0To255 }

    static readonly Dictionary<Operation, Action<object?>> _actions = new()
    {
        { Operation.ReadWholeRange, ReadWholeRange },
        { Operation.ReadPins, ReadPins },
        { Operation.WriteFrom0To255, WriteFrom0To255 },
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

        LoopPortOperation(port, (Operation)operation);
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

    #region Actions

    static void ReadWholeRange(object? param)
    {
        if (param is not Port port)
            return;

        try
        {
            ulong cycle = 0;
            short[] portValues = new short[port.ToAddress - port.FromAddress + 1];
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                bool hasNewValue = false;
                for (int i = port.FromAddress; i <= port.ToAddress; i++)
                {
                    var currentPortValue = Port.Read(i);
                    if (portValues[i - port.FromAddress] != currentPortValue)
                    {
                        portValues[i - port.FromAddress] = currentPortValue;
                        hasNewValue = true;
                    }
                }

                if (hasNewValue)
                {
                    if (cycle > 0)
                    {
                        Console.WriteLine($"=== New value(s) on cycle {cycle} ===");
                    }
                    for (int i = port.FromAddress; i <= port.ToAddress; i++)
                    {
                        Console.WriteLine($"{i:x4} => {portValues[i - port.FromAddress]}");
                    }
                }

                cycle++;
                Thread.Sleep(10);
            }
        }
        catch (ThreadInterruptedException) { }
    }

    static void ReadPins(object? param)
    {
        if (param is not Port port)
            return;

        try
        {
            ulong cycle = 0;
            int pinValues = 0;
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                var currentPinValues = port.ReadAll();
                if (pinValues != currentPinValues)
                {
                    if (cycle > 0)
                    {
                        Console.WriteLine($"=== New value(s) on cycle {cycle} ===");
                    }
                    for (int i = 0; i < 20; i++)
                    {
                        var mask = 2 ^ i;
                        if ((pinValues & mask) != (currentPinValues & mask))
                        {
                            var type = i switch
                            {
                                <= 7 => 'D',
                                >= 10 and <= 16 => 'S',
                                _ => 'C'
                            };
                            var index = i switch
                            {
                                <= 7 => i,
                                >= 10 and <= 16 => i - 10,
                                _ => i - 16
                            };
                            var value = (currentPinValues & mask) > 0 ? "ON" : "OFF";
                            Console.WriteLine($"{type}{index} = {value}");
                        }
                    }
                    pinValues = currentPinValues;
                }

                cycle++;
                Thread.Sleep(10);
            }
        }
        catch (ThreadInterruptedException) { }
    }

    static void WriteFrom0To255(object? param)
    {
        if (param is not Port port)
            return;

        try
        {
            short value = 0;
            Console.WriteLine("Writing to DATA bus from 0 to 255:");
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                port.WriteData(value);
                Console.WriteLine(value);
                Thread.Sleep(200);

                if (++value > 255)
                    break;
            }

            Console.WriteLine("Done. Press any key to exit . . .");
        }
        catch (ThreadInterruptedException) { }
    }

    #endregion
}