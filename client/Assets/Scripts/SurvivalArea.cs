using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SurvivalArea : MonoBehaviour
{
    public GameObject monster;
    public GameObject shop;
    public int monsterNum;
    public int shopNum;
    public float range;
    private List<NpcAgent> _agentList = new List<NpcAgent>();
    private List<Monster> _monsterList = new List<Monster>();
    private List<Shop> _shopList = new List<Shop>();
    private float _interactRange = 1.5f; // 交互范围, 超过此范围的对象获取不到

    private void Awake()
    {
        // 获取场上所有agent, 保存起来
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Agent");
        foreach (GameObject obj in objects)
        {
            _agentList.Add(obj.GetComponent<NpcAgent>());
        }
    }

    // 仅返回探测范围内的agent
    public NpcAgent GetAgentInRangeById(int id, NpcAgent selfAgent)
    {
        foreach (NpcAgent agent in _agentList)
        {
            if (agent.agentid == id)
            {
                if (Vector3.Distance(agent.transform.position, selfAgent.transform.position) < _interactRange)
                {
                    return agent;
                }
            }
        }

        return null;
    }

    // 仅返回探测范围内的agent
    public Monster GetMonsterInRangeById(int id, NpcAgent agent)
    {
        foreach (Monster monster in _monsterList)
        {
            if (monster.monsterid == id)
            {
                if (Vector3.Distance(monster.transform.position, agent.transform.position) < _interactRange)
                {
                    return monster;
                }
            }
        }

        return null;
    }

    public int GetMonsterCntInRange(NpcAgent self)
    {
        int cnt = 0;
        for (int i = 0; i < _monsterList.Count; i++)
        {
            if (self.agentid != _agentList[i].agentid && Vector3.Distance(_monsterList[i].transform.position, self.transform.position) < _interactRange)
            {
                cnt++;
            }
        }

        return cnt;
    }

    public int GetAgentCntInRange(NpcAgent self)
    {
        int cnt = 0;
        for (int i = 0; i < _agentList.Count; i++)
        {
            if (self.agentid != _agentList[i].agentid && Vector3.Distance(_agentList[i].transform.position, self.transform.position) < _interactRange)
            {
                cnt++;
            }
        }

        return cnt;
    }

    public NpcAgent GetNearestAgent(NpcAgent self)
    {
        float distance = float.MaxValue;
        NpcAgent targetAgent = null;
        for (int i = 0; i < _agentList.Count; i++)
        {
            if (_agentList[i].agentid == self.agentid)
            {
                continue;
            }

            float d = Vector3.Distance(self.transform.position, _agentList[i].transform.position);
            if (d < distance && d < _interactRange)
            {
                targetAgent = _agentList[i];
                distance = d;
            }
        }

        return targetAgent;
    }

    public Shop GetNearestShop(NpcAgent self)
    {
        float distance = float.MaxValue;
        Shop nearestShop = null;
        for (int i = 0; i < _shopList.Count; i++)
        {
            float d = Vector3.Distance(self.transform.position, _shopList[i].transform.position);
            if (d < distance)
            {
                nearestShop = _shopList[i];
                distance = d;
            }
        }

        return nearestShop;
    }

    public Monster GetNearestMonster(NpcAgent self)
    {
        float distance = float.MaxValue;
        Monster nearestMonster = null;
        for (int i = 0; i < _monsterList.Count; i++)
        {
            float d = Vector3.Distance(self.transform.position, _monsterList[i].transform.position);
            if (d < distance)
            {
                nearestMonster = _monsterList[i];
                distance = d;
            }
        }

        return nearestMonster;
    }

    void CreateShops(int num, GameObject type)
    {
        if (_shopList.Count == 0)
        {
            for (int i = 0; i < num; i++)
            {
                GameObject go = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                        Random.Range(-range, range)) + transform.position,
                    Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
                _shopList.Add(go.GetComponent<Shop>());
            }
        }
        else
        {
            if (_shopList.Count != num)
            {
                Debug.LogError("怎么训练过程中商店数量变了?");
                return;
            }

            for (int i = 0; i < _shopList.Count; i++)
            {
                _shopList[i].transform.position = new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range));
            }
        }
    }

    void CreateMonsters(int num, GameObject type)
    {
        _monsterList.Clear();
        for (int i = 0; i < num; i++)
        {
            GameObject go = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
            _monsterList.Add(go.GetComponent<Monster>());
        }
    }

    public void ResetArea()
    {
        ResetAgents();
        CreateMonsters(monsterNum, monster);
        CreateShops(shopNum, shop);
    }

    // 每杀一只, 重新生成一只
    public void RespawnMonster(int num)
    {
        CreateMonsters(num, monster);
    }

    // agent死后找个地方重生, 补满血, 扣分
    public void RespawnAgent(NpcAgent agent)
    {
        agent.transform.position = new Vector3(Random.Range(-range, range), 1f,
            Random.Range(-range, range));
        agent.Reset();
    }

    public void ResetAgents()
    {
        for (int i = 0; i < _agentList.Count; i++)
        {
            _agentList[i].transform.position = new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range));
        }
    }
}