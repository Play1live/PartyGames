using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenschAegerDichNichtBoard
{
    // Consts
    public static int minPlayer = 1;
    public static int maxPlayer = 8;
    public static string[] botnames = new string[] { "Bot_Peter", "Bot_Hans", "Bot_Olaf", "Bot_Kran", "Bot_Auto", "Bot_Schiff", "Bot_Maus", "Bot_Profi", "Bot_Niemals" };
    public static int bots = 0;
    public static bool watchBots = false;

    public static Color TEAM_NULL = new Color(152f, 152f, 152f);
    public static Color TEAM_BLUE = new Color(0f, 44f, 195f);
    public static Color TEAM_GREEN = new Color(0f, 123f, 7f);
    public static Color TEAM_RED = new Color(156f, 0f, 0f);
    public static Color TEAM_YELLOW = new Color(219f, 201f, 0f);
    public static Color TEAM_PINK = new Color(204f, 0f, 165f);
    public static Color TEAM_ORANGE = new Color(221f, 131f, 0f);
    public static Color TEAM_BLACK = new Color(0f, 0f, 0f);
    public static Color TEAM_PURPLE = new Color(142f, 0f, 204f);
    public string[] TEAM_COLORS = new string[] { "<color=blue>", "<color=green>", "<color=red>", "<color=yellow>", "<color=pink>", "<color=orange>", "<color=black>", "<color=purple>" };

    public static int[] P3_AUSFAHRT_INDEX = new int[] { 26, 6, 16 };
    public static int[] P4_AUSFAHRT_INDEX = new int[] { 0, 36, 12, 24 };
    public static int[] P5_AUSFAHRT_INDEX = new int[] { 0, 20, 40, 30, 10 };
    public static int[] P6_AUSFAHRT_INDEX = new int[] { 30, 0, 40, 50, 20, 10 };
    public static int[] P7_AUSFAHRT_INDEX = new int[] { 30, 40, 50, 60, 20, 10, 0};
    public static int[] P8_AUSFAHRT_INDEX = new int[] { 30, 40, 50, 60, 20, 10, 0, 70};

    // GameVars
    private GameObject MapObject;

    private List<MenschAegerDichNichtFeld> RunWay;
    private List<MenschAergerDichNichtBase> Starts;
    private List<MenschAergerDichNichtBase> Homes;
    private List<MenschAergerDichNichtPlayer> player;
    private MenschAergerDichNichtPlayer playersTurn;
    string BoardPrint = "";

    public MenschAegerDichNichtBoard(GameObject MapObject, int RunWaySize, int TeamSize, List<MenschAergerDichNichtPlayer> player)
    {
        GenerateHexColorCodes();
        this.MapObject = MapObject;
        this.player = player;
        this.RunWay = new List<MenschAegerDichNichtFeld>();
        for (int i = 0; i < RunWaySize; i++)
            RunWay.Add(new MenschAegerDichNichtFeld(MapObject.transform.GetChild(0).GetChild(i).gameObject));
        FillAusfahrten();
        this.Starts = new List<MenschAergerDichNichtBase>();
        this.Homes = new List<MenschAergerDichNichtBase>();
        for (int i = 0; i < TeamSize; i++)
        {
            this.Starts.Add(new MenschAergerDichNichtBase(MapObject.transform.GetChild(1).GetChild(i).gameObject, getTeamByIndex(i), true, this.player[i]));
            this.player[i].SetStartPos(this.Starts[i].GetBases());
            this.Homes.Add(new MenschAergerDichNichtBase(MapObject.transform.GetChild(2).GetChild(i).gameObject, getTeamByIndex(i), false, this.player[i]));
            this.player[i].SetpRunWay(this.RunWay, TeamSize);
            this.player[i].SetStartAndHomeBase(this.Starts[i], this.Homes[i]);
        }
        PrintBoard();
    }

    // Soll bestimmen welcher Spieler dran ist
    public MenschAergerDichNichtPlayer PlayerTurnSelect()
    {
        if (this.playersTurn == null)
        {
            this.playersTurn = this.player[0];
            return playersTurn;
        }
        else
        {
            int indexNextPlayer = (this.player.IndexOf(this.playersTurn) + 1) % this.player.Count;
            this.playersTurn = this.player[indexNextPlayer];
            return this.playersTurn;
        }
    }
    public MenschAergerDichNichtPlayer GetPlayerTurn()
    {
        return this.playersTurn;
    }
    private void FillAusfahrten()
    {
        if (MapObject.name.Equals("3P"))
        {
            for (int i = 0; i < P3_AUSFAHRT_INDEX.Length; i++)
            {
                RunWay[P3_AUSFAHRT_INDEX[i]].SetColor(i);
            }
        }
        else if(MapObject.name.Equals("4P"))
        {
            for (int i = 0; i < P4_AUSFAHRT_INDEX.Length; i++)
            {
                RunWay[P4_AUSFAHRT_INDEX[i]].SetColor(i);
            }
        }
        else if (MapObject.name.Equals("5P"))
        {
            for (int i = 0; i < P5_AUSFAHRT_INDEX.Length; i++)
            {
                RunWay[P5_AUSFAHRT_INDEX[i]].SetColor(i);
            }
        }
        else if (MapObject.name.Equals("6P"))
        {
            for (int i = 0; i < P6_AUSFAHRT_INDEX.Length; i++)
            {
                RunWay[P6_AUSFAHRT_INDEX[i]].SetColor(i);
            }
        }
        else if (MapObject.name.Equals("7P"))
        {
            for (int i = 0; i < P7_AUSFAHRT_INDEX.Length; i++)
            {
                RunWay[P7_AUSFAHRT_INDEX[i]].SetColor(i);
            }
        }
        else if (MapObject.name.Equals("8P"))
        {
            for (int i = 0; i < P8_AUSFAHRT_INDEX.Length; i++)
            {
                RunWay[P8_AUSFAHRT_INDEX[i]].SetColor(i);
            }
        }
    }
    private HintergrundFarbe getTeamByIndex(int index)
    {
        HintergrundFarbe[] enumValues = (HintergrundFarbe[])Enum.GetValues(typeof(HintergrundFarbe));
        foreach (HintergrundFarbe team in enumValues)
            if (((int)team) == index)
                return team;
        return HintergrundFarbe.NULL;
    }
    public void ClearMarkierungen()
    {
        foreach (MenschAegerDichNichtFeld feld in this.RunWay)
        {
            feld.GetFeld().transform.GetChild(2).gameObject.SetActive(false);
        }
        foreach (MenschAergerDichNichtBase bases in this.Homes)
        {
            foreach (MenschAegerDichNichtFeld feld in bases.GetBases())
            {
                feld.GetFeld().transform.GetChild(2).gameObject.SetActive(false);
            }
        }
    }
    public List<MenschAergerDichNichtPlayer> GetPlayerList()
    {
        return this.player;
    }
    public void SetPlayerTurn(MenschAergerDichNichtPlayer player)
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
        hexR = Mathf.RoundToInt(TEAM_BLACK.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_BLACK.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_BLACK.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[6] = "<b><color=#" + hexR + hexG + hexB + ">";

        // Konvertiere die Dezimalwerte in hexadezimale Werte
        hexR = Mathf.RoundToInt(TEAM_PURPLE.r).ToString("X2");
        hexG = Mathf.RoundToInt(TEAM_PURPLE.g).ToString("X2");
        hexB = Mathf.RoundToInt(TEAM_PURPLE.b).ToString("X2");
        // Kombiniere die hexadezimalen Werte, um den Farbcode zu erstellen
        this.TEAM_COLORS[7] = "<b><color=#" + hexR + hexG + hexB + ">";
    }
    public int GetPlayerByName(string name)
    {
        foreach (MenschAergerDichNichtPlayer p in this.player)
        {
            if (p.name == name)
                return p.gamerid;
        }
        return -1;
    }
    public string GetBoardString()
    {
        return this.BoardPrint;
    }
    public List<MenschAegerDichNichtFeld> GetRunWay()
    {
        return this.RunWay;
    }
    public string PrintBoard()
    {
        string board = "[RUNWAYSIZE]" + RunWay.Count + "[RUNWAYSIZE]";
        for (int i = 0; i < RunWay.Count; i++)
        {
            board += "[F" + i + "]" + RunWay[i].ToString() + "[F" + i + "]";
        }
        board += "[TEAMSSIZE]" + Starts.Count + "[TEAMSSIZE]";
        for (int i = 0; i < Starts.Count; i++)
        {
            board += "[T" + i + "]" + Starts[i].GetBases()[0].ToString() + "~" + Homes[i].GetBases()[0].ToString();
            for (int j = 1; j < 4; j++)
            {
                board += "#" + Starts[i].GetBases()[j].ToString() + "~" + Homes[i].GetBases()[j].ToString();
            }
            board += "[T" + i + "]";
        }
        this.BoardPrint = board;
        return board;
    }
}

