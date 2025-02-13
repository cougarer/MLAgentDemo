using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SurvivalArea : MonoBehaviour
{
    public Transform spawnCenter;
    public GameObject monster;
    public GameObject shop;
    public int monsterNum;
    public int shopNum;
    public float range;
    private List<NpcAgent> _agentList = new List<NpcAgent>();
    private List<Monster> _monsterList = new List<Monster>();
    private List<Shop> _shopList = new List<Shop>();
    [NonSerialized] public float interactRange = 2.5f; // 交互范围, 超过此范围的对象获取不到

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
                if (Vector3.Distance(agent.transform.position, selfAgent.transform.position) < interactRange)
                {
                    return agent;
                }
            }
        }

        return null;
    }

    public Monster GetMonsterById(int id)
    {
        foreach (Monster monster in _monsterList)
        {
            if (monster.monsterid == id)
            {
                return monster;
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
                if (Vector3.Distance(monster.transform.position, agent.transform.position) < interactRange)
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
            if (Vector3.Distance(_monsterList[i].transform.position, self.transform.position) < interactRange)
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
            if (self.agentid != _agentList[i].agentid && Vector3.Distance(_agentList[i].transform.position, self.transform.position) < interactRange)
            {
                cnt++;
            }
        }

        return cnt;
    }

    public NpcAgent GetNearestAgentInRange(NpcAgent self)
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
            if (d < distance && d < interactRange)
            {
                targetAgent = _agentList[i];
                distance = d;
            }
        }

        if (distance > interactRange)
        {
            return null;
        }

        return targetAgent;
    }

    public Shop GetNearestShopInRange(NpcAgent self)
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

        if (distance > interactRange)
        {
            return null;
        }

        return nearestShop;
    }

    public Monster GetNearestMonsterInRange(NpcAgent self)
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

        if (distance > interactRange)
        {
            return null;
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
                        Random.Range(-range, range)) + spawnCenter.position,
                    quaternion.identity);
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
                    Random.Range(-range, range)) + spawnCenter.position;
            }
        }
    }

    void CreateMonsters(int num, GameObject type)
    {
        for (int i = 0; i < _monsterList.Count; i++)
        {
            Destroy(_monsterList[i].gameObject);
        }

        _monsterList.Clear();

        for (int i = 0; i < num; i++)
        {
            GameObject go = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range)) + spawnCenter.position,
                quaternion.identity);
            go.GetComponent<Monster>().Reset(100 + i);
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
    public void RespawnMonster(int id)
    {
        var monster = GetMonsterById(id);
        monster.Reset(id);
        monster.transform.position = new Vector3(Random.Range(-range, range), 1f,
            Random.Range(-range, range)) + spawnCenter.position;
    }

    // agent死后找个地方重生, 补满血, 扣分
    public void RespawnAgent(NpcAgent agent)
    {
        agent.transform.position = new Vector3(Random.Range(-range, range), 1f,
            Random.Range(-range, range)) + spawnCenter.position;
        agent.Reset();
    }

    public void ResetAgents()
    {
        for (int i = 0; i < _agentList.Count; i++)
        {
            _agentList[i].transform.position = new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + spawnCenter.position;
            _agentList[i].Reset();
        }
    }
}