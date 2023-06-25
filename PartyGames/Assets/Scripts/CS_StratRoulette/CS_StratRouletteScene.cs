using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CS_StratRouletteScene : MonoBehaviour
{
    [SerializeField] Toggle CTAndT;
    [SerializeField] Toggle CT;
    [SerializeField] Toggle T;
    [SerializeField] Toggle Memory;
    [SerializeField] TMP_Text Available;
    [SerializeField] Toggle AllMaps;
    [SerializeField] Toggle[] Maps;

    [SerializeField] GameObject Anzeige;

    private List<CS_StartRouletteDatensatz> AlleDatensaetze;
    private List<CS_StartRouletteDatensatz> CTDatensaetze;
    private List<CS_StartRouletteDatensatz> TDatensaetze;
    private List<CS_StartRouletteDatensatz> MemoryDatensaetze;
    private bool gedaechtnis;

    [SerializeField] GameObject Einstellungen;
    [SerializeField] AudioMixer audiomixer;

    void OnEnable()
    {
        Utils.EinstellungenStartSzene(Einstellungen, audiomixer, Utils.EinstellungsKategorien.Audio, Utils.EinstellungsKategorien.Grafik);
        Utils.EinstellungenGrafikApply(false);

        AlleDatensaetze = new List<CS_StartRouletteDatensatz>();
        CTDatensaetze = new List<CS_StartRouletteDatensatz>();
        TDatensaetze = new List<CS_StartRouletteDatensatz>();
        MemoryDatensaetze = new List<CS_StartRouletteDatensatz>();

        CTAndT.isOn = true;
        CT.isOn = false;
        T.isOn = false;
        Memory.isOn = true;
        gedaechtnis = Memory.isOn;
        Available.text = "";
        foreach (Toggle map in Maps)
            map.isOn = false;
        AllMaps.isOn = true;
        Anzeige.transform.GetChild(0).GetComponent<TMP_Text>().text = "Titel";
        Anzeige.transform.GetChild(1).GetComponent<TMP_Text>().text = "Beschreibung";
        Anzeige.transform.GetChild(2).GetComponent<TMP_Text>().text = "Infos";
        SetAvailable(0);

        StartCoroutine(LoadDatensaetze());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (Config.APPLICATION_CONFIG != null)
            Config.APPLICATION_CONFIG.Save();
    }

    void Update()
    {
        
    }
    /// <summary>
    /// Spiel Verlassen & Zur�ck in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        SceneManager.LoadScene("Startup");
    }
    /// <summary>
    /// L�dt alle Datens�tze
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadDatensaetze()
    {
        Anzeige.transform.GetChild(0).GetComponent<TMP_Text>().text = "Loading...";
        Anzeige.transform.GetChild(1).GetComponent<TMP_Text>().text = "Loading...";
        Anzeige.transform.GetChild(2).GetComponent<TMP_Text>().text = "Loading...";
        yield return null;

        InitDefaultValues();

        foreach (CS_StartRouletteDatensatz data in AlleDatensaetze)
        {
            if (data.GetSeite().Equals("BOTH") || data.GetSeite().Equals("CT"))
                CTDatensaetze.Add(data);
            if (data.GetSeite().Equals("BOTH") || data.GetSeite().Equals("T"))
                TDatensaetze.Add(data);
        }

        UpdateAvailable();
        yield break;
    }
    /// <summary>
    /// Aktualisiert die Available Anzeige
    /// </summary>
    /// <param name="anzahl"></param>
    private void SetAvailable(int anzahl)
    {
        Available.text = "Verf�gbar: " + anzahl;
    }
    /// <summary>
    /// Aktualisiere die Teameinblendungen
    /// </summary>
    /// <param name="toggle"></param>
    public void UpdateTeams(Toggle toggle)
    {
        if (toggle == CTAndT)
        {
            if (toggle.isOn)
            {
                CT.isOn = false;
                T.isOn = false;
            }
        }
        else if (toggle == CT)
        {
            if (toggle.isOn)
            {
                CTAndT.isOn = false;
                T.isOn = false;
            }
        }
        else if (toggle == T)
        {
            if (toggle.isOn)
            {
                CTAndT.isOn = false;
                CT.isOn = false;
            }
        }
        UpdateAvailable();
    }
    /// <summary>
    /// Aktualisiere die verf�gbaren Games
    /// </summary>
    public void UpdateAvailable()
    {
        MemoryDatensaetze.Clear();
        if (CTAndT.isOn)
            MemoryDatensaetze.AddRange(AlleDatensaetze);
        else if (CT.isOn)
            MemoryDatensaetze.AddRange(CTDatensaetze);
        else if (T.isOn)
            MemoryDatensaetze.AddRange(TDatensaetze);

        SetAvailable(MemoryDatensaetze.Count);
        UpdateMaps();
    }
    /// <summary>
    /// Aktualisiere die verf�gbaren Games nach den Maps
    /// </summary>
    public void UpdateMaps()
    {
        List<CS_StartRouletteDatensatz> neueListe = new List<CS_StartRouletteDatensatz>();
        if (AllMaps.isOn)
        {
            SetAvailable(MemoryDatensaetze.Count);
            return;
        }
        foreach (Toggle map in Maps)
        {
            if (map.isOn)
            {
                foreach (CS_StartRouletteDatensatz datensatz in MemoryDatensaetze)
                {
                    if (datensatz.GetMap().Equals(map.name))
                        neueListe.Add(datensatz);
                }
            }
        }
        MemoryDatensaetze = neueListe;
        SetAvailable(MemoryDatensaetze.Count);

        GetRandomDatensatz();
        return;
    }
    /// <summary>
    /// Togglet das Ged�chtnis
    /// </summary>
    public void ToggleGedaechtnis()
    {
        gedaechtnis = Memory.isOn;
        UpdateAvailable();
    }
    /// <summary>
    /// Togglet die Mapauswahl
    /// </summary>
    /// <param name="toggle"></param>
    public void ToggleMap(Toggle toggle)
    {
        if (AllMaps == toggle)
        {
            if (toggle.isOn)
            {
                foreach (Toggle t in Maps)
                {
                    t.isOn = false;
                }
            }
        }
        else
        {
            if (toggle.isOn)
            {
                AllMaps.isOn = false;
            }
        }
        UpdateAvailable();
    }
    /// <summary>
    /// F�llt geleertes Ged�chtnis
    /// </summary>
    public void ClearGedaechtnis()
    {
        UpdateAvailable();
        Anzeige.transform.GetChild(0).GetComponent<TMP_Text>().text = "Titel";
        Anzeige.transform.GetChild(1).GetComponent<TMP_Text>().text = "Hier klicken zum Einblenden";
        Anzeige.transform.GetChild(2).GetComponent<TMP_Text>().text = "Infos";
    }
    /// <summary>
    /// Blendet zuf�lligen Datensatz ein
    /// </summary>
    public void GetRandomDatensatz()
    {
        if (MemoryDatensaetze.Count == 0)
        {
            Anzeige.transform.GetChild(0).GetComponent<TMP_Text>().text = "Keine weiteren Strats";
            Anzeige.transform.GetChild(1).GetComponent<TMP_Text>().text = "Leere das Ged�chtnis oder deaktiviere es um weitere Strats zu laden.";
            Anzeige.transform.GetChild(2).GetComponent<TMP_Text>().text = "";
            return;
        }
        int random = UnityEngine.Random.Range(0, MemoryDatensaetze.Count);
        DatensatzEinblenden(MemoryDatensaetze[random]);
        if (gedaechtnis)
        {
            MemoryDatensaetze.RemoveAt(random);
            SetAvailable(MemoryDatensaetze.Count);
        }
    }
    /// <summary>
    /// Blendet einen Datensatz ein
    /// </summary>
    /// <param name="datensatz"></param>
    private void DatensatzEinblenden(CS_StartRouletteDatensatz datensatz)
    {
        Anzeige.transform.GetChild(0).GetComponent<TMP_Text>().text = datensatz.GetTitel();
        Anzeige.transform.GetChild(1).GetComponent<TMP_Text>().text = datensatz.GetBeschreibung().Replace("\\n", "\n");
        Anzeige.transform.GetChild(2).GetComponent<TMP_Text>().text = datensatz.GetKurz().Replace("\\n", "\n");
    }
    /// <summary>
    /// F�gt die Standardm��igen Elemente hinzu
    /// </summary>
    private void InitDefaultValues()
    {
        #region T
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]ALLE[MAP][SEITE]T[SEITE][TITEL]Hit and Run[TITEL][BESCHREIBUNG]Nehmt einen Bombenspot ein und kehrt nach der Einnahme zum T-Spawn zur�ck, bevor Sie fortfahren.[BESCHREIBUNG][KURZ]1. Bombspot einnehmen\n2. Alle zum T-Spawn\n3. Alles erlaubt[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Der Boden ist Lava[TITEL][BESCHREIBUNG]Die Person mit der Bombe darf nicht den Boden ber�hren, indem er auf den K�pfen der anderen springt oder auf Hindernissen steht. F�llt der Bombentr�ger auf den Boden, muss dieser zum Spawn zur�ck oder die Bombe darf nicht mehr gelegt werden.[BESCHREIBUNG][KURZ]- Bombentr�ger darf den Boden nicht ber�hren\n- Falls doch, zur�ck zum Spawn oder nicht die Bombe legen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Blitzkrieg[TITEL][BESCHREIBUNG]5x Tec-9 oder 5x P90 + 10 Flashbangs. �bernimm einen Bombenspot mit allen Flashbangs.[BESCHREIBUNG][KURZ]5x Tec9\n5xP90\n10 Flashbangs[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Russischer Pinselrausch[TITEL][BESCHREIBUNG]Haltet W gedr�ckt, kein Anhalten[BESCHREIBUNG][KURZ]Haltet W gedr�ckt\nkein Anhalten[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Crabwalk[TITEL][BESCHREIBUNG]Jeder kauft Duels und duckt sich die ganze Runde.[BESCHREIBUNG][KURZ]- Alle kaufen Duels\n- Nur geduckt laufen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Ein-Mann-Zugang[TITEL][BESCHREIBUNG]An jedem Punkt wo Gegner stehen k�nnten, muss man einzelnd rein gehen.[BESCHREIBUNG][KURZ]Alle nacheinander Pushen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]invulnerable[TITEL][BESCHREIBUNG]Kauft alle nur Negevs/M249s und HE-Granaten. St�rmt mit den Waffen in der Hand einen Spot[BESCHREIBUNG][KURZ]- 5 Negevs/M249s\n- 5 HE-Granaten\n- nur mit Waffen in der Hand laufen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Quick Push[TITEL][BESCHREIBUNG]Wartet bis der Timer 0:15 erreicht hat, bevor ein Bombenspot gest�rmt wird.[BESCHREIBUNG][KURZ]Bis 0:15 warten\ndann pushen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Um die Welt (und wieder zur�ck)[TITEL][BESCHREIBUNG]Lauft zu A. Sobald der erste Kill erzielt wurde, dreht um und das gesamte Team eilt zur�ck zu B. Wiederholt den Vorgang bis ein freier Bombenspot erreicht wurde oder alle CTs eliminiert sind.[BESCHREIBUNG][KURZ]- Rennt auf A\n- Nach einem Kill auf B rennen\n- Wiederholen bis alle Tot sind oder ein Spot frei ist[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Sticky Bomb[TITEL][BESCHREIBUNG]Sobald die Bome gelegt wurde, darf sich keiner mehr bewegen[BESCHREIBUNG][KURZ]Stehenbleiben nach dem Legen der Bombe[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Erschie�ungskommando[TITEL][BESCHREIBUNG]Jeder kauft Scouts/AWPs und schie�t nur, wenn der Kait�t \"Feuer!\" sagt[BESCHREIBUNG][KURZ]- Alle kaufen Scout/AWP\n- Nur gleichzeitig schie�en[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Promi[TITEL][BESCHREIBUNG]W�hlt einen Promi aus den Mitspielern, der Kevlar braucht und keine Waffen nutzen darf. Er muss st�ndig in der N�he seiner Teamkollegen bleiben. Die anderen d�rfen sich Waffenkaufen und m�ssen daf�r sorgen, dass der Promi im Team �ber 70% HP bleibt. Fallen die HP darunter, darf die Runde nicht gewonnen werden.[BESCHREIBUNG][KURZ]- 1 Promi w�hlen\n- Promi darf nur Kevlar haben und muss immer bei den Kollegen bleiben\n- Der Rest muss diesen besch�tzen\n- Darf nicht unter 70% fallen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Die Klapperschlange[TITEL][BESCHREIBUNG]Reise mit deiner Gruppe um die Map. Wechselt permanent den Modus der Glock.[BESCHREIBUNG][KURZ]- Nur Glocks\n- Permanent Einzel- & Salvenfeuer wechseln[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Sei ein Anf�hrer.[TITEL][BESCHREIBUNG]Nur der Bombentr�ger darf laufen. Ihr d�rft die Bombe aber weitergeben. Liegt die Bombe au�er Reichweite, m�ssen alle zum Spawn zur�ck.[BESCHREIBUNG][KURZ]- Nur Bombentr�ger darf laufen\n- Bombe weitergeben erlaubt\n- Liegt die Bombe au�er Reichweite, alle zum Spawn zur�ck[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]T[SEITE][TITEL]Der Faker[TITEL][BESCHREIBUNG]Mid -> A -> Fake plant A 4x[BESCHREIBUNG][KURZ][KURZ]"));
        #region Ancient
        #endregion
        #region Anubis
        #endregion
        #region Inferno
        #endregion
        #region Mirage
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Mirage[MAP][SEITE]T[SEITE][TITEL]Mirage: Plan #1[TITEL][BESCHREIBUNG]T Base -> Mid -> Short -> B -> Apartments -> Underpass -> Connector -> A[BESCHREIBUNG][KURZ][KURZ]"));
        #endregion
        #region Nuke
        #endregion
        #region Overpass
        #endregion
        #region Vertigo
        #endregion
        #region Tuscan
        #endregion
        #region Dust II
        #endregion
        #region Train
        #endregion
        #region Cache
        #endregion
        #region Agency
        #endregion
        #region Office
        #endregion
        #endregion
        #region CT
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]KQLY-Style[TITEL][BESCHREIBUNG]Mach den KQLY-Style, ihr d�rft nur im Springen mit der USP-S schie�en[BESCHREIBUNG][KURZ]- Nur USP-S\n- Springen beim Schie�en[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]BOT Marvin Retake[TITEL][BESCHREIBUNG]Jeder CT muss einen Smoke, eine K�dergranate und eine klassische Bot-Bewaffnung wie Auto-Shotty oder SMGs kaufen und im Spawn campen, bis die Bombe platziert ist. Dann ALLE Smokes und K�dergranaten auf den Bombspot werfen und den Standort zur�ckerobern. Wenn m�glich, versucht mit einem Ninja die Bombe zu entsch�rfen w�hrend die anderen Chaos anrichten.[BESCHREIBUNG][KURZ]- Smoke & K�dergranaten\n- Bot-Bewaffnung: Auto-Shotty oder SMGs\n- Im Spawn campen bis die Bombe plaziert ist\n- Dann st�rmen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Ninja Defuse[TITEL][BESCHREIBUNG]Jeder kauft sich Smokes und Blendgranaten und versucht als Ninja zu entsch�rfen[BESCHREIBUNG][KURZ]- Nur als Ninja entsch�rfen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Stack Party[TITEL][BESCHREIBUNG]Versteckt euch alle auf einem Bombenspot und wartet bis Gegner kommen, dann d�rft ihr euch bewegen.[BESCHREIBUNG][KURZ]- Auf einem Spot stacken\n- Warten bis ein Gegner kommt[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Operation Window Fire![TITEL][BESCHREIBUNG]Nur Mag-7 spielen[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Operation Window Fire![TITEL][BESCHREIBUNG]Nur Mag-7 spielen[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Summer of Five-Seven[TITEL][BESCHREIBUNG]Ihr gesamtes Team h�rte Bryan Adams zu, bevor Sie auf die Baustelle kamen. Nur Five-Sevens. Jedes Mal, wenn Sie einen Kill erzielen, m�ssen Sie im Chat �Summer of Five-Seven� eingeben. Wenn Ihr Team die Runde irgendwie gewinnt, m�ssen alle im Chat �Das waren die besten Tage meines Lebens� eingeben.[BESCHREIBUNG][KURZ]- Nur Five-Seven\nBei jedem Kill \"Summer of Five - Seven\" in den Chat schreiben\n- Bei Rundengewinn: \"Das waren die besten Tage meines Lebens\" in den Chat schreiben\n- Beim Verlieren: \"Das waren die schlimmsten Tage meines Lebens\" in den Chat schreiben[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Verschlafen[TITEL][BESCHREIBUNG]Wartet am Spawn bis die Bombe plaziert wurde, beginnt dann die Runde.[BESCHREIBUNG][KURZ]- Am Spawn warten bis die Bombe gelegt wurde\n- Dann die Runde beginnen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Verschlafen[TITEL][BESCHREIBUNG]Wartet am Spawn bis die Bombe plaziert wurde, beginnt dann die Runde.[BESCHREIBUNG][KURZ]- Am Spawn warten bis die Bombe gelegt wurde\n- Dann die Runde beginnen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Der Bienenstock[TITEL][BESCHREIBUNG]1) Jeder kauft eine Waffe mit hoher Feuerrate (P90, Mac10, MP9) und versteckt sich in einem Teil der Karte, aber in der N�he der Standorte/Geiseln. 2) schreibt \"BZZZZZZ\" in den Chat. 3) St�rme das gegnerische Team wie ein Bienenschwarm und feuere als Gruppe.[BESCHREIBUNG][KURZ]- Hohe Feuerraten\nVerstecken\n \"BZZZZZZZ\" in Chat\n- Gleichzeitig st�rmen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Schei�e auf meine Einheit[TITEL][BESCHREIBUNG]1 Person kauft einen M249, alle anderen den M4s und das Team teilt sich auf, wobei eine Person alleine unterwegs ist. Diese Person darf sich die Minikarte nicht ansehen.[BESCHREIBUNG][KURZ]- Ein Einzelg�gner mit M249\n- Der Rest mit M4[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Bisonherde[TITEL][BESCHREIBUNG]Jeder muss Bizons kaufen. Laufen oder Stillstehen ist nicht erlaubt.[BESCHREIBUNG][KURZ]- Nur PP-Bison\n- Immer leise laufen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]CT[SEITE][TITEL]Negev 4-Win[TITEL][BESCHREIBUNG]W�hlt einen Standort und stellt euch mit Negevs hin. Verteidigt den Ort so gut wie m�glich. Keiner darf eine andere Waffe als eine Negev halten.[BESCHREIBUNG][KURZ]- Nur auf Bombspot stehen\n- Nur Negev[KURZ]"));
        #region Ancient
        #endregion
        #region Anubis
        #endregion
        #region Inferno
        #endregion
        #region Mirage
        #endregion
        #region Nuke
        #endregion
        #region Overpass
        #endregion
        #region Vertigo
        #endregion
        #region Tuscan
        #endregion
        #region Dust II
        #endregion
        #region Train
        #endregion
        #region Cache
        #endregion
        #region Agency
        #endregion
        #region Office
        #endregion
        #endregion
        #region BOTH
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Desert Eagle[TITEL][BESCHREIBUNG]Alle Spielen nur die Desert Eagle, mit Kevler ohne Helm.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Spawn Camp[TITEL][BESCHREIBUNG]Niemand darf den Spawn verlassen[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Einer f�r Alle[TITEL][BESCHREIBUNG]Dank der Waffenkontrolle kann dein Team nur 1 Schrotflinte, 1 Pistole, 1 Scharfsch�tzengewehr, 1 Messer und Granaten verwenden. Der Spieler mit den meisten Assists bestimmt eine Waffe f�r jeden Spieler, muss aber das Messer (+Granaten) f�r sich selber verwenden.[BESCHREIBUNG][KURZ]- 1 Schrotflinte\n1 Pistole\n1 Sniper\n1 Messer und Granaten[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Sowjetische Taktik[TITEL][BESCHREIBUNG]Eine Person kauft eine Waffe und alle anderen folgen ihm und heben die Waffe auf, wenn er stirbt.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Gehen Sie es langsam an[TITEL][BESCHREIBUNG]Sie k�nnen nur Scouts und AWPs verwenden. Lauft im Permanenten zoom.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Nur einmaliges Tippen[TITEL][BESCHREIBUNG]Ihr k�nnen jeweils nur einen Schuss abfeuern. Wenn jemand verfehlen, m�ssen er die Waffe fallen lassen oder nachladen. Man kann die Waffe wieder einsammeln, es gelten jedoch weiterhin die gleichen Regeln.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]420 %[TITEL][BESCHREIBUNG]nur f�r Skillshot-Sprungsch�sse.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Taubstumm[TITEL][BESCHREIBUNG]Keine Kommunikation erlaubt![BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Verdiene dir deine R�stung[TITEL][BESCHREIBUNG]Gewinne eine Runde. Wenn dein Team die Runde verliert, kann es in der n�chsten Runde keine R�stung mehr kaufen[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Soziale Distanzierung[TITEL][BESCHREIBUNG]Es ist nicht gestattet, dass sich zwei Spieler gleichzeitig im selben Raum aufhalten und einen Abstand von mindestens 1,5 Metern einhalten.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Onkel Richie Jr.[TITEL][BESCHREIBUNG]Nur K�rpersch�sse erlaubt. Teamkollegen, die (absichtlich oder unabsichtlich) durch Kopfsch�sse get�tet werden, k�nnen die n�chste Runde nicht kaufen.[BESCHREIBUNG][KURZ]- Nur Kopfsch�ssse\n- Stirbt ein Teammitglied durch einen Kopfschuss, darf er in der n�chsten Runde nicht kaufen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]R�ckw�rtsgang![TITEL][BESCHREIBUNG]Ihr d�rft nur R�ckw�rsts laufen[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Die goldene Waffe[TITEL][BESCHREIBUNG]Die Waffe mit der der erste Kill gemacht wird, muss verwendet werden um alle anderen zu T�ten. Die anderen Spieler m�ssen nach dem ersten Kill ihre Waffen wegwerfen und sich die goldene Waffe teilen.[BESCHREIBUNG][KURZ][KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Gott[TITEL][BESCHREIBUNG]Ihr d�rft nur die ZEUS verwenden. Habt ihr mit einer daneben geschossen, d�rft ihr die USP-S oder Glock verwenden[BESCHREIBUNG][KURZ]- Nur ZEUS\n- Nach der Verwendung: Nur USP-S oder Glock[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Deagle-Komando[TITEL][BESCHREIBUNG]5x Deagle und 5x K�dergranaten. Ultimative Verwirrung durch 10 Deagle Sounds, die gleichzeitig schie�en[BESCHREIBUNG][KURZ]- Jeder mit Deagle\n- Jeder mit K�dergranate[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Tag auf dem Schie�stand[TITEL][BESCHREIBUNG]Kauft euch alle eine Schrotflinte und immer wenn ihr einen T�tet, m�sst ihr \"AIM\" in den Chat schreiben[BESCHREIBUNG][KURZ]- Nur Schrotflinte\nSchreibe \"AIM\" nach einem Kill in den Chat[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]Robin Hood[TITEL][BESCHREIBUNG]Der reichste Spieler im Team muss Waffen f�r seine Teamkollegen kaufen (so viele wie er kann) und er muss auch seine Waffe abgeben, die er in der vorherigen Runde verwendet hat. Die einzigen Waffen, die der K�ufer verwenden kann, sind Aufsammeln von Get�teten Feinde.[BESCHREIBUNG][KURZ]- reichster Spieler verschenkt Waffen an sein Team\n- Auch seine eigene\n- Er darf nur Waffen von Gegnern aufsammeln und nutzen[KURZ]"));
        AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP]Alle[MAP][SEITE]BOTH[SEITE][TITEL]SuperCalls[TITEL][BESCHREIBUNG]Wenn man einen Feind sieht, muss man seinen Teamkameraden sagen, wie gro� er ist, wie alt er ist, was er tr�gt, welche Haut er hat, welche Schuhgr��e er hat usw.[BESCHREIBUNG][KURZ][KURZ]"));
        #endregion
    }
}

public class CS_StartRouletteDatensatz
{
    // https://strat-roulette.github.io/
    // https://csgo.stratroulettehub.com/
    // [MAP][MAP][SEITE]T[SEITE][TITEL][TITEL][BESCHREIBUNG][BESCHREIBUNG][KURZ][KURZ]
    //AlleDatensaetze.Add(new CS_StartRouletteDatensatz("[MAP][MAP][SEITE]T[SEITE][TITEL][TITEL][BESCHREIBUNG][BESCHREIBUNG][KURZ][KURZ]"));
        
    private string map;
    private string seite;
    private string titel;
    private string beschreibung;
    private string kurz;

    public CS_StartRouletteDatensatz(string map, string seite, string titel, string beschreibung, string kurz)
    {
        this.map = map;
        this.seite = seite;
        this.titel = titel;
        this.beschreibung = beschreibung;
        this.kurz = kurz;
    }
    public CS_StartRouletteDatensatz(string inhalt)
    {
        this.map = inhalt.Replace("[MAP]", "|").Split('|')[1];
        this.seite = inhalt.Replace("[SEITE]", "|").Split('|')[1];
        this.titel = inhalt.Replace("[TITEL]", "|").Split('|')[1];
        this.beschreibung = inhalt.Replace("[BESCHREIBUNG]", "|").Split('|')[1];
        this.kurz = inhalt.Replace("[KURZ]", "|").Split('|')[1];
    }

    public string GetMap()
    {
        return this.map;
    }
    public string GetSeite()
    {
        return this.seite;
    }
    public string GetTitel()
    {
        return this.titel;
    }
    public string GetBeschreibung()
    {
        return this.beschreibung;
    }
    public string GetKurz()
    {
        return this.kurz;
    }
}