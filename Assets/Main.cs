using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class Main : MonoBehaviour
{
    
    int[,] networks;

    [Header("Setting")]
    public int[] nodestate;
    public int startnode;
    public int networknumber;
    public int connectnumber;
    public float infectionprobability;
    public float recoverprobability;
    public int immunizationnum;
    public int noderepeatnum;
    public int changenodenum;
    public int changenetworknum;
    public bool printcalculatinglog;
    public bool printnetworksettinglog;
    public int immunizationstrategy;
    public bool fastmode;

    [Space(10f)]
    [Header("Not Important")]
    public int timepassed;
    bool situationend;
    bool situationstarted;
    public int plusinfected;
    public int plusrecovered;
    public int infectednum;
    public int totalinfectednum;
    public List<int> infectednumlist;
    string filePath;

    int repeatmax;
    float repeatmean;
    public GameObject Subject_Prefab;
    public GameObject[] createdsubject;
    int[] discovered;
    List<int> cutvertexnodes;
    FileStream fs;
    StreamWriter sw;

    public bool creationcomplete;
    [HideInInspector] public Color normalColor;
    [HideInInspector] public Color normalLineColor;
    [HideInInspector] public Color infectedColor;
    [HideInInspector] public Color infectedLineColor;
    [HideInInspector] public Color recoveredColor;
    [HideInInspector] public Color immunizedColor;

    public List<string> spreadlist;

    public struct averagelists
    {
        public List<float> timepassedlist;
        public List<float> infectpercentlist;
        public List<float> maxinfectedlist;
        public List<float> averageinfectedlist;
    }
    public averagelists[] aver = new averagelists[5];

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i<aver.Length; i++)
        {
            aver[i].timepassedlist = new List<float>();
            aver[i].infectpercentlist = new List<float>();
            aver[i].maxinfectedlist = new List<float>();
            aver[i].averageinfectedlist = new List<float>();
        }
        networks = new int[networknumber, networknumber];
        nodestate = new int[networknumber];
        infectednumlist = new List<int>();
        spreadlist = new List<string>();


        NetworkSetting();
        situationend = false;
        situationstarted = false;
        creationcomplete = false;

        discovered = new int[networknumber];
        cutvertexnodes = new List<int>();

        filePath = Application.dataPath + "/data.txt";
        createdsubject = new GameObject[networknumber];
        if (!fastmode)
        {
            VisualNetworkCreate();
        }
        creationcomplete = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NetworkSetting()
    {
        if (connectnumber > networknumber * (networknumber - 1) / 2f)
        {
            Debug.Log("불가능합니다. 최대 연결 개수 : " + (networknumber * (networknumber - 1) / 2f).ToString());
        }
        else
        {
            int retry = 50;
            while (true)
            {
                networks = new int[networknumber, networknumber];
                int count = connectnumber - networknumber + 1;
                int a;
                int b;
                int maxcount;
                bool overmaxcount;
                for (int i = 0; i < networknumber; i++)
                {
                    networks[i, i] = 1;
                }
                for (int i = 1; i < networknumber; i++)
                {
                    a = Random.Range(0, i);
                    networks[i, a] = 1;
                    networks[a, i] = 1;
                }
                while (count > 0)
                {
                    overmaxcount = false;
                    maxcount = 1000;
                    a = Random.Range(0, networknumber);
                    while (true)
                    {
                        b = Random.Range(0, networknumber);
                        if (networks[a, b] != 1)
                        {
                            break;
                        }
                        maxcount--;
                        if (maxcount <= 0)
                        {
                            overmaxcount = true;
                            Debug.Log("OverCounted. Left Count : " + count.ToString());
                            break;
                        }
                    }
                    if (overmaxcount)
                    {
                        break;
                    }
                    else
                    {
                        networks[a, b] = 1;
                        networks[b, a] = 1;
                        count--;
                        maxcount--;
                    }
                }
                if (count <= 0)
                {
                    break;
                }
                retry--;
                if (retry <= 0)
                {
                    Debug.Log("네트워크 구성 실패. Left Count : " + count.ToString());
                    break;
                }
            }
            if (printnetworksettinglog)
            {
                for (int i = 0; i < networknumber; i++)
                {
                    string net = (i + 1).ToString() + "번 네트워크 : ";
                    for (int j = 0; j < networknumber; j++)
                    {
                        net += networks[i, j].ToString() + " ";
                    }
                    Debug.Log(net);
                }
            }
        }
    }
    void VisualNetworkCreate()
    {
        for (int i = 0; i < networknumber; i++)
        {
            int[] a = new int[networknumber];
            string b = null;
            for (int j = 0; j < networknumber; j++)
            {
                a[j] = networks[i, j];
                b += a[j].ToString();
            }
            GameObject pref = Subject_Prefab;
            pref.GetComponent<Subject_Main>().main = this;
            pref.GetComponent<Subject_Main>().number = i;
            pref.GetComponent<Subject_Main>().connection = a;
            createdsubject[i] = Instantiate(pref, new Vector3(-8.33f + (1f * i), 4.52f, 0f), Quaternion.identity);
        }
        for (int i = 0; i < networknumber; i++)
        {
            createdsubject[i].GetComponent<Subject_Main>().ConnectedObject = createdsubject;
        }
    }

    public void ChooseStartNode()
    {
        startnode = Random.Range(0, networknumber);
    }
    public void StartSimulation()
    {
        situationstarted = true;
        situationend = false;
        timepassed = 1;
        infectednumlist.Clear();
        spreadlist.Clear();
        nodestate = new int[networknumber];
        for (int i = 0; i < networknumber; i++)
        {
            nodestate[i] = 0;
        }
        nodestate[startnode] = 1;
        plusinfected = 1;
        infectednum = 1;
        totalinfectednum = 1;
        infectednumlist.Add(infectednum);
        CurrentStateText(0);
    }
    public void NextCalculation()
    {
        if (!situationstarted)
        {
            Debug.Log("아직 상황이 시작되지 않았습니다!");
        }
        else if (!situationend)
        {
            spreadlist.Clear();
            int previnfected = infectednum;
            plusinfected = 0;
            plusrecovered = 0;
            timepassed += 1;
            for (int i = 0; i < networknumber; i++)
            {
                if (nodestate[i] == 1)
                {
                    Infection(i, networks, nodestate);
                    if (timepassed >= 2)
                    {
                        Recover(i);
                    }
                }
            }
            InfectionChange();
            CurrentStateText(previnfected);
            infectednumlist.Add(infectednum);
            bool bug = true;
            for (int i=0; i<networknumber; i++)
            {
                if (nodestate[i] == 1)
                {
                    bug = false;
                }
            }
            if (infectednum == 0)
            {
                int sum = 0;
                int max = 0;
                foreach (int num in infectednumlist)
                {
                    if (max < num)
                    {
                        max = num;
                    }
                    sum += num;
                }
                float a = sum;
                float b = timepassed;
                repeatmean = Mathf.Round((a / b) * 10f) / 10f;
                repeatmax = max;
                if (!fastmode)
                {
                    Debug.Log(timepassed.ToString() + "일 경과 : 상황이 종료되었습니다, " + "감염 상황 : " + totalinfectednum.ToString() + "/" + networknumber.ToString() + " , 평균 감염자 수 : " + repeatmean.ToString() + " , 최다 감염자 수 : " + max.ToString());
                }
                situationend = true;
                situationstarted = false;
            }
            else if (bug)
            {
                situationend = true;
                situationstarted = false;
                Debug.Log("버그발견버그발견버그발견버그발견버그발견버그발견버그발견버그발견버그발견버그발견버그발견");
            }
        }
    }
    void Infection(int node, int[,] networks, int[] nodestate)
    {
        float probability;
        for (int i = 0; i < networknumber; i++)
        {
            if (networks[node, i] == 1 && nodestate[i] == 0)
            {
                probability = Random.Range(0f, 1f);
                if (probability <= infectionprobability) //감염 성공
                {
                    nodestate[i] = -1;//감염 대기 상태
                    plusinfected++;
                    infectednum++;
                    totalinfectednum++;
                    spreadlist.Add(node.ToString() + "," + i.ToString());
                }
            }
        }
    }
    void Recover(int node)
    {
        float probability;
        probability = Random.Range(0f, 1f);
        if (probability <= recoverprobability) //회복 성공
        {
            nodestate[node] = 2;
            plusrecovered++;
            infectednum--;
        }
    }
    void InfectionChange()
    {
        for (int i = 0; i < networknumber; i++)
        {
            if (nodestate[i] == -1)
            {
                nodestate[i] = 1;
            }
        }
    }
    void CurrentStateText(int previnfected)
    {
        if (printcalculatinglog)
        {
            string text = timepassed.ToString() + "일 경과 : ";
            for (int i = 0; i < networknumber; i++)
            {
                text += nodestate[i].ToString() + " ";
            }
            text += "추가 감염 : " + plusinfected.ToString() + " / 추가 완치 :" + plusrecovered.ToString() + " / 전일 대비 : " + (infectednum - previnfected).ToString();
            Debug.Log(text);
        }
    }
    public void ImmediatelyCalculate()
    {
        StartSimulation();
        while (!situationend)
        {
            NextCalculation();
        }
    }
    public void RepetitionCalculation()
    {
        float a, b;
        string c;
        for (int i = 0; i < changenodenum; i++)
        {
            ChooseStartNode();
            for (int j = 0; j < noderepeatnum; j++)
            {
                StartSimulation();
                if (immunizationstrategy == 1)
                {
                    RandomIM();
                }
                else if (immunizationstrategy == 2)
                {
                    MultiplePointIM();
                }
                else if (immunizationstrategy == 3)
                {
                    AcquaintanceIM();
                }
                else if (immunizationstrategy == 4)
                {
                    CutVertexIM();
                }
                int count = 0;
                while (!situationend)
                {

                    NextCalculation();
                    count++;
                    if (count > 300)
                    {
                        string tt = null;
                        for (int k = 0; k < networknumber; k++)
                        {
                            tt += nodestate[k].ToString() + ", ";
                        }
                        break;
                    }
                }
                a = totalinfectednum;
                b = networknumber;
                c = (Mathf.Round((a / b) * 100000f) / 1000f).ToString();
                //sw.WriteLine(timepassed.ToString() + "," + c + "," + repeatmean.ToString() + "," + repeatmax.ToString());
                if (!fastmode)
                {
                    Debug.Log(timepassed.ToString() + "," + c + "," + repeatmean.ToString() + "," + repeatmax.ToString());
                }
                aver[immunizationstrategy].timepassedlist.Add(timepassed);
                aver[immunizationstrategy].infectpercentlist.Add(Mathf.Round((a / b) * 100000f) / 1000f);
                aver[immunizationstrategy].averageinfectedlist.Add(repeatmean);
                aver[immunizationstrategy].maxinfectedlist.Add(repeatmax);
            }
        }
    }
    public void AllStrategyCalculation()
    {
        for (int i = 0; i < aver.Length; i++)
        {
            aver[i].timepassedlist.Clear();
            aver[i].infectpercentlist.Clear();
            aver[i].maxinfectedlist.Clear();
            aver[i].averageinfectedlist.Clear();
        }
        fs = new FileStream(filePath, FileMode.Create);
        sw = new StreamWriter(fs);
        StartCoroutine(SlowCalculate());

    }
    IEnumerator SlowCalculate()
    {
        for (int i = 0; i < changenetworknum; i++)
        {
            ResetNetwork();
            Debug.Log("네트워크 재설정 완료. (" + (i + 1).ToString() + "번째)");
            yield return new WaitForSeconds(0.3f);
            for (int j = 0; j < 5; j++)
            {
                immunizationstrategy = j;
                RepetitionCalculation();
                Debug.Log("반복 계산 완료. (접종 전략 : " + j.ToString() + ")");
                yield return new WaitForSeconds(0.4f);
            }
        }
        float aav, bav, cav, dav;
        for (int i = 0; i < 5; i++)
        {
            aav = aver[i].timepassedlist.Average();
            bav = aver[i].infectpercentlist.Average();
            cav = aver[i].averageinfectedlist.Average();
            dav = aver[i].maxinfectedlist.Average();
            sw.WriteLine(i.ToString() + "번째 전략----------------------------------------");
            sw.WriteLine(aav.ToString() + "," + bav.ToString() + "," + cav.ToString() + "," + dav.ToString());
        }
        sw.Close();
        fs.Close();
        Debug.Log("계산이 완료되었습니다. data 파일을 확인해주세요.");
    }
    public void ResetNetwork()
    {
        networks = new int[networknumber, networknumber];
        nodestate = new int[networknumber];
        infectednumlist = new List<int>();
        spreadlist = new List<string>();
        for (int i = 0; i < aver.Length; i++)
        {
            aver[i].timepassedlist.Clear();
            aver[i].infectpercentlist.Clear();
            aver[i].maxinfectedlist.Clear();
            aver[i].averageinfectedlist.Clear();
        }
        NetworkSetting();
        situationend = false;
        situationstarted = false;
        creationcomplete = false;

        discovered = new int[networknumber];
        cutvertexnodes = new List<int>();

        for (int i = 0; i < createdsubject.Length; i++)
        {
            Destroy(createdsubject[i]);
        }
        createdsubject = new GameObject[networknumber];
        if (!fastmode)
        {
            VisualNetworkCreate();
        }
        creationcomplete = true;
    }
    public void RandomIM()
    {   
        for (int i = 0; i < networknumber; i++)
        {
            if (nodestate[i] == 3)
            {
                nodestate[i] = 0;
            }
        }
        int[] a = new int[immunizationnum];
        int num;
        int maxcount = 0;
        bool overlap;
        for (int i = 0; i < immunizationnum; i++)
        {
            while(maxcount < 300)
            {
                overlap = false;
                num = Random.Range(0, networknumber);
                if (nodestate[num] == 1)
                {
                    overlap = true;
                }
                if (i >= 1)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (a[j] == num)
                        {
                            overlap = true;
                        }
                    }
                    if (!overlap)
                    {
                        a[i] = num;
                        break;
                    }
                }
                else if (!overlap)
                {
                    a[i] = num;
                    break;
                }
                maxcount++;
            }
        }
        for (int i = 0; i < immunizationnum; i++)
        {
            nodestate[a[i]] = 3;
        }
        string text = null;
        for (int i = 0; i < immunizationnum; i++)
        {
            text += a[i].ToString() + ", ";
        }
    }
    public void MultiplePointIM()
    {
        if (immunizationnum < networknumber)
        {
            for (int i = 0; i < networknumber; i++)
            {
                if (nodestate[i] == 3)
                {
                    nodestate[i] = 0;
                }
            }
            int[] connectcount = new int[networknumber];
            int[] rank = new int[networknumber];
            int count = 0;
            for (int i = 0; i < networknumber; i++)
            {
                for (int j = 0; j < networknumber; j++)
                {
                    connectcount[i] += networks[i, j];
                }
                connectcount[i] -= 1;
            }
            for (int i = 0; i < networknumber; i++)
            {
                rank[i] = 1;
                for (int j = 0; j < networknumber; j++)
                {
                    if (connectcount[i] < connectcount[j])
                    {
                        rank[i]++;
                    }
                }
            }
            int rankcount = 0;
            int a;
            List<int> ranknum = new List<int>();
            for (int i = 1; i < networknumber + 1; i++)
            {
                ranknum.Clear();
                for (int j = 0; j < networknumber; j++)
                {
                    if (rank[j] == i && nodestate[j] != 1) 
                    {
                        rankcount++;
                        ranknum.Add(j);
                    }
                }
                if (rankcount <= immunizationnum - count && rankcount != 0)
                {
                    for (int j = 0; j < ranknum.Count; j++)
                    {
                        nodestate[ranknum[j]] = 3;
                        count++;
                    }
                }
                else if (rankcount != 0)
                {
                    for (int j = 0; j < immunizationnum - count; j++)
                    {
                        a = Random.Range(0, ranknum.Count);
                        nodestate[ranknum[a]] = 3;
                        ranknum.RemoveAt(a);
                    }
                    count = immunizationnum;
                }
                rankcount = 0;
                if (count >= immunizationnum)
                {
                    break;
                }
            }
        }
        else
        {
            Debug.Log("접종 대상 수가 너무 많습니다!");
        }
    }
    public void AcquaintanceIM()
    {
        for (int i = 0; i < networknumber; i++)
        {
            if (nodestate[i] == 3)
            {
                nodestate[i] = 0;
            }
        }
        int[] connectcount = new int[networknumber];
        List<int> connectednodes = new List<int>();
        List<int> mostconnectednodes = new List<int>();

        for (int i = 0; i < networknumber; i++)
        {
            for (int j = 0; j < networknumber; j++)
            {
                connectcount[i] += networks[i, j];
            }
            connectcount[i] -= 1;
        }

        for (int i=0; i<immunizationnum; i++)
        {
            for (int j = 0; j < 500; j++)
            {
                connectednodes.Clear();
                mostconnectednodes.Clear();
                int a = Random.Range(0, networknumber);
                for (int k = 0; k < networknumber; k++)
                {
                    if (networks[a, k] == 1 && k != a && nodestate[k] != 1 && nodestate[k] != 3)
                    {
                        connectednodes.Add(k);
                    }
                }
                int max = 0;
                foreach (int connectednode in connectednodes)
                {
                    if (connectcount[connectednode] > max)
                    {
                        max = connectcount[connectednode];
                        mostconnectednodes.Clear();
                        mostconnectednodes.Add(connectednode);
                    }
                    else if (connectcount[connectednode] == max)
                    {
                        mostconnectednodes.Add(connectednode);
                    }
                }
                if (mostconnectednodes.Count>1)
                {
                    //Debug.Log(a.ToString() + "번 노드와 연결된 노드 중에, ");
                    a = Random.Range(0, mostconnectednodes.Count);
                    nodestate[mostconnectednodes[a]] = 3;
                    //Debug.Log(mostconnectednodes[a].ToString() + "번 노드에 접종하였습니다.");
                    break;
                    
                }
                else if (mostconnectednodes.Count == 1)
                {
                    //Debug.Log(a.ToString() + "번 노드와 연결된 노드 중에, ");
                    //Debug.Log(mostconnectednodes[0].ToString() + "번 노드에 접종하였습니다.");
                    nodestate[mostconnectednodes[0]] = 3;
                    break;
                }
            }
        }
    }
    public void CutVertexIM()
    {
        for (int i = 0; i < networknumber; i++)
        {
            if (nodestate[i] == 3)
            {
                nodestate[i] = 0;
            }
        }
        discovered = new int[networknumber];
        cutvertexnodes.Clear();
        Debug.Log("탐색 시작.");
        FindCutVertexDFS(0, true, 1);
        int leftcount = immunizationnum;
        Debug.Log("탐색 완료. 접종 시작");
        if (cutvertexnodes.Count <= immunizationnum)
        {
            leftcount -= cutvertexnodes.Count;
            for (int i = 0; i < cutvertexnodes.Count; i++)
            {
                if (nodestate[cutvertexnodes[i]] != 1)
                {
                    nodestate[cutvertexnodes[i]] = 3;
                }
                else
                {
                    Debug.Log("버그의 원인3");
                    leftcount++;
                }
            }
            if (leftcount > 0)
            {
                Debug.Log("다중점 전략 시행, leftcount : " + leftcount.ToString());
                CutVertexMultipleIM(leftcount);
            }
        }
        else
        {
            Debug.Log("AmongCutVertexIM 실행, cutvertexnodes.Count : " + cutvertexnodes.Count.ToString());
            AmongCutVertexIM();
        }
        Debug.Log("접종 완료.");
    }
    int FindCutVertexDFS(int here, bool isroot, int counter)
    {
        discovered[here] = ++counter;
        int ret = discovered[here];
        int child = 0;
        for (int i = 0; i < networknumber; i++)
        {
            if (networks[here, i] == 1 && here != i )
            {
                if (discovered[i] == 0)
                {
                    child++;
                    int low = FindCutVertexDFS(i, false, counter);
                    if (!isroot && low >= discovered[here] && !cutvertexnodes.Contains(here))
                    {
                        cutvertexnodes.Add(here);
                    }
                    ret = Mathf.Min(ret, low);
                }
                else
                {
                    ret = Mathf.Min(ret, discovered[i]);
                }
            }
        }
        if (isroot && child >= 2 && !cutvertexnodes.Contains(here))
        {
            cutvertexnodes.Add(here);
        }
        return ret;
    }
    void CutVertexMultipleIM(int leftcount)
    {
        int[] connectcount = new int[networknumber];
        int[] rank = new int[networknumber];
        int count = 0;
        for (int i = 0; i < networknumber; i++)
        {
            for (int j = 0; j < networknumber; j++)
            {
                connectcount[i] += networks[i, j];
            }
            connectcount[i] -= 1;
        }
        for (int i = 0; i < networknumber; i++)
        {
            rank[i] = 1;
            for (int j = 0; j < networknumber; j++)
            {
                if (connectcount[i] < connectcount[j])
                {
                    rank[i]++;
                }
            }
        }
        int rankcount = 0;
        int a;
        List<int> ranknum = new List<int>();
        for (int i = 1; i < networknumber + 1; i++)
        {
            ranknum.Clear();
            for (int j = 0; j < networknumber; j++)
            {
                if (rank[j] == i && nodestate[j] != 1 && nodestate[j] != 3)
                {
                    rankcount++;
                    ranknum.Add(j);
                }
            }
            if (rankcount <= leftcount - count && rankcount != 0)
            {
                for (int j = 0; j < ranknum.Count; j++)
                {
                    if (nodestate[ranknum[j]] != 1)
                    {
                        nodestate[ranknum[j]] = 3;
                    }
                    else
                    {
                        Debug.Log("버그의 원인1");
                    }
                    count++;
                }
            }
            else if (rankcount != 0)
            {
                for (int j = 0; j < leftcount - count; j++)
                {
                    a = Random.Range(0, ranknum.Count);
                    if (nodestate[ranknum[a]] != 1)
                    {
                        nodestate[ranknum[a]] = 3;
                    }
                    else
                    {
                        Debug.Log("버그의 원인2");
                    }
                    ranknum.RemoveAt(a);
                }
                count = leftcount;
            }
            rankcount = 0;
            if (count >= leftcount)
            {
                break;
            }
        }
    }
    void AmongCutVertexIM()
    {
        int[] connectcount = new int[networknumber];
        int[] rank = new int[networknumber];
        int count = 0;
        for (int i = 0; i < networknumber; i++)
        {
            for (int j = 0; j < networknumber; j++)
            {
                connectcount[i] += networks[i, j];
            }
            connectcount[i] -= 1;
        }
        for (int i = 0; i < networknumber; i++)
        {
            rank[i] = 1;
            for (int j = 0; j < networknumber; j++)
            {
                if (connectcount[i] < connectcount[j])
                {
                    rank[i]++;
                }
            }
        }
        int rankcount = 0;
        int a;
        List<int> ranknum = new List<int>();
        for (int i = 1; i < networknumber + 1; i++)
        {
            ranknum.Clear();
            for (int j = 0; j < networknumber; j++)
            {
                if (rank[j] == i && nodestate[j] != 1 && nodestate[j] != 3 && cutvertexnodes.Contains(j))
                {
                    rankcount++;
                    ranknum.Add(j);
                }
            }
            if (rankcount <= immunizationnum - count && rankcount != 0)
            {
                for (int j = 0; j < ranknum.Count; j++)
                {
                    nodestate[ranknum[j]] = 3;
                    count++;
                }
            }
            else if (rankcount != 0)
            {
                for (int j = 0; j < immunizationnum - count; j++)
                {
                    a = Random.Range(0, ranknum.Count);
                    nodestate[ranknum[a]] = 3;
                    ranknum.RemoveAt(a);
                }
                count = immunizationnum;
            }
            rankcount = 0;
            if (count >= immunizationnum)
            {
                break;
            }
        }
    }
}
