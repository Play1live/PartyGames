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
    public static Color TEAM_YELLOW = new Color(236f, 248f, 29f);
    public static Color TEAM_PINK = new Color(204f, 0f, 165f);
    public static Color TEAM_ORANGE = new Color(221f, 131f, 0f);
    public static Color TEAM_BLACK = new Color(0f, 0f, 0f);
    public static Color TEAM_PURPLE = new Color(142f, 0f, 204f);

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
    string BoardPrint = "";

    public MenschAegerDichNichtBoard(GameObject MapObject, int RunWaySize, int TeamSize, List<MenschAergerDichNichtPlayer> player)
    {
        this.MapObject = MapObject;
        this.player = player;
        RunWay = new List<MenschAegerDichNichtFeld>();
        for (int i = 0; i < RunWaySize; i++)
            RunWay.Add(new MenschAegerDichNichtFeld(MapObject.transform.GetChild(0).GetChild(i).gameObject));
        FillAusfahrten();
        Starts = new List<MenschAergerDichNichtBase>();
        Homes = new List<MenschAergerDichNichtBase>();
        for (int i = 0; i < TeamSize; i++)
        {
            Starts.Add(new MenschAergerDichNichtBase(MapObject.transform.GetChild(1).GetChild(i).gameObject, getTeamByIndex(i), true, this.player[i].PlayerImage, this.player[i].PlayerColor));
            this.player[i].SetStartPos(Starts[i].GetBases());
            Homes.Add(new MenschAergerDichNichtBase(MapObject.transform.GetChild(2).GetChild(i).gameObject, getTeamByIndex(i), false, this.player[i].PlayerImage, this.player[i].PlayerColor));        
        }
        PrintBoard();
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
    public string GetBoardString()
    {
        return this.BoardPrint;
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
    private Sprite PlayerImage;
    private Color PlayerTeam;

    // Runway Aufruf
    public MenschAegerDichNichtFeld(GameObject field)
    {
        this.field = field;
        this.hintergrundFarbe = HintergrundFarbe.NULL;

        this.belegt = false;
        this.PlayerImage = null;
        this.PlayerTeam = new Color();

        UpdateDisplayPlayer();
        SetBackgroundColor();
    }
    // Home Aufruf
    public MenschAegerDichNichtFeld(GameObject field, HintergrundFarbe hintergrundFarbe)
    {
        this.field = field;
        this.hintergrundFarbe = hintergrundFarbe;

        this.belegt = false;
        this.PlayerImage = null;
        this.PlayerTeam = new Color();

        SetBackgroundColor();
    }
    // StartAufruf
    public MenschAegerDichNichtFeld(GameObject field, HintergrundFarbe hintergrundFarbe, Sprite PlayerImage, Color PlayerTeam)
    {
        this.field = field;
        this.hintergrundFarbe = hintergrundFarbe;

        this.belegt = true;
        this.PlayerImage = PlayerImage;
        this.PlayerTeam = PlayerTeam;

        SetBackgroundColor();
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
    public void DisplayPlayer(Sprite PlayerImage, Color PlayerTeam)
    {
        if (PlayerImage == null)
        {
            this.belegt = false;
            this.PlayerImage = null;
            this.PlayerTeam = new Color();
        }
        else
        {
            this.belegt = true;
            this.PlayerImage = PlayerImage;
            this.PlayerTeam = PlayerTeam;
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
    public bool IstBelegt()
    {
        return this.belegt;
    }
    public override string ToString()
    {
        return hintergrundFarbe + "*" + belegt + "*" + PlayerImage + "*" + PlayerTeam;
    }
}

public class MenschAergerDichNichtBase
{
    private List<MenschAegerDichNichtFeld> bases;
    public MenschAergerDichNichtBase(GameObject field, HintergrundFarbe hintergrundFarbe, bool belegt, Sprite PlayerImage, Color PlayerTeam)
    {
        bases = new List<MenschAegerDichNichtFeld>();
        if (belegt == true)
            for (int i = 0; i < 4; i++)
            {
                bases.Add(new MenschAegerDichNichtFeld(field.transform.GetChild(i).gameObject, hintergrundFarbe, PlayerImage, PlayerTeam));
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
    public MenschAegerDichNichtFeld FigurPosition_1 { set; get; }
    public MenschAegerDichNichtFeld FigurPosition_2 { set; get; }
    public MenschAegerDichNichtFeld FigurPosition_3 { set; get; }
    public MenschAegerDichNichtFeld FigurPosition_4 { set; get; }

    public MenschAergerDichNichtPlayer(int gamerid, string name, bool isBot, Sprite PlayerImage)
    {
        this.gamerid = gamerid;
        this.isBot = isBot;
        this.name = name;
        this.PlayerImage = PlayerImage;
        this.PlayerColor = getTeamColor(gamerid);

    }

    public void SetStartPos(List<MenschAegerDichNichtFeld> pos)
    {
        this.FigurPosition_1 = pos[0];
        this.FigurPosition_1.DisplayPlayer(this.PlayerImage, this.PlayerColor);
        this.FigurPosition_2 = pos[1];
        this.FigurPosition_2.DisplayPlayer(this.PlayerImage, this.PlayerColor);
        this.FigurPosition_3 = pos[2];
        this.FigurPosition_3.DisplayPlayer(this.PlayerImage, this.PlayerColor);
        this.FigurPosition_4 = pos[3];
        this.FigurPosition_4.DisplayPlayer(this.PlayerImage, this.PlayerColor);
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