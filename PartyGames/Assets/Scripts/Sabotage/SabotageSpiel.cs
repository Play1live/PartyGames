using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SabotageSpiel
{
    public static int minPlayer = 6;
    public static int maxPlayer = 6;
    public static string path = "/Spiele/Sabotage";

    public int spielindex;
    public SabotageDiktat diktat;
    public SabotageSortieren sortieren;
    public SabotageDerZugLuegt derzugluegt;
    public SabotageTabu tabu;
    public SabotageAuswahlstrategie auswahlstrategie;
    public SabotageSloxikon sloxikon;
    // TODO: Zeige per Animation dem Saboteur an das er es ist (mit Sound)

    // https://intromaker.com/duration/under-5-seconds
    public SabotageSpiel()
    {
        Logging.log(Logging.LogType.Debug, "SabotageSpiel", "SabotageSpiel", "Spiele werden geladen.");
        spielindex = 0;
        #region Spiele
        diktat = new SabotageDiktat();                      // s1
        sortieren = new SabotageSortieren();                // s2 + s4
        derzugluegt = new SabotageDerZugLuegt();            // s4 + s5
        tabu = new SabotageTabu();                          // s5 + s3
        auswahlstrategie = new SabotageAuswahlstrategie();  // s2
        sloxikon = new SabotageSloxikon();                  // s1 + s2
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
    public int saboteurTokens;
    public int placedTokens;

    public SabotagePlayer(Player player, GameObject PlayerAnzeige)
    {
        this.player = player;
        this.points = 0;
        this.isSaboteur = false;
        this.wasSaboteur = 0;
        this.saboteurTokens = 100;
        this.placedTokens = 0;

        this.playerAnzeige = new GameObject[7];
        this.playerAnzeige[0] = PlayerAnzeige;  // GameObject
        this.playerAnzeige[1] = PlayerAnzeige.transform.GetChild(0).gameObject; // ServerControl
        this.playerAnzeige[2] = PlayerAnzeige.transform.GetChild(1).gameObject; // IsSaboteur
        this.playerAnzeige[2].SetActive(false);
        this.playerAnzeige[3] = PlayerAnzeige.transform.GetChild(2).gameObject; // Icon
        this.playerAnzeige[3].GetComponent<Image>().sprite = player.icon2.icon;
        this.playerAnzeige[4] = PlayerAnzeige.transform.GetChild(2).GetChild(0).gameObject; // Ausgetabbt
        this.playerAnzeige[4].SetActive(false);
        this.playerAnzeige[5] = PlayerAnzeige.transform.GetChild(3).GetChild(1).gameObject; // Name
        this.playerAnzeige[5].GetComponent<TMP_Text>().text = player.name;
        this.playerAnzeige[6] = PlayerAnzeige.transform.GetChild(3).GetChild(2).gameObject; // Punkte
        this.playerAnzeige[6].GetComponent<TMP_InputField>().text = "" + points;
        if (Config.isServer)
            this.playerAnzeige[6].GetComponent<TMP_InputField>().interactable = true;
        else
            this.playerAnzeige[6].GetComponent<TMP_InputField>().interactable = false;
    }

    public override string ToString()
    {
        return "SabotagePlayer Player: " + player + " Points: " + points + " isSaboteur: " + isSaboteur + " wasSaboteur: " + wasSaboteur + " saboteurTokens: " + saboteurTokens + " placedTokens: " + placedTokens;
    }

    public void SetSaboteur(bool isSaboteur)
    {
        this.playerAnzeige[2].SetActive(isSaboteur);
        this.isSaboteur = isSaboteur;
        if (isSaboteur)
            this.wasSaboteur++;
    }
    public void ClientSetSabo(bool isSaboteur)
    {
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
    public void SetHiddenPoins(int punkte)
    {
        this.points = punkte;
    }
    public void AddHiddenPoins(int punkte)
    {
        this.points += punkte;
    }
    public void HidePunkte()
    {
        this.playerAnzeige[6].GetComponent<TMP_InputField>().text = "";
    }
    public void DeleteImage()
    {
        this.playerAnzeige[3].GetComponent<Image>().sprite = new PlayerIcon().icon;
    }
    public void UpdateImage()
    {
        this.playerAnzeige[3].GetComponent<Image>().sprite = player.icon2.icon;
    }
}
