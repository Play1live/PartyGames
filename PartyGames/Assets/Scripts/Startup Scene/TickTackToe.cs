using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickTackToe
{
    /// <summary>
    /// Erstellt eine Liste von freien Felder, mithilfe eines strings mit Daten zu allen Feldern
    /// </summary>
    /// <param name="inhalt">Daten zu allen Feldern</param>
    /// <returns>Liste der freien Felder</returns>
    public static List<int> GetFreieFelder(string inhalt)
    {
        List<int> freieFelder = new List<int>();
        for (int i = 1; i <= 9; i++)
        {
            string feldwert = inhalt.Replace("[" + i + "]", "|").Split('|')[1];
            if (feldwert == "")
                freieFelder.Add(i);
        }
        return freieFelder;
    }
    /// <summary>
    /// Erstellt eine Liste von freien Felder, mithilfe einer Liste von belegten Feldern
    /// </summary>
    /// <param name="belegteFelder"></param>
    /// <returns>Liste der freien Felder</returns>
    public static List<int> GetFreieFelder(List<string> belegteFelder)
    {
        List<int> freieFelder = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (belegteFelder[i] == "")
                freieFelder.Add(i);
        }
        return freieFelder;
    }
    /// <summary>
    /// Extrahiert belegte Felder aus einem string
    /// </summary>
    /// <param name="inhalt">Text aller Felder</param>
    /// <returns>Liste der belegten Felder</returns>
    public static List<string> GetBelegteFelder(string inhalt)
    {
        List<string> belegteFelder = new List<string>();
        for (int i = 1; i <= 9; i++)
        {
            string feldwert = inhalt.Replace("[" + i + "]", "|").Split('|')[1];
            belegteFelder.Add(feldwert);
        }
        return belegteFelder;
    }
    /// <summary>
    /// Fügt alle belegten Felder zusammen
    /// </summary>
    /// <param name="belegteFelder">Liste aller belegten Felder</param>
    /// <returns>gibt die belegten Felder als string zurück</returns>
    public static string PrintBelegteFelder(List<string> belegteFelder)
    {
        string felderText = "";
        for (int i = 1; i <= 9; i++)
        {
            felderText = felderText + "[" + i + "]" + belegteFelder[i - 1] + "[" + i + "]";
        }
        return felderText;
    }
    /// <summary>
    /// Berechnet wie viele Züge der Server getätigt hat
    /// </summary>
    /// <param name="belegteFelder">Liste der belegten Felder</param>
    /// <returns>Gibt zurück wie viel Züge der Server bereits gemacht hat</returns>
    public static int getServerZuege(List<string> belegteFelder)
    {
        int server = 0;
        for (int i = 0; i < belegteFelder.Count; i++)
            if (belegteFelder[i] == "X")
                server++;
        return server;
    }
    /// <summary>
    /// Testet ob das Spiel vorbei ist
    /// </summary>
    /// <param name="freieFelder">Liste der freien Felder</param>
    /// <param name="belegteFelder">Liste der belegten Felder</param>
    /// <returns>Gibt zurück ob das Spiel vorbei ist</returns>
    public static bool CheckForEnd(List<int> freieFelder, List<string> belegteFelder)
    {
        if (freieFelder.Count >= 7)
            return false;
        // Check for end
        bool isend = false;
        // Diag
        if (belegteFelder[0] == belegteFelder[4] && belegteFelder[4] == belegteFelder[8] && belegteFelder[0] != "")
            isend = true;
        if (belegteFelder[2] == belegteFelder[4] && belegteFelder[4] == belegteFelder[6] && belegteFelder[2] != "")
            isend = true;
        // Hori
        if (belegteFelder[0] == belegteFelder[1] && belegteFelder[1] == belegteFelder[2] && belegteFelder[0] != "")
            isend = true;
        if (belegteFelder[3] == belegteFelder[4] && belegteFelder[4] == belegteFelder[5] && belegteFelder[3] != "")
            isend = true;
        if (belegteFelder[6] == belegteFelder[7] && belegteFelder[7] == belegteFelder[8] && belegteFelder[6] != "")
            isend = true;
        // Verti
        if (belegteFelder[0] == belegteFelder[3] && belegteFelder[3] == belegteFelder[6] && belegteFelder[0] != "")
            isend = true;
        if (belegteFelder[1] == belegteFelder[4] && belegteFelder[4] == belegteFelder[7] && belegteFelder[1] != "")
            isend = true;
        if (belegteFelder[2] == belegteFelder[5] && belegteFelder[5] == belegteFelder[8] && belegteFelder[2] != "")
            isend = true;
        if (freieFelder.Count == 0)
            isend = true;

        return isend;
    }
    /// <summary>
    /// Sobald das Spiel vorbei ist, wird berechnet wer gewonnen hat.
    /// </summary>
    /// <param name="belegteFelder">Bekommt alle belegte Felder</param>
    /// <returns>Win: w Lose: l Draw: d</returns>
    public static string getResult(List<string> belegteFelder)
    {
        // Check for playerwin
        bool playerwin = false;
        // Diag
        if (belegteFelder[0] == belegteFelder[4] && belegteFelder[4] == belegteFelder[8] && belegteFelder[0] == "O")
            playerwin = true;
        if (belegteFelder[2] == belegteFelder[4] && belegteFelder[4] == belegteFelder[6] && belegteFelder[2] == "O")
            playerwin = true;
        // Hori
        if (belegteFelder[0] == belegteFelder[1] && belegteFelder[1] == belegteFelder[2] && belegteFelder[0] == "O")
            playerwin = true;
        if (belegteFelder[3] == belegteFelder[4] && belegteFelder[4] == belegteFelder[5] && belegteFelder[3] == "O")
            playerwin = true;
        if (belegteFelder[6] == belegteFelder[7] && belegteFelder[7] == belegteFelder[8] && belegteFelder[6] == "O")
            playerwin = true;
        // Verti
        if (belegteFelder[0] == belegteFelder[3] && belegteFelder[3] == belegteFelder[6] && belegteFelder[0] == "O")
            playerwin = true;
        if (belegteFelder[1] == belegteFelder[4] && belegteFelder[4] == belegteFelder[7] && belegteFelder[1] == "O")
            playerwin = true;
        if (belegteFelder[2] == belegteFelder[5] && belegteFelder[5] == belegteFelder[8] && belegteFelder[2] == "O")
            playerwin = true;

        if (playerwin)
            return "W";

        // Check for serverwin
        bool serverwin = false;
        // Diag
        if (belegteFelder[0] == belegteFelder[4] && belegteFelder[4] == belegteFelder[8] && belegteFelder[0] == "X")
            serverwin = true;
        if (belegteFelder[2] == belegteFelder[4] && belegteFelder[4] == belegteFelder[6] && belegteFelder[2] == "X")
            serverwin = true;
        // Hori
        if (belegteFelder[0] == belegteFelder[1] && belegteFelder[1] == belegteFelder[2] && belegteFelder[0] == "X")
            serverwin = true;
        if (belegteFelder[3] == belegteFelder[4] && belegteFelder[4] == belegteFelder[5] && belegteFelder[3] == "X")
            serverwin = true;
        if (belegteFelder[6] == belegteFelder[7] && belegteFelder[7] == belegteFelder[8] && belegteFelder[6] == "X")
            serverwin = true;
        // Verti
        if (belegteFelder[0] == belegteFelder[3] && belegteFelder[3] == belegteFelder[6] && belegteFelder[0] == "X")
            serverwin = true;
        if (belegteFelder[1] == belegteFelder[4] && belegteFelder[4] == belegteFelder[7] && belegteFelder[1] == "X")
            serverwin = true;
        if (belegteFelder[2] == belegteFelder[5] && belegteFelder[5] == belegteFelder[8] && belegteFelder[2] == "X")
            serverwin = true;

        if (serverwin)
            return "L";

        return "D";
    }
    /// <summary>
    /// Durchsucht alle Felder und teilt diese in Freie und belegte Felder ein
    /// </summary>
    /// <param name="freieFelder">Füllt diese Liste neu</param>
    /// <param name="belegteFelder">Füllt diese Liste neu</param>
    /// <returns>Gibt ein gezogenes Feld zurück</returns>
    public static List<string> ServerZiehen(List<int> freieFelder, List<string> belegteFelder)
    {
        List<int> moeglicheFelder = new List<int>();
        moeglicheFelder.AddRange(freieFelder);

        // Berechnet mögliche Felder
        // Erster Zug
        if (getServerZuege(belegteFelder) == 0)
        {
            moeglicheFelder.Remove(2);
            moeglicheFelder.Remove(4);
            moeglicheFelder.Remove(6);
            moeglicheFelder.Remove(8);
        }
        // Alle anderen Züge
        else
        {
            // Selber Gewinnen
            #region Diagonal \
            int gegner = 0;
            int server = 0;
            if (belegteFelder[0] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[8] == "O")
                gegner++;
            if (belegteFelder[0] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[8] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[0] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(1);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(9);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Diagonal /
            gegner = 0;
            server = 0;
            if (belegteFelder[2] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[6] == "O")
                gegner++;
            if (belegteFelder[2] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[6] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[2] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(3);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(7);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Horizontal - (1)
            gegner = 0;
            server = 0;
            if (belegteFelder[0] == "O")
                gegner++;
            if (belegteFelder[1] == "O")
                gegner++;
            if (belegteFelder[2] == "O")
                gegner++;
            if (belegteFelder[0] == "X")
                server++;
            if (belegteFelder[1] == "X")
                server++;
            if (belegteFelder[2] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[0] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(1);
                }
                else if (belegteFelder[1] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(2);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(3);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Horizontal - (2)
            gegner = 0;
            server = 0;
            if (belegteFelder[3] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[5] == "O")
                gegner++;
            if (belegteFelder[3] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[5] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[3] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(4);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(6);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Horizontal - (3)
            gegner = 0;
            server = 0;
            if (belegteFelder[6] == "O")
                gegner++;
            if (belegteFelder[7] == "O")
                gegner++;
            if (belegteFelder[8] == "O")
                gegner++;
            if (belegteFelder[6] == "X")
                server++;
            if (belegteFelder[7] == "X")
                server++;
            if (belegteFelder[8] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[6] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(7);
                }
                else if (belegteFelder[7] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(8);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(9);
                }
            return Ziehen(moeglicheFelder, belegteFelder);
        }
            #endregion
            #region Vertikal - (1)
            gegner = 0;
            server = 0;
            if (belegteFelder[0] == "O")
                gegner++;
            if (belegteFelder[3] == "O")
                gegner++;
            if (belegteFelder[6] == "O")
                gegner++;
            if (belegteFelder[0] == "X")
                server++;
            if (belegteFelder[3] == "X")
                server++;
            if (belegteFelder[6] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[0] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(1);
                }
                else if (belegteFelder[3] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(4);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(7);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Vertikal - (2)
            gegner = 0;
            server = 0;
            if (belegteFelder[1] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[7] == "O")
                gegner++;
            if (belegteFelder[1] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[7] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[1] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(2);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(8);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Vertikal - (3)
            gegner = 0;
            server = 0;
            if (belegteFelder[2] == "O")
                gegner++;
            if (belegteFelder[5] == "O")
                gegner++;
            if (belegteFelder[8] == "O")
                gegner++;
            if (belegteFelder[2] == "X")
                server++;
            if (belegteFelder[5] == "X")
                server++;
            if (belegteFelder[8] == "X")
                server++;
            if (gegner == 0 && server == 2)
            {
                if (belegteFelder[2] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(3);
                }
                else if (belegteFelder[5] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(6);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(9);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            // Gewinn des Gegners verhindern
            #region Diagonal \
            gegner = 0;
            server = 0;
            if (belegteFelder[0] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[8] == "O")
                gegner++;
            if (belegteFelder[0] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[8] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[0] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(1);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(9);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Diagonal /
            gegner = 0;
            server = 0;
            if (belegteFelder[2] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[6] == "O")
                gegner++;
            if (belegteFelder[2] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[6] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[2] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(3);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(7);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Horizontal - (1)
            gegner = 0;
            server = 0;
            if (belegteFelder[0] == "O")
                gegner++;
            if (belegteFelder[1] == "O")
                gegner++;
            if (belegteFelder[2] == "O")
                gegner++;
            if (belegteFelder[0] == "X")
                server++;
            if (belegteFelder[1] == "X")
                server++;
            if (belegteFelder[2] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[0] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(1);
                }
                else if (belegteFelder[1] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(2);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(3);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Horizontal - (2)
            gegner = 0;
            server = 0;
            if (belegteFelder[3] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[5] == "O")
                gegner++;
            if (belegteFelder[3] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[5] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[3] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(4);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(6);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Horizontal - (3)
            gegner = 0;
            server = 0;
            if (belegteFelder[6] == "O")
                gegner++;
            if (belegteFelder[7] == "O")
                gegner++;
            if (belegteFelder[8] == "O")
                gegner++;
            if (belegteFelder[6] == "X")
                server++;
            if (belegteFelder[7] == "X")
                server++;
            if (belegteFelder[8] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[6] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(7);
                }
                else if (belegteFelder[7] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(8);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(9);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Vertikal - (1)
            gegner = 0;
            server = 0;
            if (belegteFelder[0] == "O")
                gegner++;
            if (belegteFelder[3] == "O")
                gegner++;
            if (belegteFelder[6] == "O")
                gegner++;
            if (belegteFelder[0] == "X")
                server++;
            if (belegteFelder[3] == "X")
                server++;
            if (belegteFelder[6] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[0] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(1);
                }
                else if (belegteFelder[3] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(4);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(7);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Vertikal - (2)
            gegner = 0;
            server = 0;
            if (belegteFelder[1] == "O")
                gegner++;
            if (belegteFelder[4] == "O")
                gegner++;
            if (belegteFelder[7] == "O")
                gegner++;
            if (belegteFelder[1] == "X")
                server++;
            if (belegteFelder[4] == "X")
                server++;
            if (belegteFelder[7] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[1] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(2);
                }
                else if (belegteFelder[4] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(5);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(8);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
            #region Vertikal - (3)
            gegner = 0;
            server = 0;
            if (belegteFelder[2] == "O")
                gegner++;
            if (belegteFelder[5] == "O")
                gegner++;
            if (belegteFelder[8] == "O")
                gegner++;
            if (belegteFelder[2] == "X")
                server++;
            if (belegteFelder[5] == "X")
                server++;
            if (belegteFelder[8] == "X")
                server++;
            if (gegner == 2 && server == 0)
            {
                if (belegteFelder[2] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(3);
                }
                else if (belegteFelder[5] == "")
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(6);
                }
                else
                {
                    moeglicheFelder = new List<int>();
                    moeglicheFelder.Add(9);
                }
                return Ziehen(moeglicheFelder, belegteFelder);
            }
            #endregion
        }
        return Ziehen(moeglicheFelder, belegteFelder);
    }
    /// <summary>
    /// Wählt ein zufälliges Feld aus
    /// </summary>
    /// <param name="moeglicheFelder">Mögliche Felder</param>
    /// <param name="belegteFelder">Bereits belegte Felder</param>
    /// <returns></returns>
    private static List<string> Ziehen(List<int> moeglicheFelder, List<string> belegteFelder)
    {
        int serverzug = moeglicheFelder[UnityEngine.Random.Range(0, moeglicheFelder.Count)];
        //freieFelder.Remove(serverzug);
        belegteFelder[serverzug - 1] = "X";
        return belegteFelder;
    }
}
