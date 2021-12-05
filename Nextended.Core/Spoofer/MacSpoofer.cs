using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Microsoft.Win32;

namespace Nextended.Core.Spoofer;

public class MacSpoofer
{
    private static readonly RegistryKey NetworkClass = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\");
    private readonly string driverDesc;
    private readonly ManagementObject NetworkAdapter;
    private readonly RegistryKey NetworkInterface;
    private string Device;
    private string RegPath = @"Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\";

    public MacSpoofer(string deviceId)
    {
        driverDesc = GetDriverDescByID(deviceId);
        Device = deviceId;
        NetworkInterface = NetworkClass.OpenSubKey(deviceId, true);
        NetworkAdapter = new ManagementObjectSearcher("select * from win32_networkadapter where Name='" + driverDesc + "'").Get()
                .Cast<ManagementObject>().FirstOrDefault();
    }


    private static string GenerateId(int i)
    {
        return i.ToString().PadLeft(4, '0');
    }

    // Generate a random MAC address
    public static string GenerateRandomMAC()
    {
        var r = new Random((int) DateTime.Now.ToFileTimeUtc());
        var abc = "0123456789ABCDEF";
        var MAC = "";
        for (var i = 1; i < 12; i++) MAC += abc[r.Next(0, 15)];
        return MAC;
    }

    private bool DisableNetworkDriver()
    {
        try
        {
            if ((uint) NetworkAdapter.InvokeMethod("Disable", null) == 0)
                return true;
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private bool EnableNetworkDriver()
    {
        try
        {
            if ((uint) NetworkAdapter.InvokeMethod("Enable", null) == 0)
                return true;
        }
        catch
        {
            // ignored
        }

        return false;
    }

    public static List<string> GetDeviceIDs()
    {
        var IDs = new List<string>();
        for (var i = 0; i <= 9999; i++)
        {
            var ID = GenerateId(i);
            var regKey = NetworkClass.OpenSubKey(ID);
            if (regKey != null)
                IDs.Add(ID);
            else
                break;
        }

        return IDs;
    }

    public static string GetDriverDescByID(string id)
    {
        return NetworkClass.OpenSubKey(id).GetValue("DriverDesc").ToString();
    }

    public bool Spoof(string MAC)
    {
        if (DisableNetworkDriver())
            return false;
        NetworkInterface.SetValue("NetworkAddress", MAC, RegistryValueKind.String);
        if (EnableNetworkDriver())
            return false;
        return true;
    }

    public bool Spoof()
    {
        return Spoof(GenerateRandomMAC());
    }

    public bool Reset()
    {
        if (!DisableNetworkDriver())
            return false;

        NetworkInterface.DeleteValue("NetworkAddress");

        return EnableNetworkDriver();
    }
}