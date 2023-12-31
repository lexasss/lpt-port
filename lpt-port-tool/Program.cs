﻿namespace LptPortTool;
using Port = LptPort.LptPort;

class Program
{
    enum Operation {
        ReadWholeRange,
        ReadPins,
        WriteToData0To255,
        SetAllDataPins,
        ClearAllDataPins,
        SetAllControlPins,
        ClearAllControlPins,
        SetDataPin,
        SetControlPin }

    static readonly Dictionary<Operation, (Action<object?>, bool)> _actions = new()
    {
        { Operation.ReadWholeRange, (ReadWholeRange, false) },
        { Operation.ReadPins, (ReadPins, false) },
        { Operation.WriteToData0To255, (WriteToData0To255, false) },
        { Operation.SetAllDataPins, ((o) => WriteToData(o, 0xFF), true) },
        { Operation.ClearAllDataPins, ((o) => WriteToData(o, 0), true) },
        { Operation.SetAllControlPins, ((o) => WriteToControl(o, 0xFF), true) },
        { Operation.ClearAllControlPins, ((o) => WriteToControl(o, 0), true) },
        { Operation.SetDataPin, (SetDataPin, true) },
        { Operation.SetControlPin, (SetControlPin, true) },
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

        do
        {
            Operation? operation;
            if ((operation = ChooseOperation()) is null)
                return;

            RunOperationThread(port, (Operation)operation);
            
        } while (true);
    }

    #region Procedure steps

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
        Port[] lptPorts = Port.GetPorts();
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
        Console.WriteLine("Actions available:");

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
            if (input.Key == ConsoleKey.Escape)
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
            Console.Write($"'{input.KeyChar}' is an invalid value. ");
            Console.WriteLine($"Please try again, or press ESC to exit: ");
        } while (true);

        return result;
    }

    static void RunOperationThread(Port port, Operation operation)
    {
        var (action, isHandlingInput) = _actions[operation];
        if (action == null)
            return;

        Console.CursorTop--;
        Console.CursorLeft = 0;
        Console.WriteLine(" ");
        Console.WriteLine($"Executing '{operation}' operation on port {port.Name}");
        if (!isHandlingInput)
            Console.WriteLine("Press ESC to exit . . .");
        Console.WriteLine("");

        var thread = new Thread(new ParameterizedThreadStart(action));
        thread.Start(port);

        if (!isHandlingInput)
        {
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            { }

            thread.Interrupt();
            Console.WriteLine("\nA");
        }

        thread.Join();
    }

    #endregion

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
                var currentPinValues = port.ReadAllAsPins();
                if (pinValues != currentPinValues)
                {
                    Console.WriteLine($"=== Value 0x{currentPinValues:x4} (binary = 0x{port.ReadAll():x4}) on cycle {cycle} ===");
                    for (byte i = 0; i < 20; i++)
                    {
                        var mask = 1 << i;
                        if ((pinValues & mask) != (currentPinValues & mask) || cycle == 0)
                        {
                            var (type, index) = i switch
                            {
                                <= 7 => ('D', i),
                                >= 11 and <= 15 => ('S', i - 8),
                                >= 16 => ('C', i - 16),
                                _ => ('\0', 0)
                            };
                            if (type != '\0')
                            {
                                var value = (currentPinValues & mask) > 0 ? "ON" : "OFF";
                                Console.WriteLine($"{type}{index} = {value} [{Port.PinName(i)}]");
                            }
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

    static void WriteToData0To255(object? param)
    {
        if (param is not Port port)
            return;

        try
        {
            short value = 0;
            Console.WriteLine("Writing 0 to 255 to DATA bus:");
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                port.WriteData(value);
                Console.WriteLine(value);
                Thread.Sleep(200);

                if (++value > 255)
                    break;
            }
        }
        catch (ThreadInterruptedException) { }
    }

    static void WriteToData(object? param, byte value)
    {
        if (param is not Port port)
            return;

        try
        {
            port.WriteData(value);
            Thread.Sleep(500);
        }
        catch (ThreadInterruptedException) { }
    }

    static void WriteToControl(object? param, byte value)
    {
        if (param is not Port port)
            return;

        try
        {
            port.WriteControl(value);
            Thread.Sleep(500);
        }
        catch (ThreadInterruptedException) { }
    }

    static void SetDataPin(object? param)
    {
        if (param is not Port port)
            return;

        try
        {
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                Console.Write("Choose pin (1-8): ");
                int pin = ReadDigit(8);
                if (pin < 0)
                    break;

                Console.Write("Choose value (1 = off, 2 = on): ");
                int value = ReadDigit(2);
                if (value < 0)
                    break;

                var allPinsData = port.ReadData();
                if (value == 1)
                {
                    port.WriteData((short)(allPinsData | (1 << pin)));
                }
                else
                {
                    port.WriteData((short)(allPinsData & ~(1 << pin)));
                }

                Console.WriteLine();
                Thread.Sleep(200);
            }
        }
        catch (ThreadInterruptedException) { }
    }

    static void SetControlPin(object? param)
    {
        if (param is not Port port)
            return;

        try
        {
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                Console.WriteLine($"Pins: 1 - {Port.PinName(16)}, 2 - {Port.PinName(17)}, 3 - {Port.PinName(18)}, 4 - {Port.PinName(19)}");
                Console.Write("Choose pin (1-4): ");
                int pin = ReadDigit(4);
                if (pin < 0)
                    break;

                Console.Write("Choose value (1 = off, 2 = on): ");
                int value = ReadDigit(2);
                if (value < 0)
                    break;

                var allPinsData = port.ReadControl();
                if (value == 1)
                {
                    port.WriteControl((short)(allPinsData | (1 << pin)));
                }
                else
                {
                    port.WriteControl((short)(allPinsData & ~(1 << pin)));
                }

                Console.WriteLine();
                Thread.Sleep(200);
            }
        }
        catch (ThreadInterruptedException) { }
    }

    #endregion
}