using UnityEngine;

public class SurvivalArea : MonoBehaviour
{
    public GameObject monster;
    public GameObject shop;
    public int monsterNum;
    public int shopNum;
    public float range;

    void CreateShops(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
        }
    }

    void CreateMonsters(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
        }
    }

    public void ResetArea(GameObject[] agents)
    {
        foreach (GameObject agent in agents)
        {
            if (agent.transform.parent == gameObject.transform)
            {
                agent.transform.position = new Vector3(Random.Range(-range, range), 2f,
                                               Random.Range(-range, range))
                                           + transform.position;
                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }

        CreateMonsters(monsterNum, monster);
        CreateShops(shopNum, shop);
    }

    // 每杀一只, 重新生成一只
    public void RespawnMonster(int num)
    {
        CreateMonsters(num, monster);
    }
}