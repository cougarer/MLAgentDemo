using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditor;

public class NpcAgent : Agent
{
    public DynamicTextData textData;
    public int agentid; // 这个从场景赋值
    public SurvivalArea area;
    Rigidbody _AgentRb;

    public float turnSpeed = 300;
    public float moveSpeed = 2;
    private float _atkCD; // 攻击cd, 与等级相关
    private float _minAtkCD = 1; // 最小攻击cd
    private float _maxAtkCD = 2; // 最大攻击cd
    private int _hpPotionCnt; // 身上带的药水的数量
    private int _hpPotionEffect = 100; // 药水效果
    private int _hpPotionPrice = 50; // 药水价格
    private int _gold; // 玩家金币
    private int _expScrollPrice = 50; // 经验卷轴价格
    private int _expScrollEffect = 100; // 经验卷轴效果

    private float _lastAtkTime; // 上次攻击时间
    private float _lastBeAtkTime; // 上次被攻击时间

    private float Damage
    {
        get
        {
            return 10 + _level; // 攻击力是等级相关的
        }
    }

    private float AtkCD
    {
        get { return _minAtkCD + (float)(_maxLevel - _level) / _maxLevel * (_maxAtkCD - _minAtkCD); }
    }

    private float _hp; // 当前血量
    private float _maxHp = 500; // 最大血量
    private int _level; // 当前等级
    private int _maxLevel = 10; // 最大等级
    private float _exp;

    // 经验值
    private float Exp
    {
        get { return _exp; }
        set
        {
            int oldLevel = _level;
            foreach (var item in expTable)
            {
                if (value > item.Item2)
                {
                    _level = item.Item1;
                    if (_level == _maxLevel)
                    {
                        Debug.LogError("agent" + agentid + "成功满级, 一个训练周期结束, 开始下一步");
                        area.ResetArea(false);
                    }
                }
            }

            if (_level != oldLevel)
            {
                Debug.LogError("agent" + agentid + "升级了, 当前等级为" + _level);
            }

            _exp = value;
        }
    }

    // agent level进阶需要的经验值, 举例: 大于100点经验值才能到1级
    private List<(int, float)> expTable = new List<(int, float)>()
    {
        (0, 0),
        (1, 100),
        (2, 200),
        (3, 400),
        (4, 600),
        (5, 800),
        (6, 1000),
        (7, 1200),
        (8, 1400),
        (9, 1600),
        (10, 2000)
    };

    private bool _inCombat; // 战斗中移动速度下降
    private bool _inShop; // 在商店附近一定范围内不会被攻击, 会自动回血, 可以购买道具

    private EnvironmentParameters _ResetParams;

    public void FloatTip(string str)
    {
        Debug.LogError(str);
        Vector3 destination = transform.position + new Vector3(0, 1, 0);
        destination.x += (Random.value - 0.5f) / 3f;
        destination.y += Random.value;
        destination.z += (Random.value - 0.5f) / 3f;

        DynamicTextManager.CreateText(destination, str, textData);
    }

