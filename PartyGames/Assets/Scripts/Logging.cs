using System;
using System.Collections.Generic;
using UnityEngine;

public class Logging
{
    public enum Type
    {
        Normal = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    };

    public Type type { get; set; }
    public DateTime time { get; set; }
    public string klasse { get; set; }
    public string methode { get; set; }
    public string msg { get; set; }
    public Exception exception { get; set; }

    public Logging(Type type, string klasse, string methode, string msg)
    {
        this.type = type;
        this.time = DateTime.Now;
        this.klasse = klasse;
        this.methode = methode;
        this.msg = msg;
        this.exception = null;
    }

    public Logging(Type type, string klasse, string methode, string msg, Exception exception)
    {
        this.type = type;
        this.time = DateTime.Now;
        this.klasse = klasse;
        this.msg = msg;
        this.exception = exception;
    }

    public static string getType(List<Logging> log, Type type, bool withExceptions)
    {
        string list = "";
        foreach (Logging l in log)
        {
            if (type <= l.type)
            {
                list += "[" + l.time + "] " + l.type + " " + l.klasse + ": " + l.methode + ": " + l.msg;

                if (withExceptions)
                    list += " <<" + l.exception.Message + ">>";
                list += "\n";
            }
        }
        return list;
    }

    public string tostring()
    {
        string s = "[" + this.time + "] " + this.type + " " + this.klasse + ": " + this.methode + ": " + this.msg;
        if (this.exception != null)
            s += " <<" + this.exception.Message + ">>";
        return s;
    }

    public static void add(Logging log)
    {
        Config.log.Add(log);

        if (log.type == Type.Normal)
        {
            if (log.exception == null)
            {
                Debug.Log(log.klasse + ": " + log.methode + ": " + log.msg);
            }
            else
            {
                Debug.Log(log.klasse + ": " + log.methode + ": " + log.msg + " <<" + log.exception.Message + ">>");
            }
        }
        else if (log.type == Type.Warning)
        {
            if (log.exception == null)
            {
                Debug.LogWarning(log.klasse + ": " + log.methode + ": " + log.msg);
            }
            else
            {
                Debug.LogWarning(log.klasse + ": " + log.methode + ": " + log.msg + " <<" + log.exception.Message + ">>");
            }
        }
        else if (log.type == Type.Error || log.type == Type.Fatal)
        {
            if (log.exception == null)
            {
                Debug.LogError(log.klasse + ": " + log.methode + ": " + log.msg);
            }
            else
            {
                Debug.LogError(log.klasse + ": " + log.methode + ": " + log.msg + " <<" + log.exception.Message + ">>");
            }
        }
    }

    public static void add(Type type, string klasse, string method, string msg, Exception exeption)
    {
        add(new Logging(type, klasse, method, msg, exeption));
    }
}

