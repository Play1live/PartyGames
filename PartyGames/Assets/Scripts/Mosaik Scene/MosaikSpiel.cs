using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MosaikSpiel
{
    private List<Mosaik> mosaike;
    private Mosaik selected;
    private Sprite beispiel;

    public MosaikSpiel()
    {
        mosaike = new List<Mosaik>();
        beispiel = Resources.Load<Sprite>("Spiele/Mosaik/Beispiel");

        for (int i = 0; i < 5; i++)
        {
            mosaike.Add(new Mosaik(i));
        }
    }

    public List<Mosaik> getMosaike() { return mosaike; }
    public int getIndex(Mosaik mosaik) { return mosaike.IndexOf(mosaik); }
    public int getIndex(string titel) { foreach (Mosaik mosaik in mosaike) if (mosaik.getTitel().ToLower().Equals(titel.ToLower())) return mosaike.IndexOf(mosaik); return -1; }
    public Mosaik getMosaik(int index) { return mosaike[index]; }
    public Mosaik getMosaik(string titel)
    {
        foreach (Mosaik mosaik in mosaike)
        {
            if (mosaik.getTitel().ToLower().Equals(titel.ToLower()))
                return mosaik;
        }
        return null;
    }
    public void setSelected(Mosaik mosaik) { selected = mosaik; }
    public Mosaik getSelected() { return selected; }
    public Sprite getBeispiel() { return beispiel; }

    public List<string> getListenAsStringList()
    {
        List<string> list = new List<string>();
        foreach (Mosaik mosaik in mosaike)
        {
            list.Add(mosaik.getTitel());
        }
        return list;
    }
}
