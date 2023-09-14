
using System;
using System.IO;
using System.Text;

public class MedienUtil
{
    /// <summary>
    /// Erstellt die für die Gamefiles notwendigen Ordner und Beispieldateien.
    /// </summary>
    public static void CreateMediaDirectory()
    {
        Logging.log(Logging.LogType.Debug, "MediaUtil", "CreateMediaDirectory", "Erstelle benötigte Ordner für Spieldateien");
        // Erstellt die Spiele Ordner
        string[] spielOrdner = { "Quiz", "Listen", "Geheimwörter", "WerBietetMehr", "Auktion", "Mosaik", "Sloxikon", "Jeopardy" };
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
                byte[] info = new UTF8Encoding(true).GetBytes(
                    // 1-10
                    "Frage: 1\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    // 11-20
                    "Frage: 11\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    // 21-30
                    "Frage: 21\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    // 31-40
                    "Frage: 31\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos" +
                    "Frage: Text\nAntwort: Antwort\nInfo: Infos");
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
                byte[] info = new UTF8Encoding(true).GetBytes(
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n" +
                    "- Name <!#!#!> 0,00 <!#!#!> ShopURL <!#!#!> BildUrl1 <!#!#!> BildUrl2 <!#!#!> BildUrl3 <!#!#!> BildUrl4 <!#!#!> BildUrl5\n");
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

        #region Sloxikon
        string sloxikonvorlage = Config.MedienPath + @"/Spiele/Sloxikon/#Vorlage.txt";
        if (!File.Exists(sloxikonvorlage))
        {
            using (FileStream fs = File.Create(sloxikonvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("- Thema [!#!] Antwort\n- Thema [!#!] Antwort");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        #endregion

        #region Jeopardy
        string jeopardyvorlage = Config.MedienPath + @"/Spiele/Jeopardy/#Vorlage.txt";
        if (!File.Exists(jeopardyvorlage))
        {
            using (FileStream fs = File.Create(jeopardyvorlage))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("|~~~|~~~|~~~|~~~|~~~\n|~~~|~~~|~~~|~~~|~~~\n|~~~|~~~|~~~|~~~|~~~\n|~~~|~~~|~~~|~~~|~~~\n|~~~|~~~|~~~|~~~|~~~\n|~~~|~~~|~~~|~~~|~~~"); 
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        #endregion
    }
}
