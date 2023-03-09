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

    GameObject[] Player;
    bool[] ismoving;

    int Schritte = 100;
    int Schrittweite;

    void OnEnable()
    {
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

        for (int i = 0; i < Player.Length; i++)
        {
            //Player[i].GetComponent<Image>().sprite = Config.PLAYERLIST[i].icon;
            Player[i].transform.GetChild(1).gameObject.SetActive(true);
            Player[i].transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = getSPIELER_WERT(i)+"";
            if (getSPIELER_WERT(i) < getSTART_WERT(i))
            {
                ismoving[i] = false;
            }
            else
            {
                ismoving[i] = true;
            }
        }

        started = true;
        /*
         data_text += "[START_WERT]" + StartWert + "[START_WERT]";
        data_text += "[ZIEL_WERT]" + ZielWert + "[ZIEL_WERT]";
        data_text += "[MAX_WERT]" + MaxWert + "[MAX_WERT]";
        data_text += "[START_POSITION]" + StartPosition + "[START_POSITION]";
        data_text += "[MAX_POSITION]" + MaxPosition + "[MAX_POSITION]";
        data_text += "[DIFF_NULL]" + DifftoNull + "[DIFF_NULL]";
        data_text += "[DIFF_MAX]" + DiffToMax + "[DIFF_MAX]";
        data_text += "[DISTANCE_PER_MOVE]" + SchritteProEinheit + "[DISTANCE_PER_MOVE]";
        data_text += "[SPIELER_WERT]" + spielerwert + "[SPIELER_WERT]";

         * */
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

            // Spieler bewegen
            if (i == 0)
            {
                Debug.Log(getSPIELER_POSITION_X(i));
                Debug.Log(getSPIELER_END_POSITION(i));
            }
            moveSPIELER_POSITION_X(i, 1);

            // Spieler drüber hinaus
            if (getSPIELER_POSITION_X(i) >= getSPIELER_END_POSITION(i))
            {
                ismoving[i] = false;
                setSPIELER_POSITION_X(i, getSPIELER_END_POSITION(i));
            }
        }


        // Beenden
        if (!minOneisMoving)
            Controller.SetActive(false);
    }

    void OnDisable()
    {
        
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
        Player[index].transform.localPosition = new Vector3(x, getSPIELER_POSITION_X(index) + x, 0);
    }
    private float getSPIELER_END_POSITION(int index)
    {
        return (getDISTANCE_PER_MOVE(index) * (getSPIELER_WERT(index) - getSPIELER_WERT(index)) - getDIFF_NULL(index));
    }
}