    public override void Initialize()
    {
        _AgentRb = GetComponent<Rigidbody>();
        _ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public int GetLevel()
    {
        return _level;
    }

    public float GetHp()
    {
        return _hp;
    }

    public int GetGold()
    {
        return _gold;
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        var localVelocity = transform.InverseTransformDirection(_AgentRb.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        sensor.AddObservation(_hp); // agent当前血量
        sensor.AddObservation(_maxHp); // agent当前等级的最大血量
        sensor.AddObservation(area.GetAgentCntInRange(this)); // 范围内agent数量
        sensor.AddObservation(area.GetNearestAgentInRange(this) != null
            ? area.GetNearestAgentInRange(this).GetLevel()
            : 0); // 最近agent等级
        sensor.AddObservation(area.GetNearestAgentInRange(this) != null
            ? area.GetNearestAgentInRange(this).GetHp()
            : 0); // 最近agent血量
        sensor.AddObservation(area.GetNearestAgentInRange(this) != null
            ? area.GetNearestAgentInRange(this).agentid
            : 0); // 最近agent id
        sensor.AddObservation(area.GetMonsterCntInRange(this)); // 范围内怪物数量
        sensor.AddObservation(area.GetNearestMonsterInRange(this)
            ? area.GetNearestMonsterInRange(this).GetHp()
            : 0); // 最近怪物血量
        sensor.AddObservation(area.GetNearestMonsterInRange(this)
            ? area.GetNearestMonsterInRange(this).GetLevel()
            : 0); // 最近怪物等级
        sensor.AddObservation(area.GetNearestMonsterInRange(this)
            ? area.GetNearestMonsterInRange(this).monsterid
            : 0); // 最近怪物id
        sensor.AddObservation(_gold); // 金币数量
    }

    public void Reset()
    {
        _lastAtkTime = 0;
        _level = 0;
        _hp = _maxHp;
        _gold = 0;
        _inCombat = false;
        Exp = 0;
    }

    // 施加被攻击, 被agent击杀的话增加agent的经验
    public int ApplyAtk(float num)
    {
        _hp -= num;
        Debug.LogError("agent受到伤害, id: " + agentid + " 受到伤害: " + num + " 当前血量: " + _hp + " 最大血量: " + _maxHp);
        if (_hp <= 0)
        {
            AddReward(-1000);
            Debug.LogError("agent死亡, id:" + agentid + " 当前血量:" + _hp);
            area.RespawnAgent(this);
        }

        return Random.Range(50, 150);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;
        _AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        var usePotionCommand = discreteActions[0] > 0;
        if (usePotionCommand)
        {
            UseHpPotion();
        }

        var buyPotionCommand = discreteActions[1] > 0;
        if (buyPotionCommand)
        {
            if (area.GetNearestShopInRange(this) != null)
            {
                BuyHpPotion(1);
            }
        }

        var buyExpCommand = discreteActions[2] > 0;
        if (buyExpCommand)
        {
            if (area.GetNearestShopInRange(this) != null)
            {
                BuyExpScroll(1);
            }
        }

        var atkAgentCommand = discreteActions[3] > 0; // 直接转为agentid
        if (atkAgentCommand)
        {
            // 看一下他想打的agent是否在攻击范围内
            var agent = area.GetNearestAgentInRange(this);
            if (agent != null)
            {
                // 这个需要判定攻击cd
                if (Time.time - _lastAtkTime > AtkCD)
                {
                    _lastAtkTime = Time.time;
                    FloatTip("agentid为" + agentid + "的个体尝试攻击id为" + agent.agentid + "的agent");
                    AtkAgent(agent);
                }
            }
        }

        var atkMonsterCommand = discreteActions[4] > 0;
        if (atkMonsterCommand)
        {
            // 这个需要判定攻击cd
            var monster = area.GetNearestMonsterInRange(this);
            if (monster != null)
            {
                // 这个需要判定攻击cd
                if (Time.time - _lastAtkTime > AtkCD)
                {
                    _lastAtkTime = Time.time;

                    FloatTip("agentid为" + agentid + "的个体尝试攻击怪物id为" + monster.monsterid + "的怪物");
                    AtkMonster(monster);
                }
            }
        }

        if (_AgentRb.velocity.sqrMagnitude > 5f)
        {
            _AgentRb.velocity *= 0.95f;
        }
    }

    private void AtkAgent(NpcAgent agent)
    {
        var ret = agent.ApplyAtk(Damage);

        if (ret > 0)
        {
            AddReward(80); // 击杀npc给奖励
            Exp += ret;
            _gold += agent.GetGold();
            FloatTip("agent" + agentid + "成功击杀其他agent, 当前经验值为" + Exp);
            area.RespawnAgent(agent); // 让这个agent重生
        }
    }

    private void AtkMonster(Monster monster)
    {
        var monsterKillRet = monster.ApplyAtk(Damage);

        if (monsterKillRet.Item1)
        {
            AddReward(50); // 击杀怪物给奖励
            Exp += monster.rewardExp;
            _gold += monster.rewardGold;
            FloatTip("agent" + agentid + "成功击杀怪物, 当前经验值为" + Exp);
            area.RespawnMonster(monsterKillRet.Item2); // 让这个怪物重生
        }
    }

    private void UseHpPotion()
    {
        if (_hpPotionCnt > 0)
        {
            FloatTip("agentid为" + agentid + "的个体使用了血瓶, 当前血量为" + _hp + "最大血量为" + _maxHp + "当前血瓶数量为" + _hpPotionCnt);
            if (_hp < _maxHp)
            {
                AddReward(10); // 有效加血给奖励
            }

            _hpPotionCnt--;
            _hp = _hp + _hpPotionEffect > _maxHp ? _maxHp : _hp + _hpPotionEffect;
        }
    }

    private void BuyHpPotion(int cnt)
    {
        if (_gold > _hpPotionPrice * cnt)
        {
            _gold -= _hpPotionPrice * cnt;
            _hpPotionCnt += cnt;
            FloatTip("agentid为" + agentid + "的个体购买了血瓶, 当前血瓶数量为" + _hpPotionCnt + " 剩余金币为" + _gold);
        }
    }

    private void BuyExpScroll(int cnt)
    {
        if (_gold > _expScrollPrice * cnt)
        {
            AddReward(10 * cnt); // 加经验给奖励
            _gold -= _expScrollPrice * cnt;
            Exp += _expScrollEffect * cnt;
            FloatTip("agentid为" + agentid + "的个体购买并使用了经验卷轴");
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = 1;
        }

        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }

        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = -1;
        }

        if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1;
        }
    }

    public void End()
    {
        EndEpisode();
    }

    public override void OnEpisodeBegin()
    {
        Debug.LogError("agent初始化, agentid为" + agentid + "血量为" + _hp + "等级为" + _level + "经验值为" + _exp + "金币数量为" + _gold);
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

        SetResetParameters();
    }

    public void SetAgentScale()
    {
        float agentScale = _ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void SetResetParameters()
    {
        SetAgentScale();
    }

    public int segments = 50;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        float deltaTheta = (2f * Mathf.PI) / segments;

        Vector3 firstPoint = transform.position + new Vector3(area.interactRange, 0, 0);
        Vector3 lastPoint = firstPoint;

        for (int i = 1; i <= segments; i++)
        {
            float theta = i * deltaTheta;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(theta) * area.interactRange, 0, Mathf.Sin(theta) * area.interactRange);
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }

        Gizmos.DrawLine(lastPoint, firstPoint);
    }

}
