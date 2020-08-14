using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Subject_Main : MonoBehaviour
{
    public int number;
    public int[] connection;
    public Main main;
    public int[] connectnode;
    public GameObject[] ConnectedObject;
    LineRenderer[] lrs;
    public GameObject LineObjectPrefab;
    int sum;
    public int state;
    int colorstate;
    int[] linecolorstate;
    TextMesh text;
    // Start is called before the first frame update
    void Start()
    {
        sum = 0;
        for (int i = 0; i < connection.Length; i++)
        {
            if (connection[i] == 1 && number != i)
            {
                sum++;
            }
        }
        connectnode = new int[sum];
        lrs = new LineRenderer[sum];
        linecolorstate = new int[sum];
        int count = 0;
        for (int i = 0; i < connection.Length; i++)
        {
            if (connection[i] == 1 && number != i)
            {
                connectnode[count] = i;
                count++;
            }
        }

        for (int i = 0; i < sum; i++)
        {
            GameObject pref = Instantiate(LineObjectPrefab, transform);
            lrs[i] = pref.GetComponent<LineRenderer>();
            lrs[i].positionCount = 2;
        }
        colorstate = 0;
        text = transform.GetChild(0).GetComponent<TextMesh>();
        text.text = number.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (main.creationcomplete)
        {
            state = main.nodestate[number];
            if (state == 0 && state != colorstate)
            {
                GetComponent<SpriteRenderer>().color = main.normalColor;
                colorstate = 0;
            }
            else if (state == 1 && state != colorstate)
            {
                GetComponent<SpriteRenderer>().color = main.infectedColor;
                colorstate = 1;
            }
            else if (state == 2 && state != colorstate)
            {
                GetComponent<SpriteRenderer>().color = main.recoveredColor;
                colorstate = 2;
            }
            else if (state == 3 && state != colorstate)
            {
                GetComponent<SpriteRenderer>().color = main.immunizedColor;
                colorstate = 3;
            }
            for (int i = 0; i < sum; i++)
            {
                lrs[i].SetPosition(0, transform.position);
                lrs[i].SetPosition(1, ConnectedObject[connectnode[i]].transform.position);
                string a = number.ToString() + "," + connectnode[i].ToString();
                string b = connectnode[i].ToString() + "," + number.ToString();
                if (linecolorstate[i] != 1 && (main.spreadlist.Contains(a) || main.spreadlist.Contains(b)))
                {
                    lrs[i].startColor = main.infectedLineColor;
                    lrs[i].endColor = main.infectedLineColor;
                    linecolorstate[i] = 1;
                }
                else if (linecolorstate[i] != 0 && !(main.spreadlist.Contains(a) || main.spreadlist.Contains(b)))
                {
                    lrs[i].startColor = main.normalLineColor;
                    lrs[i].endColor = main.normalLineColor;
                    linecolorstate[i] = 0;
                }
            }
        }
    }
}
