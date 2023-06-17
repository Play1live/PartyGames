using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSpiele
{
    public static IEnumerator LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "SetupSpiele", "LoadGameFiles", "Loading Games...");
        reloadQuiz();
        yield return null;
        reloadFlaggen();
        yield return null;
        reloadListen();
        yield return null;
        reloadMosaik();
        yield return null;
        reloadGeheimwoerter();
        yield return null;
        reloadWerBietetMehr();
        yield return null;
        reloadAuktion();
        yield return null;
        reloadSloxikon();
        Logging.log(Logging.LogType.Normal, "SetupSpiele", "LoadGameFiles", "Games are ready!");
        yield break;
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
    public static void reloadGeheimwoerter()
    {
        Config.GEHEIMWOERTER_SPIEL = new Geheimw�rterSpiel();
    }
    public static void reloadWerBietetMehr()
    {
        Config.WERBIETETMEHR_SPIEL = new WerBietetMehrSpiel();
    }
    public static void reloadAuktion()
    {
        Config.AUKTION_SPIEL = new AuktionSpiel();
    }
    public static void reloadSloxikon()
    {
        Config.SLOXIKON_SPIEL = new SloxikonSpiel();
    }
}
