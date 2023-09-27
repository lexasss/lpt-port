using System.Management;
using System.Runtime.InteropServices;

namespace LPTReader;

internal readonly struct Port
{
    public string Name { get; init; }
    public int From { get; init; }
    public int To { get; init; }
    public override string ToString()
    {
        string range = "0x" + Convert.ToString(From, 16).PadLeft(2, '0') + " - 0x" + Convert.ToString(To, 16).PadLeft(2, '0');
        return $"{Name} ({From:X2}{range.ToUpper()})";
    }
}

internal static class PortReader
{
    [DllImport("Inpout32.dll")]
    public static extern short Inp32(int address);
    [DllImport("inpout32.dll", EntryPoint = "Out32")]
    public static extern void Output(int adress, int value); // decimal

    public static Port[] GetParallelPorts()
    {
        var ports = new List<Port>();
        ManagementObjectSearcher lptPortSearcher;
        try
        {
            lptPortSearcher = new ManagementObjectSearcher("Select * From Win32_ParallelPort");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when retrieving Win32_ParallelPort: {ex}");
            return ports.ToArray();
        }

        foreach (var lptPort in lptPortSearcher.Get())
        {
            ManagementObjectSearcher pnpSearcher;
            try
            {
                pnpSearcher = new ManagementObjectSearcher("Select * From Win32_PnPAllocatedResource");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when retrieving Win32_PnPAllocatedResource for {lptPort.ClassPath}: {ex}");
                continue;
            }

            string? searchTerm;
            try
            {
                searchTerm = lptPort.Properties["PNPDeviceId"].Value.ToString()?.Replace(@"\", @"\\");
            }
            catch
            {
                Console.WriteLine($"'PNPDeviceId' is not in {lptPort.ClassPath}... Skipped.");
                continue;
            }

            if (searchTerm is null)
                continue;

            foreach (var pnp in pnpSearcher.Get())
            {
                string? dependentValue, antecedentValue;
                try
                {
                    dependentValue = pnp.Properties["dependent"].Value.ToString();
                    antecedentValue = pnp.Properties["antecedent"].Value.ToString();
                }
                catch
                {
                    Console.WriteLine($"'dependent' or 'antecedent' is not in {pnp.ClassPath}... Skipped.");
                    continue;
                }

                if (dependentValue?.Contains(searchTerm) ?? false)
                {
                    ManagementObjectSearcher portResourceSearcher;
                    try
                    {
                        portResourceSearcher = new ManagementObjectSearcher("Select * From Win32_PortResource");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception when retrieving Win32_PortResource for {pnp.ClassPath}: {ex}");
                        continue;
                    }

                    foreach (var portResource in portResourceSearcher.Get())
                    {
                        if (portResource.ToString() == antecedentValue)
                        {
                            int startAddress, endAddress;
                            try
                            {
                                startAddress = Convert.ToInt32(portResource.Properties["StartingAddress"].Value);
                                endAddress = Convert.ToInt32(portResource.Properties["EndingAddress"].Value);
                            }
                            catch
                            {
                                Console.WriteLine($"'StartingAddress' or 'EndingAddress' is not in {portResource.ClassPath}... Skipped.");
                                continue;
                            }
                            ports.Add(new Port() { Name = lptPort.Properties["Name"].Value.ToString() ?? "LPT", From = startAddress, To = endAddress });
                        }
                    }
                }
            }
        }

        return ports.ToArray();
    }
}