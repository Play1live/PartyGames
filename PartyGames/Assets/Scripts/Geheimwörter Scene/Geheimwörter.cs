
using System.Collections.Generic;

public class Geheimwörter
{
    private string path;
    private string dateiname;
    private List<string> code;
    private List<GeheimwörterListe> geheimwoerter;

    public Geheimwörter(string path)
    {
        this.path = path;
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.dateiname = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");

        this.code = new List<string>();
        this.geheimwoerter = new List<GeheimwörterListe>();

        string[] dateiinhalt = LadeDateien.listInhalt(path);
        string[] codewoerter = dateiinhalt[0].Replace("<#>", "|").Split('|');
        code.AddRange(codewoerter);
        for (int i = 1; i < dateiinhalt.Length; i++)
        {
            geheimwoerter.Add(new GeheimwörterListe(dateiinhalt[i]));
        }
    }
    public string getPath() { return this.path; }
    public string getTitel() { return this.dateiname; }
    public List<string> getCode() { return this.code; }
    public List<GeheimwörterListe> getGeheimwörter() { return this.geheimwoerter; }
}

public class GeheimwörterListe
{
    private List<string> woerter;
    private string loesung;

    public GeheimwörterListe(string liste)
    {
        woerter = new List<string>();
        string[] tempwoerter = liste.Replace("[Wort]", "|").Split('|');
        for (int i = 0; i < tempwoerter.Length-1; i++)
        {
            this.woerter.Add(tempwoerter[i]);
        }
        this.loesung = liste.Replace("[Lösung]", "|").Split('|')[1];
    }

    public List<string> getWoerter() { return this.woerter; }
    public string getWorte()
    {
        string worte = this.woerter[0].Replace("[#]", "|").Split('|')[0];
        for (int i = 1; i < this.woerter.Count; i++)
        {
            worte += "\n" + this.woerter[i].Replace("[#]", "|").Split('|')[0];
        }
        return worte;
    }
    public string getKategorien()
    {
        string kategorien = this.woerter[0].Replace("[#]", "|").Split('|')[1];
        for (int i = 1; i < this.woerter.Count; i++)
        {
            kategorien += "\n" + this.woerter[i].Replace("[#]", "|").Split('|')[1];
        }
        return kategorien;
    }
    public string getLoesung() { return this.loesung; }
}
