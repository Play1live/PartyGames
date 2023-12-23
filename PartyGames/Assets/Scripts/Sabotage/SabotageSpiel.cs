using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class SabotageSpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    public static string path = "/Spiele/Sabotage";

    public int spielindex;
    public SabotageDiktat diktat;
    // TODO: verhindern das zu viele Spieler das Spiel starten können

    // https://intromaker.com/duration/under-5-seconds
    public SabotageSpiel()
    {
        Logging.log(Logging.LogType.Debug, "SabotageSpiel", "SabotageSpiel", "Spiele werden geladen.");
        spielindex = 0;
        #region Spiele
        diktat = new SabotageDiktat();  // s1
        // Sortieren (Listen)           // s2 + s4
        // Memory                       // s3
        // Tabu                         // s5 + s3
        // Auswahlstrategie             // s2 + s1

        // erklärungen dazu schreiben
        #endregion
    }
}

public class SabotagePlayer
{
    public Player player;
    public int points;
    public int tokens;
    public bool isSaboteur;

    public SabotagePlayer(Player player)
    {
        this.player = player;
        this.points = 0;
        this.tokens = 0;
        this.isSaboteur = false;
    }
}