public class MenschAegerDichNichtFeld
{
    private GameObject field;
    private HintergrundFarbe hintergrundFarbe;

    private bool belegt;
    private MenschAergerDichNichtPlayer Player;
    private Sprite PlayerImage;
    private Color PlayerTeam;

    // Runway Aufruf
    public MenschAegerDichNichtFeld(GameObject field)
    {
        this.field = field;
        this.hintergrundFarbe = HintergrundFarbe.NULL;

        this.belegt = false;
        this.Player = new MenschAergerDichNichtPlayer(-1, "ERROR", false, Resources.Load<Sprite>("Images/ProfileIcons/empty"));
        this.PlayerImage = this.Player.PlayerImage;
        this.PlayerTeam = new Color();

        UpdateDisplayPlayer();
        SetBackgroundColor();
        ClearSelected();
    }
    // Home Aufruf
    public MenschAegerDichNichtFeld(GameObject field, HintergrundFarbe hintergrundFarbe)
    {
        this.field = field;
        this.hintergrundFarbe = hintergrundFarbe;

        this.belegt = false;
        this.Player = new MenschAergerDichNichtPlayer(-1, "ERROR", false, Resources.Load<Sprite>("Images/ProfileIcons/empty"));
        this.PlayerImage = this.Player.PlayerImage;
        this.PlayerTeam = new Color();

        SetBackgroundColor();
        ClearSelected();
    }
    // StartAufruf
    public MenschAegerDichNichtFeld(GameObject field, HintergrundFarbe hintergrundFarbe, MenschAergerDichNichtPlayer player)
    {
        this.field = field;
        this.hintergrundFarbe = hintergrundFarbe;

        this.belegt = true;
        this.Player = player;
        this.PlayerImage = player.PlayerImage;
        this.PlayerTeam = player.PlayerColor;

        SetBackgroundColor();
        ClearSelected();
    }
    
