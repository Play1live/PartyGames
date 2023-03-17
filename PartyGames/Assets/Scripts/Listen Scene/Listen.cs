
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Listen
{
    private string path;
    private string titel;
    private string sortby;
    private string sortbyText;
    private string quelle;
    private string einheit;
    private List<Element> auswahlElemente;

    private List<Element> alleElemente; // Sortierte Liste mit allen Elementen zum abgleich
    private List<Element> gameElemente; // Füllt sich im Game durch die Spiele

    public Listen(string path)
    {
        this.path = path;
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        this.einheit = "";
        this.auswahlElemente = new List<Element>();

        this.alleElemente = new List<Element>();
        this.gameElemente = new List<Element>();

        foreach (string s in LadeDateien.listInhalt(path))
        {
            try
            {
                // SortBy Angabe
                if (s.StartsWith("SortBy: "))
                {
                    this.sortby = s.Substring("SortBy: ".Length);
                }
                // SortByText Anzeige
                else if (s.StartsWith("SortByAnzeige: "))
                {
                    this.sortbyText = s.Substring("SortByAnzeige: ".Length);
                }
                else if (s.StartsWith("Quelle: "))
                {
                    this.quelle = s.Substring("Quelle: ".Length);
                }
                else if (s.StartsWith("Einheit:"))
                {
                    this.einheit = s.Substring("Einheit:".Length);
                }
                // ListenElement
                else if (s.StartsWith("- "))
                {
                    if (alleElemente.Count > 30)
                    {
                        return;
                    }
                    string tmp = s.Substring(2);
                    string[] split = tmp.Replace(" # ", "|").Split('|');
                    string item = split[0];
                    string sortby = split[1];
                    if (split[1].Contains("-"))
                    {
                        sortby = split[1].Split('-')[0]; // Erweiterbar
                    }
                    string display = split[1];

                    alleElemente.Add(new Element(item, sortby, display, einheit));
                }
                else
                {
                    Debug.LogWarning("Listen.cs ~ Unknown Type: " + s);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Fehler beim Laden von Listen: " + titel + " ->  " + s);
            }
        }

        auswahlElemente.AddRange(alleElemente);
        // Randomizer
        auswahlElemente = ShuffelList(alleElemente);

        // Lösche Daten zu int
        //alleElemente = DatumZuInt(alleElemente);

        Sorting(); // Sortiert Liste
    }
    #region Sorting
    public void Sorting()
    {
        List<string> byNumberASC = new List<string>(); // Erweiterbar
        byNumberASC.Add("Früh - Spät"); byNumberASC.Add("Niedrig - Hoch"); byNumberASC.Add("Kurz - Lang");

        List<string> byNumberDESC = new List<string>(); // Erweiterbar
        byNumberDESC.Add("Schwer - Leicht"); byNumberDESC.Add("Viel - Wenig"); byNumberDESC.Add("Groß - Klein");

        if (sortby.Equals("int"))
        {
            if (byNumberASC.Contains(sortbyText))
            {
                heapSortInt(alleElemente, alleElemente.Count);
            }

            if (byNumberDESC.Contains(sortbyText))
            {
                heapSortInt(alleElemente, alleElemente.Count);
                alleElemente.Reverse();
            }
        }
        else if (sortby.Equals("double"))
        {
            if (byNumberASC.Contains(sortbyText))
            {
                heapSortDouble(alleElemente, alleElemente.Count);
            }

            if (byNumberDESC.Contains(sortbyText))
            {
                heapSortDouble(alleElemente, alleElemente.Count);
                alleElemente.Reverse();
            }
        }

        else
        {
            Debug.LogWarning("No Way to Sort List");
        }
    }

    public List<Element> DatumZuInt(List<Element> alle)
    {
        if (alle[0].getSortBy().Split('.').Length <= 2)
            return alle;

        List<Element> elemente = new List<Element>();
        elemente.AddRange(alle);
        foreach (Element e in elemente)
        {
            e.setSortBy(e.getSortBy().Split('.')[2]+ e.getSortBy().Split('.')[1]+ e.getSortBy().Split('.')[0]);
        }
        return elemente;
    }

    public List<Element> ShuffelList(List<Element> liste)
    {
        List<Element> alte = new List<Element>();
        alte.AddRange(liste);
        List<Element> neueListe = new List<Element>();
        while (alte.Count > 0)
        {
            int ran = Random.Range(0, alte.Count);
            neueListe.Add(alte[ran]);
            alte.RemoveAt(ran);
        }
        return neueListe;
    }

    #endregion


    public string getPath() { return this.path; }
    public string getTitel() { return this.titel; }
    public string getQuelle() { return this.quelle; }
    public string getSortby() { return this.sortby; }
    public string getSortByDisplay() { return this.sortbyText; }
    public List<Element> getAuswahlElemente() { return this.auswahlElemente; }
    public Element removeFromAuswahlElement(Element e) { auswahlElemente.Remove(e); return e; }
    public int getAuswahlElementCount() { return this.auswahlElemente.Count; }
    public List<Element> getAlleElemente() { return this.alleElemente; }
    public List<Element> getGameElemene() { return this.gameElemente; }

    public int getAlleFromAuswahl(Element e)
    {
        for (int i = 0; i < getAlleElemente().Count; i++)
        {
            if (e.getItem() == getAlleElemente()[i].getItem())
                return i;
        }
        return -1;
    }
    
    #region Sort int
    void heapSortInt(List<Element> list, int length)
    {
        for (int i = length / 2 - 1; i >= 0; i--)
            heapifySortByInt(list, length, i);
        for (int i = length - 1; i >= 0; i--)
        {
            Element temp = list[0];
            list[0] = list[i];
            list[i] = temp;
            heapifySortByInt(list, i, 0);
        }
    }
    void heapifySortByInt(List<Element> list, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;
        if (left < n && list[left].getSortByInt() > list[largest].getSortByInt())
            largest = left;
        if (right < n && list[right].getSortByInt() > list[largest].getSortByInt())
            largest = right;
        if (largest != i)
        {
            Element swap = list[i];
            list[i] = list[largest];
            list[largest] = swap;
            heapifySortByInt(list, n, largest);
        }
    }
    #endregion
    #region Sort double
    void heapSortDouble(List<Element> list, int length)
    {
        for (int i = length / 2 - 1; i >= 0; i--)
            heapifySortByDouble(list, length, i);
        for (int i = length - 1; i >= 0; i--)
        {
            Element temp = list[0];
            list[0] = list[i];
            list[i] = temp;
            heapifySortByDouble(list, i, 0);
        }
    }
    void heapifySortByDouble(List<Element> list, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;
        if (left < n && list[left].getSortByDouble() > list[largest].getSortByDouble())
            largest = left;
        if (right < n && list[right].getSortByDouble() > list[largest].getSortByDouble())
            largest = right;
        if (largest != i)
        {
            Element swap = list[i];
            list[i] = list[largest];
            list[largest] = swap;
            heapifySortByDouble(list, n, largest);
        }
    }
    #endregion

}
#region Element
public class Element
{
    private string item;
    private string sortby;
    private string display;

    public Element(string item, string sort, string dis, string einheit)
    {
        this.item = item;
        sortby = sort;
        display = dis;

        // Verhindert die Punktierung
        if (einheit.EndsWith("Jahr"))
        {
            display = sortby;
            return;
        }

        // Datem abbrechen
        if (sort.Split('.').Length == 3)
        {
            // Datum zu int machen
            sortby = sort.Split('.')[2] + sort.Split('.')[1] + sort.Split('.')[0];
            return;
        }

        /// Nummern punktieren
        string parseint = display;
        string nachkommastelle = "";
        if (sortby.Contains("."))
        {
            parseint = sortby.Split('.')[0];
            nachkommastelle = ","+ sortby.Split('.')[1];
        }
        int number = Int32.Parse(parseint);
        string formattedNumber = string.Format("{0:n}", number);
        //string formatted = string.Format("{0:n}", parseint);
        //string formatted = parseint.ToString("N3");
        formattedNumber = formattedNumber.Replace(",00", "");
        //formattedNumber = formattedNumber.Replace(",", ".");

        display = formattedNumber + nachkommastelle + " "+einheit;
    }

    public string getItem() { return item; }
    public string getDisplay() { return display; }
    public string getSortBy() { return sortby; }
    public void setSortBy(string s) { sortby = s; }
    public int getSortByInt() { return Int32.Parse(sortby); }
    public double getSortByDouble() { return Double.Parse(sortby); }
    public float getSortByFLoat() { return float.Parse(sortby); }
}
#endregion
