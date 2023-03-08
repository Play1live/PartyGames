
using System;
using System.IO;
using System.Text;

public class MedienUtil
{
    public static void CreateMediaDirectory()
    {
        // Erstellt die Spiele Ordner
        string[] spielOrdner = { "Quiz", "Listen" };
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

        // Speichert Vorlage f�r Listen
        string listenvorlage = Config.MedienPath+ @"/Spiele/Listen/#Vorlage.txt";
        if (!File.Exists(listenvorlage))
        {
            using (FileStream fs = File.Create(listenvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("SortBy: int\nSortByAnzeige: Wenig - Viel\n- 1 # 1\n- 2 # 2\n- 3 # 3\n- 4 # 4\n- 5 # 5" +
                    "\n- 6 # 6\n- 7 # 7\n- 8 # 8\n- 9 # 9\n- 10 # 10\n- 11 # 11\n- 12 # 12\n- 13 # 13\n- 14 # 14\n- 15 # 15\n- 16 # 16\n- 17 # 17\n- 18 # 18" +
                    "\n- 19 # 19\n- 20 # 20\n- 21 # 21\n- 22 # 22\n- 23 # 23\n- 24 # 24\n- 25 # 25\n- 26 # 26\n- 27 # 27\n- 28 # 28\n- 29 # 29\n- 30 # 30");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
    }

    public static void WriteLogsInDirectory()
    {
        // Erstellt Logs Ordner
        if (!Directory.Exists(Config.MedienPath + @"/Logs"))
            Directory.CreateDirectory(Config.MedienPath + @"/Logs");

        string titel = DateTime.Now.ToString().Replace(":", "-");
        string datum = DateTime.Now.ToString().Split(' ')[1].Replace(":","-");
        string text = "Logs vom "+DateTime.Now.ToString().Split(' ')[0];
        foreach (Logging logs in Config.log)
        {
            // Bestimme Typ
            string type = "";
            if (logs.type == Logging.Type.Normal)
                type = "Normal: ";
            else if (logs.type == Logging.Type.Warning)
                type = "Warning: ";
            else if (logs.type == Logging.Type.Error)
                type = "Error: ";
            else if (logs.type == Logging.Type.Fatal)
                type = "Fatal: ";
            else
                type = "Unkown: ";

            if (logs.exception == null)
            {
                text += "\n[" + type + logs.time + "] "+logs.klasse +" - "+logs.methode+" -> "+logs.msg;
            }
            else
            {
                text += "\n[" + type + logs.time + "] " + logs.klasse + " - " + logs.methode + " -> " + logs.msg +" >> "+ logs.exception;
            }
        }

        using (FileStream fs = File.Create(Config.MedienPath +@"/Logs/"+ titel + ".txt"))
        {
            byte[] info = new UTF8Encoding(true).GetBytes(text);
            
            // Add some information to the file.
            fs.Write(info, 0, info.Length);
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

        // Speichert Vorlage Datei f�r Fischstaebchen
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

        // Speichert Vorlage f�r Listen
        string listenvorlage = Settings.MedienOrdnerPath + @"/Spiele/Listen/#Vorlage.txt";
        if (!File.Exists(listenvorlage))
        {
            using (FileStream fs = File.Create(listenvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("*int\n#Fr�h - Sp�t\n-Aussage # 1940\n-Aussage # 1950\n-Aussage # 1992\n-Aussage # 1999\n-Aussage # 2010" +
                    "\n-Aussage # 2016\n-Aussage # 2019\n-Aussage # 2021\n-Aussage # 2012\n-Aussage # 2003\n+Aussage # 2009\n+Aussage # 2000\n+Aussage # 1994" +
                    "\n+Aussage # 1973\n+Aussage # 1967");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        // Speichert Vorlage f�r Quiz
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

        // Speichert Vorlage f�r WWMFragen
        string wwmfragenvorlage = Settings.MedienOrdnerPath + @"/Spiele/WWMFragen/#Vorlage.txt";
        if (!File.Exists(wwmfragenvorlage))
        {
            using (FileStream fs = File.Create(wwmfragenvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("\"Frage\"\nA: M�gA\nB: M�gB\nC: M�gC\nD: M�gD\n<A-D>");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        string geheimwoertervorlage = Settings.MedienOrdnerPath + @"/Spiele/Geheimwoerter/#Vorlage.txt";
        if (!File.Exists(geheimwoertervorlage))
        {
            using (FileStream fs = File.Create(geheimwoertervorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("A = ...<#>B = ...<#>C = ...\n[Wort]Wort[#]Kategorie[Wort]Wort[#]Kategorie[Wort][L�sung]L�sungswort[L�sung]\n[Wort]Wort[#]Kategorie[Wort]Wort[#]Kategorie[Wort][L�sung]L�sungswort[L�sung]");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
    }*/
}