    public void MarkSelectableField()
    {
        this.field.transform.GetChild(2).gameObject.SetActive(true);
    }
    public void UpdateDisplayPlayer()
    {
        if (this.belegt)
        {
            this.field.transform.GetChild(1).gameObject.SetActive(true);
            this.field.transform.GetChild(1).GetComponent<Image>().sprite = this.PlayerImage;
            Color neu = new Color(this.PlayerTeam.r / 255f, this.PlayerTeam.g / 255f, this.PlayerTeam.b / 255f);
            this.field.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = neu;
        }
        else
        {
            this.field.transform.GetChild(1).gameObject.SetActive(false);
        }
    }
    public void DisplayPlayer(MenschAergerDichNichtPlayer player)
    {
        if (player == null)
        {
            this.belegt = false;
            this.Player = player;
            this.PlayerImage = null;
            this.PlayerTeam = new Color();
        }
        else if (player.gamerid == -1)
        {
            this.belegt = false;
            this.Player = player;
            this.PlayerImage = player.PlayerImage;
            this.PlayerTeam = new Color();
        }
        else
        {
            this.belegt = true;
            this.Player = player;
            this.PlayerImage = player.PlayerImage;
            this.PlayerTeam = player.PlayerColor;
        }
       UpdateDisplayPlayer();
    }
    private HintergrundFarbe getTeamByIndex(int index)
    {
        HintergrundFarbe[] enumValues = (HintergrundFarbe[])Enum.GetValues(typeof(HintergrundFarbe));
        foreach (HintergrundFarbe team in enumValues)
            if (((int)team) == index)
                return team;
        return HintergrundFarbe.NULL;
    }
    public void SetColor(int team)
    {
        this.hintergrundFarbe = getTeamByIndex(team);
        SetBackgroundColor();
    }
    private void SetBackgroundColor()
    {
        if (this.hintergrundFarbe.Equals(HintergrundFarbe.NULL))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_NULL.r / 255f, MenschAegerDichNichtBoard.TEAM_NULL.g / 255f, MenschAegerDichNichtBoard.TEAM_NULL.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.BLUE))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_BLUE.r / 255f, MenschAegerDichNichtBoard.TEAM_BLUE.g / 255f, MenschAegerDichNichtBoard.TEAM_BLUE.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.GREEN))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_GREEN.r / 255f, MenschAegerDichNichtBoard.TEAM_GREEN.g / 255f, MenschAegerDichNichtBoard.TEAM_GREEN.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.RED))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_RED.r / 255f, MenschAegerDichNichtBoard.TEAM_RED.g / 255f, MenschAegerDichNichtBoard.TEAM_RED.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.YELLOW))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_YELLOW.r / 255f, MenschAegerDichNichtBoard.TEAM_YELLOW.g / 255f, MenschAegerDichNichtBoard.TEAM_YELLOW.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.PINK))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_PINK.r / 255f, MenschAegerDichNichtBoard.TEAM_PINK.g / 255f, MenschAegerDichNichtBoard.TEAM_PINK.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.ORANGE))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_ORANGE.r / 255f, MenschAegerDichNichtBoard.TEAM_ORANGE.g / 255f, MenschAegerDichNichtBoard.TEAM_ORANGE.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.BLACK))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_BLACK.r / 255f, MenschAegerDichNichtBoard.TEAM_BLACK.g / 255f, MenschAegerDichNichtBoard.TEAM_BLACK.b / 255f);
        else if (this.hintergrundFarbe.Equals(HintergrundFarbe.PURPLE))
            this.field.transform.GetChild(0).GetComponent<Image>().color = new Color(MenschAegerDichNichtBoard.TEAM_PURPLE.r / 255f, MenschAegerDichNichtBoard.TEAM_PURPLE.g / 255f, MenschAegerDichNichtBoard.TEAM_PURPLE.b / 255f);
    }
    private void ClearSelected()
    {
        this.field.transform.GetChild(2).gameObject.SetActive(false);
    }
    public bool IstBelegt()
    {
        return this.belegt;
    }
    public bool IstMarkiert()
    {
        if (this.field.transform.GetChild(2).gameObject.activeInHierarchy)
            return true;
        else
            return false;
    }
    public MenschAergerDichNichtPlayer GetPlayer()
    {
        return this.Player;
    }
    public GameObject GetFeld()
    {
        return this.field;
    }
    public override string ToString()
    {
        return hintergrundFarbe + "*" + belegt + "*" + Player;
    }
}

