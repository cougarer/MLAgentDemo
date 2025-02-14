using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Monster : MonoBehaviour
{
    private float _hp; // 当前血量
    private float _maxHp = 90f; // 最大血量
    private int _level = 1; // 暂时先都是1级怪物
    [NonSerialized] public int rewardExp = 70; // 击杀奖励经验
    [NonSerialized] public int rewardGold = 90; // 击杀奖励金币
    [NonSerialized] public int monsterid;
    [NonSerialized] public float speed = 1f; // 怪物移动速度
    public float range = 10f; // 移动范围，以怪物初始位置为中心的正方形区域
    private Vector3 targetPosition; // 目标位置
    private float _lastAtkTime; // 上次攻击时间
    private float _atkCD = 2; // 攻击cd
    private float _damage = 5; // 攻击cd

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Agent"))
        {
            if (Time.time - _lastAtkTime > _atkCD)
            {
                _lastAtkTime = Time.time;
                var agent = collision.gameObject.GetComponent<NpcAgent>();
                Debug.LogError("怪物" + monsterid + "尝试攻击id为" + agent.agentid + "的agent");
                agent.ApplyAtk(_damage);
            }
        }
    }

    public float GetHp()
    {
        return _hp;
    }

    public int GetLevel()
    {
        return _level;
    }

    public void Reset(int id)
    {
        monsterid = id;
        _hp = _maxHp;
        _lastAtkTime = 0;
    }

    // bool: 是否死亡, int: 怪物id
    public (bool, int) ApplyAtk(float damage)
    {
        Debug.LogError("怪物id: " + monsterid + " 受到伤害: " + damage + " 当前血量: " + _hp + " 最大血量: " + _maxHp + "");
        _hp -= damage;
        if (_hp <= 0)
        {
            return (true, monsterid);
        }

        return (false, monsterid);
    }

    private void Start()
    {
        SetRandomTargetPosition();
    }

    private void Update()
    {
        // 如果怪物到达目标位置，则重新设置目标位置
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetRandomTargetPosition();
        }

        // 移动怪物朝向目标位置
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    void SetRandomTargetPosition()
    {
        targetPosition = new Vector3(
            transform.position.x + Random.Range(-range, range),
            transform.position.y,
            transform.position.z + Random.Range(-range, range)
        );
    }
}
