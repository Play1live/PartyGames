using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TabuBoard
{
    // Consts
    public static int minPlayer = 4;
    public static int maxPlayer = 8;

    public static Color TEAM_NULL = new Color(152f, 152f, 152f);
    public static Color TEAM_BLUE = new Color(0f, 42f, 180f);
    public static Color TEAM_GREEN = new Color(0f, 123f, 7f);
    public static Color TEAM_RED = new Color(156f, 0f, 0f);
    public static Color TEAM_YELLOW = new Color(219f, 201f, 0f);
    public static Color TEAM_PINK = new Color(204f, 0f, 165f);
    public static Color TEAM_ORANGE = new Color(221f, 131f, 0f);
    public static Color TEAM_LIGHT_BLUE = new Color(0f, 236f, 255f);
    public static Color TEAM_PURPLE = new Color(142f, 0f, 204f);
    public static Color TEAM_BROWN = new Color(139f, 69f, 19f);
    public string[] TEAM_COLORS = new string[] { "<color=blue>", "<color=green>", "<color=red>", "<color=yellow>", "<color=pink>", "<color=orange>", "<color=black>", "<color=purple>", "<color=brown>" };


    public TabuBoard()
    {
        // TODO: gamepacks einlesen
    }
    private void GenerateHexColorCodes()
    {
        // TODO: für listen oder einzelne elemente mit parameter anpassen
        // Konvertiere die Dezimalwerte in hexadezimale Werte
        string hexR = Mathf.RoundToInt(TEAM_BLUE.r).ToString("X2");
        string hexG = Mathf.RoundToInt(TEAM_BLUE.g).ToString("X2");
        string hexB = Mathf.RoundToInt(TEAM_BLUE.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[0] = "<b><color=#" + hexR + hexG + hexB + ">";
    }
}

public class TabuGamePacks
{

}