using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LadeDateien
{
    /**
     * Gibt ein String Array mit allen Dateien im Ordner zur�ck.
     * (Keine Ordner)
     */
    public static string[] listDateien(string path)
    {
        return Directory.GetFiles(path);
    }

    /**
     * Gibt Inhalt der Datei zeilenweise in einem String Array zur�ck.
     */
    public static string[] listInhalt(string path)
    {
        return File.ReadAllLines(path);
    }
}
