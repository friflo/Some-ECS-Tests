namespace RouderSky;

public class DebugMgr
{
    public static void LogInfo(Func<string> func) {
        Console.WriteLine(func());
    }
}