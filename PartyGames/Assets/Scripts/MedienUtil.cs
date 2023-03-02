
using System.IO;
using System.Text;

public class MedienUtil
{
    public static void CreateMediaDirectory()
    {
        // Erstellt die Spiele Ordner
        string[] spielOrdner = { "Quiz" };
        foreach (string game in spielOrdner)
        {
            if (!Directory.Exists(Config.MedienPath + @"/Spiele/" + game))
                Directory.CreateDirectory(Config.MedienPath + @"/Spiele/" + game);
        }

        // Quiz - Erstellt Vorlage
        string quizvorlage = Config.MedienPath + QuizSpiel.path +@"/#Vorlage.txt";
        if (!File.Exists(quizvorlage))
        {
            using (FileStream fs = File.Create(quizvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("Frage: Das ist meine \"Frage\"? \nAntwort: Haus des  Rundfunks \nInfo: Hier kann was richtig cooles stehen \nFrage: Das ist meine \"Frage\"? \nAntwort: Haus des  Rundfunks \nInfo: Hier kann was richtig cooles stehen");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
    }


    /*public static void CreateMedienDirectory()
    {
        // Flaggenn bleiben in Spieldateien

        // Erstellt die Spiel Ordner
        string[] spielOrdner = { "DerZugLuegt", "Listen", "Quiz", "WWMFragen", "Geheimwoerter" };
        foreach (string game in spielOrdner)
        {
            if (!Directory.Exists(Settings.MedienOrdnerPath + @"/Spiele/" + game))
                Directory.CreateDirectory(Settings.MedienOrdnerPath + @"/Spiele/" + game);
        }

        // Speichert Vorlage Datei für Fischstaebchen
        string fischvorlage = Settings.MedienOrdnerPath + @"/Spiele/DerZugLuegt/#Vorlage.txt";
        if (!File.Exists(fischvorlage))
        {
            using (FileStream fs = File.Create(fischvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("Aussage # falsch\nAussage # wahr\nAussage # wahr\nAussage # falsch" +
                    "\nAussage # falsch\nAussage # falsch\nAussage  # wahr\nAussage # wahr\nAussage # falsch\nAussage # falsch");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        // Speichert Vorlage für Listen
        string listenvorlage = Settings.MedienOrdnerPath + @"/Spiele/Listen/#Vorlage.txt";
        if (!File.Exists(listenvorlage))
        {
            using (FileStream fs = File.Create(listenvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("*int\n#Früh - Spät\n-Aussage # 1940\n-Aussage # 1950\n-Aussage # 1992\n-Aussage # 1999\n-Aussage # 2010" +
                    "\n-Aussage # 2016\n-Aussage # 2019\n-Aussage # 2021\n-Aussage # 2012\n-Aussage # 2003\n+Aussage # 2009\n+Aussage # 2000\n+Aussage # 1994" +
                    "\n+Aussage # 1973\n+Aussage # 1967");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        // Speichert Vorlage für Quiz
        string quizvorlage = Settings.MedienOrdnerPath + @"/Spiele/Quiz/#Vorlage.txt";
        if (!File.Exists(quizvorlage))
        {
            using (FileStream fs = File.Create(quizvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("Frage \" <#!#> Antwort <#!#> Keine Info");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        // Speichert Vorlage für WWMFragen
        string wwmfragenvorlage = Settings.MedienOrdnerPath + @"/Spiele/WWMFragen/#Vorlage.txt";
        if (!File.Exists(wwmfragenvorlage))
        {
            using (FileStream fs = File.Create(wwmfragenvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("\"Frage\"\nA: MögA\nB: MögB\nC: MögC\nD: MögD\n<A-D>");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        string geheimwoertervorlage = Settings.MedienOrdnerPath + @"/Spiele/Geheimwoerter/#Vorlage.txt";
        if (!File.Exists(geheimwoertervorlage))
        {
            using (FileStream fs = File.Create(geheimwoertervorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("A = ...<#>B = ...<#>C = ...\n[Wort]Wort[#]Kategorie[Wort]Wort[#]Kategorie[Wort][Lösung]Lösungswort[Lösung]\n[Wort]Wort[#]Kategorie[Wort]Wort[#]Kategorie[Wort][Lösung]Lösungswort[Lösung]");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
    }*/
}
