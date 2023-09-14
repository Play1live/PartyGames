using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupSpiele
{
    public static IEnumerator LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "SetupSpiele", "LoadGameFiles", "Loading Games...");
        Config.QUIZ_SPIEL = new QuizSpiel();
        yield return null;
        Config.FLAGGEN_SPIEL = new FlaggenSpiel();
        yield return null;
        Config.LISTEN_SPIEL = new ListenSpiel();
        yield return null;
        Config.MOSAIK_SPIEL = new MosaikSpiel();
        yield return null;
        Config.GEHEIMWOERTER_SPIEL = new GeheimwörterSpiel();
        yield return null;
        Config.WERBIETETMEHR_SPIEL = new WerBietetMehrSpiel();
        yield return null;
        Config.AUKTION_SPIEL = new AuktionSpiel();
        yield return null;
        Config.SLOXIKON_SPIEL = new SloxikonSpiel();
        yield return null;
        Config.JEOPARDY_SPIEL = new JeopardySpiel();
        yield return null;
        Config.TABU_SPIEL = new TabuSpiel();
        yield return null;
        Logging.log(Logging.LogType.Normal, "SetupSpiele", "LoadGameFiles", "Games are ready!");
        yield break;
    }
}
