using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuizServer : MonoBehaviour
{
    GameObject Frage;
    GameObject[,] SpielerAnzeige;

    // Start is called before the first frame update
    void OnEnable()
    {
        InitAnzeigen();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // TODO: Change Name later
    private void InitAnzeigen()
    {
        // Fragen Anzeige
        Frage = GameObject.Find("Frage");
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 7]; // Anzahl benötigter Elemente
        for (int i = 1; i <= Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + i + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + i + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + i + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + i + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + i + ")/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + i + ")/Punkte"); // Spieler Punkte
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + i + ")/SpielerAntwort"); // Spieler Antwort
        }
    }

    private void UpdateSpieler()
    {

    }
    private void BroadcastUpdateSpieler()
    {

    }
}
