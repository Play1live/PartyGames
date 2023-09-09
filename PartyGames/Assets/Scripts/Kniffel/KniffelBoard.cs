using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KniffelBoard
{
    // Consts
    public static int minPlayer = 1;
    public static int maxPlayer = 9;

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

    private GameObject Punkteliste;
    private GameObject WuerfelBoard;
    private List<KniffelPlayer> player;
    public KniffelPlayer playersTurn;
    string BoardPrint = "";

    public KniffelBoard(GameObject Punkteliste, GameObject WuerfelBoard, List<KniffelPlayer> player)
    {
        GenerateHexColorCodes();
        this.Punkteliste = Punkteliste;
        this.WuerfelBoard = WuerfelBoard;
        this.player = player;
        foreach (KniffelPlayer p in this.player)
        {
            p.ObenSummeOhneBonus.SetPoints(0, p.PlayerColor);
            p.Bonus.SetPoints(0, p.PlayerColor);
            p.ObenSumme.SetPoints(0, p.PlayerColor);
            p.SummeUntererTeil.SetPoints(0, p.PlayerColor);
            p.SummeObererTeil.SetPoints(0, p.PlayerColor);
            p.EndSumme.SetPoints(0, p.PlayerColor);
        }
    }

    // Soll bestimmen welcher Spieler dran ist
    public KniffelPlayer PlayerTurnSelect()
    {
        if (this.playersTurn == null)
        {
            this.playersTurn = this.player[0];
            BlendeIstDranOutlineEin(this.playersTurn);
            return playersTurn;
        }
        else
        {
            int indexNextPlayer = (this.player.IndexOf(this.playersTurn) + 1) % this.player.Count;
            this.playersTurn = this.player[indexNextPlayer];
            BlendeIstDranOutlineEin(this.playersTurn);
            return this.playersTurn;
        }
    }
    public void BlendeIstDranOutlineEin(KniffelPlayer p)
    {
        foreach (KniffelPlayer player in this.GetPlayerList())
            player.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = false; // Blendet ist dran outline aus
        p.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = true; // Blendet ist dran outline ein
    }
    public KniffelPlayer GetPlayerTurn()
    {
        return this.playersTurn;
    }
    private KniffelHintergrundFarbe getTeamByIndex(int index)
    {
        KniffelHintergrundFarbe[] enumValues = (KniffelHintergrundFarbe[])Enum.GetValues(typeof(KniffelHintergrundFarbe));
        foreach (KniffelHintergrundFarbe team in enumValues)
            if (((int)team) == index)
                return team;
        return KniffelHintergrundFarbe.NULL;
    }
    public List<KniffelPlayer> GetPlayerList()
    {
        return this.player;
    }
    public void SetPlayerTurn(KniffelPlayer player)
    {
        this.playersTurn = player;
    }
    private void GenerateHexColorCodes()
    {
        // Konvertiere die Dezimalwerte in hexadezimale Werte
        string hexR = Mathf.RoundToInt(TEAM_BLUE.r).ToString("X2");
        string hexG = Mathf.RoundToInt(TEAM_BLUE.g).ToString("X2");
        string hexB = Mathf.RoundToInt(TEAM_BLUE.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[0] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_GREEN.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_GREEN.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_GREEN.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[1] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_RED.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_RED.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_RED.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[2] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_YELLOW.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_YELLOW.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_YELLOW.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[3] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_PINK.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_PINK.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_PINK.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[4] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_ORANGE.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_ORANGE.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_ORANGE.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[5] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_LIGHT_BLUE.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_LIGHT_BLUE.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_LIGHT_BLUE.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[6] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_PURPLE.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_PURPLE.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_PURPLE.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[7] = "<b><color=#" + hexR + hexG + hexB + ">";
    }
}

public class KniffelPlayer
{
    public int gamerid { set; get; }
    public string name { set; get; }
    public int availablewuerfe { get; set; }
    public Sprite PlayerImage { set; get; }
    public Color PlayerColor { set; get; }

    public GameObject Punkteliste;

    public KniffelKategorie Einsen; 
    public KniffelKategorie Zweien;
    public KniffelKategorie Dreien;
    public KniffelKategorie Vieren;
    public KniffelKategorie Fuenfen;
    public KniffelKategorie Sechsen;
    public KniffelKategorie ObenSummeOhneBonus;
    public KniffelKategorie Bonus;
    public KniffelKategorie ObenSumme;
    public KniffelKategorie Dreierpasch;
    public KniffelKategorie Viererpasch;
    public KniffelKategorie FullHouse;
    public KniffelKategorie KleineStraﬂe;
    public KniffelKategorie GroﬂeStraﬂe;
    public KniffelKategorie Kniffel;
    public KniffelKategorie Chance;
    public KniffelKategorie SummeUntererTeil;
    public KniffelKategorie SummeObererTeil;
    public KniffelKategorie EndSumme;

    public List<int> safewuerfel;
    public List<int> unsafewuerfe;

    public KniffelPlayer(int gamerid, string name, Sprite PlayerImage, GameObject Punkteliste)
    {
        this.gamerid = gamerid;
        this.name = name;
        this.PlayerImage = PlayerImage;
        this.Punkteliste = Punkteliste;
        this.Punkteliste.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = this.PlayerImage;
        this.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = false; // Blendet ist dran outline aus
        this.Punkteliste.SetActive(true);
        this.PlayerColor = getTeamColor(gamerid);
        this.PlayerColor = new Color(this.PlayerColor.r / 255f, this.PlayerColor.g / 255f, this.PlayerColor.b / 255f);
        this.availablewuerfe = 0;

        for (int i = 1; i < this.Punkteliste.transform.childCount; i++)
        {
            if (this.Punkteliste.transform.GetChild(i).name.Equals("Spacer"))
                continue;
            this.Punkteliste.transform.GetChild(i).GetComponentInChildren<TMP_Text>().color = this.PlayerColor;
        }

        this.Einsen = new KniffelKategorie(this.Punkteliste.transform.GetChild(1).gameObject, true, "Einsen");
        this.Zweien = new KniffelKategorie(this.Punkteliste.transform.GetChild(2).gameObject, true, "Zweien");
        this.Dreien = new KniffelKategorie(this.Punkteliste.transform.GetChild(3).gameObject, true, "Dreien");
        this.Vieren = new KniffelKategorie(this.Punkteliste.transform.GetChild(4).gameObject, true, "Vieren");
        this.Fuenfen = new KniffelKategorie(this.Punkteliste.transform.GetChild(5).gameObject, true, "Fuenfen");
        this.Sechsen = new KniffelKategorie(this.Punkteliste.transform.GetChild(6).gameObject, true, "Sechsen");
        this.ObenSummeOhneBonus = new KniffelKategorie(this.Punkteliste.transform.GetChild(7).gameObject, false, "ObenSummeOhneBonus");
        this.Bonus = new KniffelKategorie(this.Punkteliste.transform.GetChild(8).gameObject, false, "Bonus");
        this.ObenSumme = new KniffelKategorie(this.Punkteliste.transform.GetChild(9).gameObject, false, "ObenSumme");
        this.Dreierpasch = new KniffelKategorie(this.Punkteliste.transform.GetChild(11).gameObject, true, "Dreierpasch");
        this.Viererpasch = new KniffelKategorie(this.Punkteliste.transform.GetChild(12).gameObject, true, "Viererpasch");
        this.FullHouse = new KniffelKategorie(this.Punkteliste.transform.GetChild(13).gameObject, true, "FullHouse");
        this.KleineStraﬂe = new KniffelKategorie(this.Punkteliste.transform.GetChild(14).gameObject, true, "KleineStraﬂe");
        this.GroﬂeStraﬂe = new KniffelKategorie(this.Punkteliste.transform.GetChild(15).gameObject, true, "GroﬂeStraﬂe");
        this.Kniffel = new KniffelKategorie(this.Punkteliste.transform.GetChild(16).gameObject, true, "Kniffel");
        this.Chance = new KniffelKategorie(this.Punkteliste.transform.GetChild(17).gameObject, true, "Chance");
        this.SummeUntererTeil = new KniffelKategorie(this.Punkteliste.transform.GetChild(18).gameObject, false, "SummeUntererTeil");
        this.SummeObererTeil = new KniffelKategorie(this.Punkteliste.transform.GetChild(19).gameObject, false, "SummeObererTeil");
        this.EndSumme = new KniffelKategorie(this.Punkteliste.transform.GetChild(20).gameObject, false, "EndSumme");

        this.safewuerfel = new List<int>();
        this.unsafewuerfe = new List<int>();
    }

    public override string ToString()
    {
        return this.gamerid + "*" + this.name + "*" + PlayerIcon.getIdByName(this.PlayerImage.name) + "*" + this.Einsen.ToString() + "*" + this.Zweien.ToString() + "*" + 
            this.Dreien.ToString() + "*" + this.Vieren.ToString() + "*" +this.Fuenfen.ToString() + "*" +this.Sechsen.ToString() + "*" + 
            this.ObenSummeOhneBonus.ToString() + "*" +this.Bonus.ToString() + "*" + this.ObenSumme.ToString() + "*" +this.Dreierpasch.ToString() + "*" + 
            this.Viererpasch.ToString() + "*" + this.FullHouse.ToString() + "*" + this.KleineStraﬂe.ToString() + "*" + this.GroﬂeStraﬂe.ToString() + "*" + 
            this.Kniffel.ToString() + "*" + this.Chance.ToString() + "*" + this.SummeUntererTeil.ToString() + "*" + this.SummeObererTeil.ToString() + "*" +
            this.EndSumme.ToString();
    }
    private Color getTeamColor(int index)
    {
        if (index == 0)
            return KniffelBoard.TEAM_BLUE;
        else if (index == 1)
            return KniffelBoard.TEAM_GREEN;
        else if (index == 2)
            return KniffelBoard.TEAM_RED;
        else if (index == 3)
            return KniffelBoard.TEAM_YELLOW;
        else if (index == 4)
            return KniffelBoard.TEAM_PINK;
        else if (index == 5)
            return KniffelBoard.TEAM_ORANGE;
        else if (index == 6)
            return KniffelBoard.TEAM_LIGHT_BLUE;
        else if (index == 7)
            return KniffelBoard.TEAM_PURPLE;
        else if (index == 8)
            return KniffelBoard.TEAM_BROWN;
        else
            return KniffelBoard.TEAM_NULL;
    }

    public static KniffelPlayer GetPlayerByName(List<KniffelPlayer> players, string name)
    {
        foreach (KniffelPlayer player in players)
            if (player.name.Equals(name))
                return player;
        return null;
    }
    public static KniffelPlayer GetPlayerById(List<KniffelPlayer> players, int id)
    {
        foreach (KniffelPlayer player in players)
            if (player.gamerid == id)
                return player;
        return null;
    }
    public bool GetPlayerFinished()
    {
        if (!Einsen.used)
            return false;
        if (!Zweien.used)
            return false;
        if (!Dreien.used)
            return false;
        if (!Vieren.used)
            return false;
        if (!Fuenfen.used)
            return false;
        if (!Sechsen.used)
            return false;
        if (!Dreierpasch.used)
            return false;
        if (!Viererpasch.used)
            return false;
        if (!FullHouse.used)
            return false;
        if (!KleineStraﬂe.used)
            return false;
        if (!GroﬂeStraﬂe.used)
            return false;
        if (!Kniffel.used)
            return false;
        if (!Chance.used)
            return false;
        return true;
    }
    public KniffelKategorie GetKategorie(string name)
    {
        switch (name)
        {
            default:
                return null;
            case "Einsen":
                return this.Einsen;
            case "Zweien":
                return this.Zweien;
            case "Dreien":
                return this.Dreien;
            case "Vieren":
                return this.Vieren;
            case "Fuenfen":
                return this.Fuenfen;
            case "Sechsen":
                return this.Sechsen;
            case "ObenSummeOhneBonus":
                return this.ObenSummeOhneBonus;
            case "Bonus":
                return this.Bonus;
            case "ObenSumme":
                return this.ObenSumme;
            case "Dreierpasch":
                return this.Dreierpasch;
            case "Viererpasch":
                return this.Viererpasch;
            case "FullHouse":
                return this.FullHouse;
            case "KleineStraﬂe":
                return this.KleineStraﬂe;
            case "GroﬂeStraﬂe":
                return this.GroﬂeStraﬂe;
            case "Kniffel":
                return this.Kniffel;
            case "Chance":
                return this.Chance;
            case "SummeUntererTeil":
                return this.SummeUntererTeil;
            case "SummeObererTeil":
                return this.SummeObererTeil;
            case "EndSumme":
                return this.EndSumme;
        }
    }
}

public class KniffelKategorie
{
    public GameObject button;
    public bool clickable;
    public bool used;
    public string name;
    public int points;

    public KniffelKategorie(GameObject button, bool clickable, string name)
    {
        this.button = button;
        this.button.GetComponent<Button>().interactable = false;
        this.clickable = clickable;
        if (!clickable)
            this.button.GetComponent<Button>().enabled = false;
        this.used = false;
        this.name = name;
        this.points = 0;
        this.button.GetComponentInChildren<TMP_Text>().text = "";
    }

    public override string ToString()
    {
        return used+"~"+points;
    }
    public void SetPoints(int points, Color farbe)
    {
        this.points = points;
        this.used = true;
        this.button.GetComponent<Button>().interactable = false;
        this.button.GetComponentInChildren<TMP_Text>().color = farbe;
        Display();
    }
    private void Display()
    {
        this.button.GetComponentInChildren<TMP_Text>().text = "" + points;
    }
    public void DisplayTest(int temp) 
    {
        this.button.GetComponentInChildren<TMP_Text>().text = "" + temp;
        this.button.GetComponentInChildren<TMP_Text>().color = Color.black;
    } 
    public int getDisplay()
    {
        if (this.button.GetComponentInChildren<TMP_Text>().text.Length == 0)
            return 0;
        else 
            return Int32.Parse(this.button.GetComponentInChildren<TMP_Text>().text);
    }
}

public enum KniffelHintergrundFarbe
{
    NULL = -1,
    BLUE = 0,
    GREEN = 1,
    RED = 2,
    YELLOW = 3,
    PINK = 4,
    ORANGE = 5,
    LIGHT_BLUE = 6,
    PURPLE = 7,
    BROWN = 8
}