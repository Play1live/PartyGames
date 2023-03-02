using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSpiele
{
    public static void LoadGameFiles()
    {
        Debug.Log("Loading Games...");
        reloadQuiz();
        Debug.Log("Games are ready!");
    }

    public static void reloadQuiz()
    {
        Config.QUIZ_SPIEL = new QuizSpiel();
    }

    /*public static void reloadDerZugLuegt()
    {
        Settings.derzugluegtSpiel = new DerZugLuegtSpiel();
    }

    public static void reloadFlaggen()
    {
        Settings.flaggenSpiel = new FlaggenSpiel();
    }

    public static void reloadListen()
    {
        Settings.listenSpiel = new ListenSpiel();
    }

    public static void reloadWWMFragen()
    {
        Settings.wwmfragenSpiel = new WWMFragenSpiel();
    }

    public static void reloadGeheimwoerter()
    {
        Settings.geheimwoerterSpiel = new GeheimwörterSpiel();
    }
    public static void reloadMosaik()
    {
        Settings.mosaikSpiel = new MosaikSpiel();
    }
    public static void reloadAuktion()
    {
        Settings.auktionSpiel1 = new AuktionSpiel(1);
        Settings.auktionSpiel2 = new AuktionSpiel(2);
    }*/
}
