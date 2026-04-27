using System.Net;
using System.Net.Sockets;

public static class IPValidator
{
    public static bool IsValidIPv4(string ip)
    {
        if (IPAddress.TryParse(ip, out IPAddress address))
        {
            return address.AddressFamily == AddressFamily.InterNetwork;
        }
        return false;
    }
}