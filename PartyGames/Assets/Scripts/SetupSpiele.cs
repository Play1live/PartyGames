using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSpiele
{
    public static void LoadGameFiles()
    {
        Logging.add(Logging.Type.Normal, "SetupSpiele", "LoadGameFiles", "Loading Games...");
        reloadQuiz();
        reloadFlaggen();
        reloadListen();
        reloadMosaik();
        Logging.add(Logging.Type.Normal, "SetupSpiele", "LoadGameFiles", "Games are ready!");
    }

    public static void reloadQuiz()
    {
        Config.QUIZ_SPIEL = new QuizSpiel();
    }
    public static void reloadFlaggen()
    {
        Config.FLAGGEN_SPIEL = new FlaggenSpiel();
    }
    public static void reloadListen()
    {
        Config.LISTEN_SPIEL = new ListenSpiel();
    }
    private static void reloadMosaik()
    {
        Config.MOSAIK_SPIEL = new MosaikSpiel();
    }

    /*public static void reloadDerZugLuegt()
    {
        Settings.derzugluegtSpiel = new DerZugLuegtSpiel();
    }
    public static void reloadWWMFragen()
    {
        Settings.wwmfragenSpiel = new WWMFragenSpiel();
    }
    public static void reloadGeheimwoerter()
    {
        Settings.geheimwoerterSpiel = new GeheimwörterSpiel();
    }
    public static void reloadAuktion()
    {
        Settings.auktionSpiel1 = new AuktionSpiel(1);
        Settings.auktionSpiel2 = new AuktionSpiel(2);
    }*/
}
