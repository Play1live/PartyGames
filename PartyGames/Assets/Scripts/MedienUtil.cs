
using System;
using System.IO;
using System.Text;

public class MedienUtil
{
    public static void CreateMediaDirectory()
    {
        // Erstellt die Spiele Ordner
        string[] spielOrdner = { "Quiz", "Listen", "Geheimwörter", "WerBietetMehr", "Auktion", "Mosaik" };
        foreach (string game in spielOrdner)
        {
            if (!Directory.Exists(Config.MedienPath + @"/Spiele/" + game))
                Directory.CreateDirectory(Config.MedienPath + @"/Spiele/" + game);
        }

        #region Quiz
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
        #endregion

        #region Listen
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
        #endregion

        #region Geheimwörter
        string geheimwoertervorlage = Config.MedienPath + @"/Spiele/Geheimwörter/#Vorlage.txt";
        if (!File.Exists(geheimwoertervorlage))
        {
            using (FileStream fs = File.Create(geheimwoertervorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("A = ...<#>B = ...<#>C = ...\nWort[#]Kategorie[Wort]Wort[#]Kategorie[Wort][Lösung]Lösungswort[Lösung]\nWort[#]Kategorie[Wort]Wort[#]Kategorie[Wort][Lösung]Lösungswort[Lösung]");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        #endregion

        #region WerBietetMehr
        string werbietetmehrvorlage = Config.MedienPath + @"/Spiele/WerBietetMehr/#Vorlage.txt";
        if (!File.Exists(werbietetmehrvorlage))
        {
            using (FileStream fs = File.Create(werbietetmehrvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("Quelle: www.google.de/vertraumirbruder\n- Element\n- (bis zu 30Elemente)");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        #endregion

        #region Auktion
        string auktiovorlage = Config.MedienPath + @"/Spiele/Auktion/#Vorlage.txt";
        if (!File.Exists(auktiovorlage))
        {
            using (FileStream fs = File.Create(auktiovorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(""); // TODO
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        #endregion

        #region Mosaik
        string mosaikvorlage = Config.MedienPath + @"/Spiele/Mosaik/#Vorlage.txt";
        if (!File.Exists(mosaikvorlage))
        {
            using (FileStream fs = File.Create(mosaikvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("- Name [!#!] URL\n- Name [!#!] URL\n- Name [!#!] URL");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        #endregion
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
}
