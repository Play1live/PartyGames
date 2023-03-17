using System.Collections.Generic;

public class WerBietetMehr
{
    private string path;
    private string titel;
    private string quelle;
    private List<string> elemente;

    public WerBietetMehr(string path)
    {
        this.path = path;
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        elemente = new List<string>();

        string[] zeilen = LadeDateien.listInhalt(path);
        foreach (string zeile in LadeDateien.listInhalt(path))
        {
            if (zeile.StartsWith("Quelle: "))
            {
                quelle = zeile.Replace("Quelle:", "");
            }
            else if (zeile.StartsWith("-"))
            {
                elemente.Add(zeile.Replace("-", ""));
            }
        }
    }

    public string getPath() { return this.path; }
    public string getQuelle() { return this.quelle; }
    public string getTitel() { return this.titel; }
    public List<string> getElemente() { return this.elemente; }
    public string getElement(int index) { return this.elemente[index]; }
}
