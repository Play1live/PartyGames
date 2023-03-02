using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickTackToe
{
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

    public static string PrintBelegteFelder(List<string> belegteFelder)
    {
        string felderText = "";
        for (int i = 1; i <= 9; i++)
        {
            felderText = felderText + "[" + i + "]" + belegteFelder[i - 1] + "[" + i + "]";
        }
        return felderText;
    }

    public static int getServerZuege(List<string> belegteFelder)
    {
        int server = 0;
        for (int i = 0; i < belegteFelder.Count; i++)
            if (belegteFelder[i] == "X")
                server++;
        return server;
    }

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

    private static List<string> Ziehen(List<int> moeglicheFelder, List<string> belegteFelder)
    {
        int serverzug = moeglicheFelder[UnityEngine.Random.Range(0, moeglicheFelder.Count)];
        //freieFelder.Remove(serverzug);
        belegteFelder[serverzug - 1] = "X";
        return belegteFelder;
    }
}
