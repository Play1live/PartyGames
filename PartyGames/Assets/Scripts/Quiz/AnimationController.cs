using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimationController : MonoBehaviour
{
    bool started = false;
    [SerializeField] GameObject Controller;
    [SerializeField] GameObject Player1;
    [SerializeField] GameObject Player2;
    [SerializeField] GameObject Player3;
    [SerializeField] GameObject Player4;
    [SerializeField] GameObject Player5;
    [SerializeField] GameObject Player6;
    [SerializeField] GameObject Player7;
    [SerializeField] GameObject Player8;

    [SerializeField] Toggle ZielAnzeigen;
    [SerializeField] TMP_InputField StartWert;
    [SerializeField] TMP_InputField ZielWert;
    [SerializeField] TMP_InputField MaxWert;

    GameObject[] Player;
    bool[] ismoving;
    bool[] israising;

    float Schrittweite;

    DateTime MaxTime;
    DateTime StartTime;

    void OnEnable()
    {
        MaxTime = DateTime.Now.AddSeconds(10);
        StartTime = DateTime.Now;
        Logging.log(Logging.LogType.Normal, "AnimationController", "OnEnable", StartTime + " " + MaxTime);

        Player = new GameObject[8];
        Player[0] = Player1;
        Player[1] = Player2;
        Player[2] = Player3;
        Player[3] = Player4;
        Player[4] = Player5;
        Player[5] = Player6;
        Player[6] = Player7;
        Player[7] = Player8;
        ismoving = new bool[8];
        israising = new bool[8];

        for (int i = 0; i < Player.Length; i++)
        {
            if (!Player[i].activeInHierarchy)
                continue;

            Player[i].GetComponent<Image>().sprite = Config.PLAYERLIST[i].icon2.icon;
            Player[i].transform.GetChild(1).gameObject.SetActive(true);
            setSPIELER_POSITION_X(i, getSTART_POSITION(i));
            if (getSPIELER_WERT(i) < getSTART_WERT(i))
            {
                Player[i].transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = getSPIELER_WERT(i) + getEINHEIT(i);
                Player[i].transform.GetChild(1).gameObject.SetActive(true);
                ismoving[i] = false;
                israising[i] = false;
            }
            else
            {
                Player[i].transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = "0" + getEINHEIT(i);
                ismoving[i] = true;
                israising[i] = true;
            }
        }

        started = true;
        Logging.log(Logging.LogType.Debug, "AnimationController", "OnEnbale", "Start inited");
    }

    private void OnDisable()
    {
        Logging.log(Logging.LogType.Normal, "AnimationController", "OnDisable", "Beendet");
    }

    void Update()
    {
        if (!started)
            return;

        bool minOneisMoving = false;
        for (int i = 0; i < 8; i++)
        {
            if (!ismoving[i])
                continue;
            minOneisMoving = true;
            
            DateTime start = DateTime.Now;
            DateTime max = MaxTime;
            TimeSpan difference = max - start;
            double milliseconds = difference.TotalMilliseconds;
            TimeSpan diff = MaxTime - StartTime;
            double milisdif = diff.TotalMilliseconds;
            double rest = (1 - milliseconds / milisdif);

            // Spieler Bewegen
            Schrittweite = (float) (getDIFF_MAX(i)*rest);
            setSPIELER_POSITION_X(i, Schrittweite+getSTART_POSITION(i));
            // Wert Belegen
            float pos = getSPIELER_POSITION_X(i) + Math.Abs(getSTART_POSITION(i));
            float maxpos = getMAX_POSITION(i) + Math.Abs(getSTART_POSITION(i));
            float relativzumax = pos / maxpos;
            float wert = ((getMAX_WERT(i) - getSTART_WERT(i)) * relativzumax + getSTART_WERT(i));
            string ausgabe = (int)wert + "";
            if (getKOMMASTELLEN(i) != 0)
            {
                int stellenvorne = ("" + (int)wert).Length;
                ausgabe = ("" + wert+"0000000000000000000").Substring(0, getKOMMASTELLEN(i)+stellenvorne+1);
            }
            setSPIELER_DISPLAY(i, "" + ausgabe);

            // Spieler drüber hinaus
            if (getSPIELER_POSITION_X(i) >= getSPIELER_END_POSITION(i))
            {
                ismoving[i] = false;
                setSPIELER_POSITION_X(i, getSPIELER_END_POSITION(i));
                setSPIELER_DISPLAY(i, getSPIELER_WERT(i) + "");
            }
        }
        bool minOnerising = false;

        // Beenden
        if (!minOneisMoving && !minOnerising)
            Controller.SetActive(false);
    }
    private void setSPIELER_DISPLAY(int index, string display)
    {
        Player[index].transform.GetChild(1).GetComponent<TMP_Text>().text = display + getEINHEIT(index);
    }
    private float getSPIELER_DISPLAY(int index)
    {
        return float.Parse(Player[index].transform.GetChild(1).GetComponent<TMP_Text>().text.Replace(getEINHEIT(index), ""));
    }
    private float getSTART_WERT(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[START_WERT]", "|").Split('|')[1]);
    }
    private float getZIEL_WERT(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[ZIEL_WERT]", "|").Split('|')[1]);
    }
    private float getMAX_WERT(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[MAX_WERT]", "|").Split('|')[1]);
    }
    private float getSTART_POSITION(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[START_POSITION]", "|").Split('|')[1]);
    }
    private float getMAX_POSITION(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[MAX_POSITION]", "|").Split('|')[1]);
    }
    private float getDIFF_NULL(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[DIFF_NULL]", "|").Split('|')[1]);
    }
    private float getDIFF_MAX(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[DIFF_MAX]", "|").Split('|')[1]);
    }
    private float getDISTANCE_PER_MOVE(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[DISTANCE_PER_MOVE]", "|").Split('|')[1]);
    }
    private float getSPIELER_WERT(int index)
    {
        return float.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[SPIELER_WERT]", "|").Split('|')[1]);
    }
    private float getSPIELER_POSITION_X(int index)
    {
        return Player[index].transform.localPosition.x;
    }
    private float getSPIELER_POSITION_Y(int index)
    {
        return Player[index].transform.localPosition.y;
    }
    private void setSPIELER_POSITION_X(int index, float x)
    {
        Player[index].transform.localPosition = new Vector3(x, getSPIELER_POSITION_Y(index), 0);
    }
    private void moveSPIELER_POSITION_X(int index, float x)
    {
        Player[index].transform.localPosition = new Vector3(x + getSPIELER_POSITION_X(index), getSPIELER_POSITION_Y(index), 0);
        // Wert Belegen
        float pos = getSPIELER_POSITION_X(index) + Math.Abs(getSTART_POSITION(index));
        float maxpos = getMAX_POSITION(index) + Math.Abs(getSTART_POSITION(index));
        float relativzumax = pos / maxpos;
        float wert = ((getMAX_WERT(index)-getSTART_WERT(index)) * relativzumax + getSTART_WERT(index));
        string ausgabe = (int)wert+"";
        if (getKOMMASTELLEN(index) != 0 && (int)wert < wert)
        {
            int vorne = (int)wert;
            int hinten = (int)((wert - vorne) * (getKOMMASTELLEN(index)*1000));
            string hin = hinten +"000000000000";
            ausgabe = vorne + "," + hin.Substring(0, getKOMMASTELLEN(index));
        }
        setSPIELER_DISPLAY(index, ausgabe);
    }
    private float getSPIELER_END_POSITION(int index)
    {
        float endpos = (getDISTANCE_PER_MOVE(index) * (getSPIELER_WERT(index) - getSTART_WERT(index)) - getDIFF_NULL(index));
        if (endpos > getMAX_POSITION(index))
            endpos = getMAX_POSITION(index);
        return endpos;
    }
    private string getEINHEIT(int index)
    {
        return Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[EINHEIT]", "|").Split('|')[1];
    }
    private int getKOMMASTELLEN(int index)
    {
        return Int32.Parse(Player[index].transform.GetChild(2).GetComponent<TMP_Text>().text.Replace("[KOMMASTELLEN]", "|").Split('|')[1]);
    }
}
