namespace RouderSky;

public static class DebugMgr
{
    public static bool EnableLogging = true;
        
        
    public static void LogInfo(Func<string> func) {
        if (!EnableLogging) {
            return;
        }
        Console.WriteLine(func());
    }
}