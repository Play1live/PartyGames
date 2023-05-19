using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigFile
{
    string path;
    string title;
    //Hashtable table;
    Dictionary<string, string> table;

    /// <summary>
    /// Erstellt eine Config mit passender Datei.
    /// Falls die dazugehörige Datei bereits existiert,
    /// wird diese eingelesen.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="title"></param>
    public ConfigFile(string path, string title)
    {
        this.path = path;
        this.title = title;
        //table = new Hashtable();
        table = new Dictionary<string, string>();

        if (File.Exists(path + title))
        {
            LoadTable();
        }
        else
        {
            CreateFile();
        }
    }
    /// <summary>
    /// Liest die Config ein
    /// </summary>
    private void LoadTable()
    {
        foreach (string line in File.ReadAllLines(path + title))
        {
            table.Add(line.Split('=')[0], line.Split('=')[1]);
        }
    }
    /// <summary>
    /// Erstellt die Config-Datei
    /// </summary>
    private void CreateFile()
    {
        if (!File.Exists(path + title))
        {
            File.Create(path + title);
        }
    }
    /// <summary>
    /// Löscht alle Key-Value-Paare und hinterlässt eine leere Datei
    /// </summary>
    public void DeleteAll()
    {
        //table = new Hashtable();
        table = new Dictionary<string, string>();
        if (File.Exists(path + title))
        {
            File.WriteAllText(path + title, "");
        }
    }
    /// <summary>
    /// Löscht das Key-Value-Paar
    /// </summary>
    /// <param name="key"></param>
    public void DeleteKey(string key)
    {
        table.Remove(key);
    }
    /// <summary>
    /// Prüft ob in der Config das Key-Value-Paar vorhanden ist
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool HasKey(string key)
    {
        return table.ContainsKey(key);
    }
    /// <summary>
    /// Speichert die Config ab.
    /// </summary>
    public void Save()
    {
        string lines = "";
        foreach (string key in table.Keys)
        {
            lines += key + "=" + table[key] + "\n";
        }
        /*foreach (DictionaryEntry entry in table)
        {
            lines += entry.Key + "=" + entry.Value + "\n";
        }*/
        if (lines.Length > 2)
            lines = lines.Substring(0, lines.Length - "\n".Length);

        File.WriteAllText(path + title, lines);
        Logging.log(Logging.LogType.Normal, "ConfigFile", "Save", title + " wurde gespeichert. \n"+lines);
    }
    /// <summary>
    /// Fügt ein neues Key-Value-Paar hinzu.
    /// Falls unter diesem Key bereits ein Paar zufinden ist,
    /// wird das value dazu überschrieben.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    private void Set(string key, string value)
    {
        if (table.ContainsKey(key))
            table[key] = value;
        else
            table.Add(key, value);
    }
    /// <summary>
    /// Gibt ein float Value des Key-Value-Paar zurück.
    /// Falls kein Paar unter diesem Key gespeichert ist,
    /// wird 0.0f zurückgegeben.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue">Falls kein Value gefunden wurde</param>
    /// <returns></returns>
    public float GetFloat(string key, float defaultValue)
    {
        try
        {
            return float.Parse((string)table[key]);
        }
        catch
        {
            return defaultValue;
        }
    }
    /// <summary>
    /// Fügt ein float Key-Value-Paar hinzu
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetFloat(string key, float value)
    {
        Set(key, value.ToString());
    }
    /// <summary>
    /// Gibt ein double Value des Key-Value-Paar zurück.
    /// Falls kein Paar unter diesem Key gespeichert ist,
    /// wird 0.0 zurückgegeben.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue">Falls kein Value gefunden wurde</param>
    /// <returns></returns>
    public double GetDouble(string key, double defaultValue)
    {
        try
        {
            return double.Parse((string)table[key]);
        }
        catch
        {
            return defaultValue;
        }
    }
    /// <summary>
    /// Fügt ein double Key-Value-Paar hinzu
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetDouble(string key, double value)
    {
        Set(key, value.ToString());
    }
    /// <summary>
    /// Gibt ein int Value des Key-Value-Paar zurück.
    /// Falls kein Paar unter diesem Key gespeichert ist,
    /// wird 0 zurückgegeben.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue">Falls kein Value gefunden wurde</param>
    /// <returns></returns>
    public int GetInt(string key, int defaultValue)
    {
        try
        {
            return int.Parse((string)table[key]);
        }
        catch
        {
            return defaultValue;
        }
    }
    /// <summary>
    /// Fügt ein int Key-Value-Paar hinzu
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetInt(string key, int value)
    {
        Set(key, value.ToString());
    }
    /// <summary>
    /// Gibt ein string Value des Key-Value-Paar zurück.
    /// Falls kein Paar unter diesem Key gespeichert ist,
    /// wird null zurückgegeben.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue">Falls kein Value gefunden wurde</param>
    /// <returns></returns>
    public string GetString(string key, string defaultValue)
    {
        try
        {
            string s = (string)table[key];
            if (s.Equals(null))
                return defaultValue;
            else
                return s;
        }
        catch
        {
            return defaultValue;
        }
    }
    /// <summary>
    /// Fügt ein string Key-Value-Paar hinzu
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetString(string key, string value)
    {
        Set(key, value);
    }
    /// <summary>
    /// Gibt ein bool Value des Key-Value-Paar zurück.
    /// Falls kein Paar unter diesem Key gespeichert ist,
    /// wird false zurückgegeben.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue">Falls kein Value gefunden wurde</param>
    /// <returns></returns>
    public bool GetBool(string key, bool defaultValue)
    {
        try
        {
            return bool.Parse((string)table[key]);
        }
        catch
        {
            return defaultValue;
        }
    }
    /// <summary>
    /// Fügt ein bool Key-Value-Paar hinzu
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetBool(string key, bool value)
    {
        Set(key, value.ToString());
    }
}