public class MenschAergerDichNichtBase
{
    private List<MenschAegerDichNichtFeld> bases;
    public MenschAergerDichNichtBase(GameObject field, HintergrundFarbe hintergrundFarbe, bool belegt, MenschAergerDichNichtPlayer player)
    {
        bases = new List<MenschAegerDichNichtFeld>();
        if (belegt == true)
            for (int i = 0; i < 4; i++)
            {
                bases.Add(new MenschAegerDichNichtFeld(field.transform.GetChild(i).gameObject, hintergrundFarbe, player));
            }
        else
            for (int i = 0; i < 4; i++)
            {
                bases.Add(new MenschAegerDichNichtFeld(field.transform.GetChild(i).gameObject, hintergrundFarbe));
            }
    }

    public List<MenschAegerDichNichtFeld> GetBases()
    {
        return bases;
    }
}

public class MenschAergerDichNichtPlayer
{
    public int gamerid { private set; get; }
    public bool isBot { private set; get; }
    public string name { private set; get; }
    public Sprite PlayerImage { private set; get; }
    public Color PlayerColor { private set; get; }
    public List<MenschAegerDichNichtFeld> pRunWay { get; set; }
    public MenschAergerDichNichtBase StartBase { get; set; }
    public MenschAergerDichNichtBase HomeBase { get; set; }
    public MenschAegerDichNichtFeld FigurPosition_1 { set; get; }
    public MenschAegerDichNichtFeld FigurPosition_2 { set; get; }
    public MenschAegerDichNichtFeld FigurPosition_3 { set; get; }
    public MenschAegerDichNichtFeld FigurPosition_4 { set; get; }
    public List<MenschAegerDichNichtFeld> Figuren { set; get; }
    public int availableDices { set; get; }
    public int wuerfel { set; get; }
    public int wuerfelCounter { set; get; }
    public int schlagCounter { set; get; }
    public int deathCounter { set; get; }

