using UnityEngine;
using System.Collections;
public static class GameLogger
{
    public enum LogCategory
    {
        Auth,
        Network,
        Gameplay,
        Server
    }
    
    public static void Log(LogCategory category, string message, UnityEngine.Object context = null)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.Log($"[{timestamp}][{category}] {message}", context);
    }
    
    public static void LogWarning(LogCategory category, string message, UnityEngine.Object context = null)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.LogWarning($"[{timestamp}][{category}] {message}", context);
    }
    
    public static void LogError(LogCategory category, string message, UnityEngine.Object context = null)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.LogError($"[{timestamp}][{category}] {message}", context);
    }
}