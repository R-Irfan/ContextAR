using UnityEngine;

public static class AppConfig
{
    private const string KEY_IP = "SERVER_IP";

    public static void SetIP(string ip)
    {
        PlayerPrefs.SetString(KEY_IP, ip);
        PlayerPrefs.Save();
    }

    public static string GetIP()
    {
        return PlayerPrefs.GetString(KEY_IP, "");
    }

    public static bool HasIP()
    {
        return PlayerPrefs.HasKey(KEY_IP);
    }

    public static void ClearIP()
    {
        PlayerPrefs.DeleteKey(KEY_IP);
    }
}