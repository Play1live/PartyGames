using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;

public class SabotageSpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    public static string path = "/Spiele/Sabotage";

    public int spielindex;
    public SabotageDiktat diktat;
    // TODO: Zeige per Animation dem Saboteur an das er es ist (mit Sound)

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

    public SabotagePlayer getPlayerByPlayer(SabotagePlayer[] list, Player player)
    {
        foreach (var item in list)
            if (item.player == player)
                return item;
        return null;
    }
    public void SendToSaboteur(SabotagePlayer[] list, string msg)
    {
        foreach (var item in list)
        {
            if (item.isSaboteur)
                ServerUtils.SendMSG(msg, item.player, false);
        }
    }
}

public class SabotagePlayer
{
    public GameObject[] playerAnzeige;
    public Player player;
    public int points;
    public bool isSaboteur;
    public int wasSaboteur;

    public SabotagePlayer(Player player, GameObject PlayerAnzeige)
    {
        this.playerAnzeige = new GameObject[7];
        this.playerAnzeige[0] = PlayerAnzeige;  // GameObject
        this.playerAnzeige[1] = PlayerAnzeige.transform.GetChild(0).gameObject; // ServerControl
        this.playerAnzeige[2] = PlayerAnzeige.transform.GetChild(1).gameObject; // IsSaboteur
        this.playerAnzeige[2].SetActive(false);
        this.playerAnzeige[3] = PlayerAnzeige.transform.GetChild(2).gameObject; // Icon
        this.playerAnzeige[4] = PlayerAnzeige.transform.GetChild(2).GetChild(0).gameObject; // Ausgetabbt
        this.playerAnzeige[4].SetActive(false);
        this.playerAnzeige[5] = PlayerAnzeige.transform.GetChild(3).GetChild(1).gameObject; // Name
        this.playerAnzeige[5].GetComponent<TMP_Text>().text = player.name;
        this.playerAnzeige[6] = PlayerAnzeige.transform.GetChild(3).GetChild(2).gameObject; // Punkte
        if (Config.isServer)
            this.playerAnzeige[6].GetComponent<TMP_InputField>().interactable = true;

        this.player = player;
        this.points = 0;
        this.playerAnzeige[6].GetComponent<TMP_InputField>().text = "" + 0;
        this.isSaboteur = false;
        this.wasSaboteur = 0;
    }

    public void SetSaboteur(bool isSaboteur)
    {
        this.playerAnzeige[2].SetActive(isSaboteur);
        this.isSaboteur = isSaboteur;
        if (isSaboteur)
            this.wasSaboteur++;
    }
    public void SetAusgetabbt(bool isAusgetabbt)
    {
        this.playerAnzeige[4].SetActive(isAusgetabbt);
    }
    public void SetPunkte(int punkte)
    {
        this.points = punkte;
        this.playerAnzeige[6].GetComponent<TMP_InputField>().text = "" + punkte;
    }
    public void AddPunkte(int punkte)
    {
        SetPunkte(this.points += punkte);
    }
}
