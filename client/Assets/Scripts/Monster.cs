using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    private float _hp; // 当前血量
    private float _maxHp = 100; // 最大血量
    private int _level;
    public int monsterid;

    public float GetHp()
    {
        return _hp;
    }

    public int GetLevel()
    {
        return _level;
    }

    public void Reset()
    {
        
    }

    // 返回的是材料数量, 如果被击杀则返回材料
    public int ApplyAtk(float damage)
    {
        _hp -= damage;
        if (_hp <= 0)
        {
            return Random.Range(1, 5);
        }

        return 0;
    }
}