    public MenschAergerDichNichtPlayer(int gamerid, string name, bool isBot, Sprite PlayerImage)
    {
        this.gamerid = gamerid;
        this.isBot = isBot;
        this.name = name;
        this.PlayerImage = PlayerImage;
        this.PlayerColor = getTeamColor(gamerid);
        this.availableDices = 0;
        this.wuerfel = 0;
        this.wuerfelCounter = 0;
        this.schlagCounter = 0;
        this.deathCounter = 0;
        this.pRunWay = new List<MenschAegerDichNichtFeld>();
    }

    public string Move(MenschAegerDichNichtFeld gewaehltesFeld)
    {
        // gewähltes Feld ist die Ausfahrt
        if (this.pRunWay[0].Equals(gewaehltesFeld))
        {
            foreach (MenschAegerDichNichtFeld figur in this.Figuren)
            {
                if (this.StartBase.GetBases().Contains(figur))
                {
                    // bewegen
                    return MovePlayerToField(this.Figuren.IndexOf(figur), gewaehltesFeld);
                }
            }
        }
        // gewähltes Feld ist auf dem Runway
        else if (this.pRunWay.Contains(gewaehltesFeld))
        {
            int indexPlayer = this.pRunWay.IndexOf(gewaehltesFeld) - this.wuerfel;
            foreach (MenschAegerDichNichtFeld figur in this.Figuren)
            {
                if (this.pRunWay.Contains(figur))
                {
                    if (this.pRunWay.IndexOf(figur) == indexPlayer)
                    {
                        // bewegen
                        return MovePlayerToField(this.Figuren.IndexOf(figur), gewaehltesFeld);
                    }
                }
            }
        }
        // gewähltes Feld ist im Home
        else if (this.HomeBase.GetBases().Contains(gewaehltesFeld))
        {
            int indexPlayer = this.HomeBase.GetBases().IndexOf(gewaehltesFeld) - wuerfel;
            // Spieler ist noch nicht im Haus
            if (indexPlayer < 0)
            {
                int indexRunWayPlayer = this.pRunWay.Count + indexPlayer;
                foreach (MenschAegerDichNichtFeld figur in this.Figuren)
                {
                    if (this.pRunWay.Contains(figur))
                    {
                        if (this.pRunWay.IndexOf(figur) == indexRunWayPlayer)
                        {
                            // bewegen
                            MovePlayerToField(this.Figuren.IndexOf(figur), gewaehltesFeld);
                            return "[C]" + this.gamerid + "[C]" + this.name + "[/COLOR] betritt sein [C]" + this.gamerid + "[C]Haus[/COLOR]![C]"+(this.gamerid+1)%2+"[C][/COLOR]";
                        }
                    }
                }
            }
            // Spieler ist im Haus
            else
            {
                foreach (MenschAegerDichNichtFeld figur in this.Figuren)
                {
                    if (this.HomeBase.GetBases().Contains(figur))
                    {
                        if (this.HomeBase.GetBases().IndexOf(figur) == indexPlayer)
                        {
                            // bewegen
                            return MovePlayerToField(this.Figuren.IndexOf(figur), gewaehltesFeld);
                        }
                    }
                }
            }
        }
        return "";
    }
    private string MovePlayerToField(int figurIndex, MenschAegerDichNichtFeld ziel)
    {
        string ausgabe = "";
        MenschAegerDichNichtFeld figurOld = this.Figuren[figurIndex];
        figurOld.DisplayPlayer(new MenschAergerDichNichtPlayer(-1, "ERROR", false, Resources.Load<Sprite>("Images/ProfileIcons/empty")));
        // Schlägt Spieler auf dem ZielFeld
        if (ziel.IstBelegt())
        {
            int geschlagenefigur = ziel.GetPlayer().Figuren.IndexOf(ziel);
            ziel.GetPlayer().StartBase.GetBases()[geschlagenefigur].DisplayPlayer(ziel.GetPlayer());
            ziel.GetPlayer().Figuren[geschlagenefigur] = ziel.GetPlayer().StartBase.GetBases()[geschlagenefigur];
            ausgabe = "[C]" + this.gamerid + "[C]" + this.name + "[/COLOR] schlägt [C]" + ziel.GetPlayer().gamerid + "[C]" + ziel.GetPlayer().name + "[/COLOR].";

            ziel.GetPlayer().deathCounter++;
            this.schlagCounter++;
        }
        // Bewegt Spieler
        ziel.DisplayPlayer(this);
        this.Figuren[figurIndex] = ziel;
        return ausgabe;
    }
    // bool, sagt ob man laufen kann oder nicht, wenn nicht ist der nächste Spieler dran
    public bool MarkAvailableMoves(int wuerfel)
    {
        this.wuerfel = wuerfel;
        // Prio 1: Freimachen
        if (this.pRunWay[0].GetPlayer().gamerid == this.gamerid)
        {
            // Ist eine Figur überhaupt im Startbereich?
            if (this.StartBase.GetBases()[0].IstBelegt() ||
                this.StartBase.GetBases()[1].IstBelegt() ||
                this.StartBase.GetBases()[2].IstBelegt() ||
                this.StartBase.GetBases()[3].IstBelegt())
            {
                MenschAegerDichNichtFeld nextfeld = GetNextField(this.pRunWay[0], wuerfel);
                if (nextfeld != null)
                {
                    nextfeld.MarkSelectableField();
                    return true;
                }
            }
        }
        // Prio 2: Rausstellen
        if (wuerfel == 6)
        {
            // Ist eine Figur überhaupt im Startbereich?
            if (this.StartBase.GetBases()[0].IstBelegt() || 
                this.StartBase.GetBases()[1].IstBelegt() || 
                this.StartBase.GetBases()[2].IstBelegt() || 
                this.StartBase.GetBases()[3].IstBelegt())
            {
                if (this.pRunWay[0].GetPlayer().gamerid != this.gamerid)
                {
                    this.pRunWay[0].MarkSelectableField();
                    return true;
                }
            }
        }
        /**/// Prio 3: Schlagen
        bool kannschlagen = false;
        for (int i = 0; i < this.Figuren.Count; i++)
        {
            if (!this.StartBase.GetBases().Contains(this.Figuren[i]) && !this.HomeBase.GetBases().Contains(this.Figuren[i]))
            {
                MenschAegerDichNichtFeld nextfeld_F = GetNextField(this.Figuren[i], wuerfel);
                if (nextfeld_F != null)
                {
                    if (nextfeld_F.IstBelegt()) // Feld ist von gegner belegt
                    {
                        nextfeld_F.MarkSelectableField();
                        kannschlagen = true;
                    }
                }
            }
        }
        if (kannschlagen)
            return true; /**/
        // Prio 4: Haus
        bool kanninshaus = false;
        for (int i = 0; i < this.Figuren.Count; i++)
        {
            if (!this.StartBase.GetBases().Contains(this.Figuren[i]) && !this.HomeBase.GetBases().Contains(this.Figuren[i]))
            {
                MenschAegerDichNichtFeld nextfeld_F = GetNextField(this.Figuren[i], wuerfel);
                if (nextfeld_F != null)
                {
                    if (this.HomeBase.GetBases().Contains(nextfeld_F))
                    {
                        nextfeld_F.MarkSelectableField();
                        kanninshaus = true;
                    }
                }
            }
        }
        if (kanninshaus)
            return true;
        // Prio 5.1: im haus nach oben laufen
        bool kannimhauslaufen = false;
        for (int i = 0; i < this.Figuren.Count; i++)
        {
            if (this.HomeBase.GetBases().Contains(this.Figuren[i]))
            {
                MenschAegerDichNichtFeld nextfeld_F = GetNextField(this.Figuren[i], wuerfel);
                if (nextfeld_F != null)
                {
                    nextfeld_F.MarkSelectableField();
                    kannimhauslaufen = true;
                }
            }
        }
        // Priorisiert das im Hauslaufen nur für Bots, bei spielern bleibt beides möglich
        if (this.isBot && kannimhauslaufen)
            return true;
        // Prio 5: normal laufen
        bool kannnormallaufen = false;
        for (int i = 0; i < this.Figuren.Count; i++)
        {
            if (!this.StartBase.GetBases().Contains(this.Figuren[i]) && !this.HomeBase.GetBases().Contains(this.Figuren[i]))
            {
                MenschAegerDichNichtFeld nextfeld_F = GetNextField(this.Figuren[i], wuerfel);
                if (nextfeld_F != null)
                {
                    // TODO nur zum testen
                    //if (nextfeld_F.IstBelegt())
                      //  continue;
                    nextfeld_F.MarkSelectableField();
                    kannnormallaufen = true;
                }
            }
        }
        if (kannnormallaufen || kannimhauslaufen)
            return true;
        // Keine möglichkeit zu laufen
        return false;
    }
    public List<MenschAegerDichNichtFeld> GetAvailableMoves()
    {
        List<MenschAegerDichNichtFeld> moeglicheFelder = new List<MenschAegerDichNichtFeld>();
        foreach (MenschAegerDichNichtFeld feld in this.pRunWay)
        {
            if (feld.IstMarkiert())
                moeglicheFelder.Add(feld);
        }
        foreach (MenschAegerDichNichtFeld feld in this.HomeBase.GetBases())
        {
            if (feld.IstMarkiert())
                moeglicheFelder.Add(feld);
        }
        return moeglicheFelder;
    }
    public MenschAegerDichNichtFeld GetNextField(MenschAegerDichNichtFeld feld, int wuerfel)
    {
        // Figur startet auf dem RunWay
        if (this.pRunWay.Contains(feld))
        {
            int feldindex = this.pRunWay.IndexOf(feld);
            // Zielfeld befindet sich innerhalb der Runde
            if ((feldindex + wuerfel) < this.pRunWay.Count)
            {
                // Zielfeld ist frei
                if (!this.pRunWay[feldindex + wuerfel].IstBelegt())
                    return this.pRunWay[feldindex + wuerfel];
                // Zielfeld ist belegt
                else
                {
                    // Spieler auf dem Feld ist ein Gegner
                    if (this.pRunWay[feldindex + wuerfel].GetPlayer() != this)
                        return this.pRunWay[feldindex + wuerfel];
                    // Spieler auf dem Feld ist man selber -> figur kann nicht laufen
                    else
                        return null;
                }
            }
            // Zielfeld befindet sich außerhalb der Runde (im Haus/Ziel)
            else
            {
                // Haus felder: 0, 1, 2, 3
                int housefied = (feldindex + wuerfel) % this.pRunWay.Count;
                // Zielfeld ist das erste Feld im Haus
                if (housefied >= this.HomeBase.GetBases().Count)
                    return null;

                for (int i = 0; i <= housefied; i++)
                {
                    if (HomeBase.GetBases()[i].IstBelegt())
                        return null;
                }
                return HomeBase.GetBases()[housefied];
            }
        }
        // Figur startet im Haus
        else if (this.HomeBase.GetBases().Contains(feld))
        {
            int homeindex = this.HomeBase.GetBases().IndexOf(feld);
            // Haus felder: 0, 1, 2, 3
            // Spieler läuft im Haus zuweit
            if ((homeindex + wuerfel) >= this.HomeBase.GetBases().Count)
                return null;
            // Spieler will innerhalb des Hauses laufen
            int housefied = homeindex + wuerfel;

            for (int i = homeindex+1; i <= housefied; i++)
            {
                if (HomeBase.GetBases()[i].IstBelegt())
                    return null;
            }
            return HomeBase.GetBases()[housefied];
        }
        return null;
    }
    public bool HasPlayerWon()
    {
        foreach (MenschAegerDichNichtFeld feld in this.HomeBase.GetBases())
        {
            if (!feld.IstBelegt())
                return false;
        }
        return true;
    }
    public bool GetAllInStartOrHome()
    {
        // 3 Mal Würfeln
        // Fall 1: Alle Figuren sind im Start
        if (StartBase.GetBases()[0].IstBelegt() &&
            StartBase.GetBases()[1].IstBelegt() &&
            StartBase.GetBases()[2].IstBelegt() &&
            StartBase.GetBases()[3].IstBelegt())
        {
            return true;
        }
        // Fall 2: Figuren sind nur im Start und im max Home
        int summeStart = 0;
        for (int i = 0; i < 4; i++)
            if (StartBase.GetBases()[i].IstBelegt())
                summeStart++;
        int summeHome = 0;
        for (int i = 0; i < (4 - summeStart); i++)
            if (HomeBase.GetBases()[3 - i].IstBelegt())
                summeHome++;
        if ((summeStart + summeHome) == 4)
        {
            return true;
        }
        // Fall 3: Sonst: 1 mal würfeln
        return false;
    }
    public void SetStartAndHomeBase(MenschAergerDichNichtBase Start, MenschAergerDichNichtBase Home)
    {
        this.StartBase = Start;
        this.HomeBase = Home;
    }
    public void SetpRunWay(List <MenschAegerDichNichtFeld> RunWay, int map)
    {
        #region MapSelect
        int[] ausfahrten;
        if (map == 3)
        {
            ausfahrten = MenschAegerDichNichtBoard.P3_AUSFAHRT_INDEX;
        }
        else if (map == 4)
        {
            ausfahrten = MenschAegerDichNichtBoard.P4_AUSFAHRT_INDEX;
        }
        else if (map == 5)
        {
            ausfahrten = MenschAegerDichNichtBoard.P5_AUSFAHRT_INDEX;
        }
        else if (map == 6)
        {
            ausfahrten = MenschAegerDichNichtBoard.P6_AUSFAHRT_INDEX;
        }
        else if (map == 7)
        {
            ausfahrten = MenschAegerDichNichtBoard.P7_AUSFAHRT_INDEX;
        }
        else if (map == 8)
        {
            ausfahrten = MenschAegerDichNichtBoard.P8_AUSFAHRT_INDEX;
        }
        else
        {
            Logging.log(Logging.LogType.Error, "MenschAergerDichNichtPlayer", "SetpRunWay", "Map konnte nicht geladen werden.");
            return;
        }
        #endregion

        for (int i = 0; i < RunWay.Count; i++)
        {
            int index = (ausfahrten[gamerid] + i) % RunWay.Count;
            this.pRunWay.Add(RunWay[index]);
        }
    }
    public void SetStartPos(List<MenschAegerDichNichtFeld> pos)
    {
        this.FigurPosition_1 = pos[0];
        this.FigurPosition_1.DisplayPlayer(this);
        this.FigurPosition_2 = pos[1];
        this.FigurPosition_2.DisplayPlayer(this);
        this.FigurPosition_3 = pos[2];
        this.FigurPosition_3.DisplayPlayer(this);
        this.FigurPosition_4 = pos[3];
        this.FigurPosition_4.DisplayPlayer(this);

        this.Figuren = new List<MenschAegerDichNichtFeld>();
        this.Figuren.Add(FigurPosition_1);
        this.Figuren.Add(FigurPosition_2);
        this.Figuren.Add(FigurPosition_3);
        this.Figuren.Add(FigurPosition_4);
    }
    private Color getTeamColor(int index)
    {
        if (index == 0)
            return MenschAegerDichNichtBoard.TEAM_BLUE;
        else if (index == 1)
            return MenschAegerDichNichtBoard.TEAM_GREEN;
        else if (index == 2)
            return MenschAegerDichNichtBoard.TEAM_RED;
        else if (index == 3)
            return MenschAegerDichNichtBoard.TEAM_YELLOW;
        else if (index == 4)
            return MenschAegerDichNichtBoard.TEAM_PINK;
        else if (index == 5)
            return MenschAegerDichNichtBoard.TEAM_ORANGE;
        else if (index == 6)
            return MenschAegerDichNichtBoard.TEAM_BLACK;
        else if (index == 7)
            return MenschAegerDichNichtBoard.TEAM_PURPLE;
        else
            return MenschAegerDichNichtBoard.TEAM_NULL;
    }
    public void SetPlayerIntoBot()
    {
        this.isBot = true;
    }
}

public enum HintergrundFarbe
{
    NULL = -1,
    BLUE = 0,
    GREEN = 1,
    RED = 2,
    YELLOW = 3,
    PINK = 4,
    ORANGE = 5,
    BLACK = 6,
    PURPLE = 7
}