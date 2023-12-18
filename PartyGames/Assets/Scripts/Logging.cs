using System;
using UnityEngine;

public class Logging
{
    /// <summary>
    /// Ist der Typ der auszugebenen Nachrichten
    /// </summary>
    public enum LogType
    {
        Debug = 0,
        Normal = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    };
    /// <summary>
    /// Erstellt eine Log-Nachricht
    /// </summary>
    /// <param name="type">Wichtigkeits Grad der Ausgabe</param>
    /// <param name="klasse">Betroffene Klasse</param>
    /// <param name="methode">Betroffene Methode</param>
    /// <param name="msg">Auszugebene Nachricht</param>
    public static void log(LogType type, string klasse, string methode, string msg)
    {
        generate(type, klasse, methode, msg);
    }
    /// <summary>
    /// Erstellt eine Log-Nachricht
    /// </summary>
    /// <param name="type">Wichtigkeits Grad der Ausgabe</param>
    /// <param name="klasse">Betroffene Klasse</param>
    /// <param name="methode">Betroffene Methode</param>
    /// <param name="msg">Auszugebene Nachricht</param>
    /// <param name="e">Exception</param>
    public static void log(LogType type, string klasse, string methode, string msg, Exception e)
    {
        generate(type, klasse, methode, msg + " >> Exception:\n" + e);
    }
    /// <summary>
    /// Erstellt eine Log-Nachricht
    /// </summary>
    /// <param name="type">Wichtigkeits Grad der Ausgabe</param>
    /// <param name="msg">Auszugebene Nachricht</param>
    public static void log(LogType type, string msg)
    {
        generate(type, "", "", msg);
    }
    /// <summary>
    /// Erstellt die auszugebene Nachricht mit Zeitstempel
    /// </summary>
    /// <param name="type">Wichtigkeits Grad der Ausgabe</param>
    /// <param name="klasse">Betroffene Klasse</param>
    /// <param name="methode">Betroffene Methode</param>
    /// <param name="msg">Auszugebene Nachricht mit evtl. Exception</param>
    private static void generate(LogType type, string klasse, string methode, string msg)
    {
#if UNITY_EDITOR
        //print(type, "[" + methode + "] " + msg);
        string message = "[" + methode + "] " + msg;
        if (type == LogType.Debug && Config.DEBUG_MODE)
            Debug.Log("DEBUG " + message);
        else if (type == LogType.Normal)
            Debug.Log(message);
        else if (type == LogType.Warning)
            Debug.LogWarning(message);
        else if (type == LogType.Error)
            Debug.LogError(message);
        else if (type == LogType.Fatal)
            Debug.LogError(message);
#else
        //print(type, "[" + klasse + " - " + methode + "] " + msg); 
        string message = "[" + methode + "] " + msg;
        message = DateTime.Now + " " + message;
        if (type == LogType.Debug || Config.DEBUG_MODE)
            Debug.Log("DEBUG >> " + message);
        else if (type == LogType.Normal)
            Debug.Log("NORMAL >> " + message);
        else if (type == LogType.Warning)
            Debug.LogWarning("WARNING >> " + message);
        else if (type == LogType.Error)
            Debug.LogError("ERROR >> " + message);
        else if (type == LogType.Fatal)
            Debug.LogError("FATAL >> " + message);
#endif
    }
    /// <summary>
    /// Gibt die Lognachricht aus
    /// </summary>
    /// <param name="type">Wichtigkeits Grad der Ausgabe</param>
    /// <param name="message">Auszugebene Nachricht</param>
    private static void print(LogType type, string message)
    {
#if UNITY_EDITOR
        if (type == LogType.Debug && Config.DEBUG_MODE)
            Debug.Log("DEBUG " + message);
        else if (type == LogType.Normal)
            Debug.Log(message);
        else if (type == LogType.Warning)
            Debug.LogWarning(message);
        else if (type == LogType.Error)
            Debug.LogError(message);
        else if (type == LogType.Fatal)
            Debug.LogError(message);
#else
        message = DateTime.Now + " " + message;
        if (type == LogType.Debug || Config.DEBUG_MODE)
            Debug.Log("DEBUG >> " + message);
        else if (type == LogType.Normal)
            Debug.Log("NORMAL >> " + message);
        else if (type == LogType.Warning)
            Debug.LogWarning("WARNING >> " + message);
        else if (type == LogType.Error)
            Debug.LogError("ERROR >> " + message);
        else if (type == LogType.Fatal)
            Debug.LogError("FATAL >> " + message);
#endif
    }
}

