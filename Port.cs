using System.Management;
using System.Runtime.InteropServices;

namespace LptPortSniffer;

internal readonly struct Port
{
    public string Name { get; init; }
    public int From { get; init; }
    public int To { get; init; }

    public override string ToString()
    {
        string range = $"0x{From:X4} - 0x{To:X4}";
        return $"{Name} ({range})";
    }

    [DllImport("inpout32.dll", EntryPoint = "IsInpOutDriverOpen")]
    public static extern uint IsAvailable();

    [DllImport("inpout32.dll", EntryPoint = "Inp32")]
    public static extern short Read(int address);
    [DllImport("inpout32.dll", EntryPoint = "Out32")]
    public static extern void Write(int adress, short value); // decimal

    [DllImport("inpout32.dll", EntryPoint = "DlPortReadPortUchar")]
    public static extern byte ReadUInt8(short PortAddress);
    [DllImport("inpout32.dll", EntryPoint = "DlPortWritePortUchar")]
    public static extern void WriteUint8(short PortAddress, byte Data);

    [DllImport("inpout32.dll", EntryPoint = "DlPortReadPortUshort")]
    public static extern ushort ReadUInt16(short PortAddress);
    [DllImport("inpout32.dll", EntryPoint = "DlPortWritePortUshort")]
    public static extern void WriteUint16(short PortAddress, ushort Data);

    [DllImport("inpout32.dll", EntryPoint = "DlPortWritePortUlong")]
    public static extern void WriteUInt32(int PortAddress, uint Data);
    [DllImport("inpout32.dll", EntryPoint = "DlPortReadPortUlong")]
    public static extern uint ReadUInt32(int PortAddress);

    public short Read() => Read(From);
    public void Write(short value) => Write(From, value);

    public static Port[] GetAll()
    {
        var ports = new List<Port>();
        ManagementObjectSearcher lptPortSearcher;
        try
        {
            lptPortSearcher = new ManagementObjectSearcher("Select * From Win32_ParallelPort");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when retrieving Win32_ParallelPort: {ex.Message}");
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
                Console.WriteLine($"Exception when retrieving Win32_PnPAllocatedResource for {lptPort.ClassPath}: {ex.Message}");
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
                        Console.WriteLine($"Exception when retrieving Win32_PortResource for {pnp.ClassPath}: {ex.Message}");
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
