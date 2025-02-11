using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class NpcAgent : Agent
{
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

    private float AtkCD
    {
        get
        {
            return _minAtkCD + (float)(_maxLevel - _level) / _maxLevel * (_maxAtkCD - _minAtkCD);
        }
    }

    private float _hp; // 当前血量
    private float _maxHp; // 最大血量, 与等级相关
    private int _level; // 当前等级
    private int _maxLevel = 10; // 最大等级
    private float _exp; // 经验值
    private float _materialNum; // 怪物材料数量, 可以用来换钱

    // agent level进阶需要的经验值
    private List<(int, float)> expTable = new List<(int, float)>()
    {
        (1, 100),
        (2, 200),
        (3, 400),
        (4, 800),
        (5, 1600),
        (6, 3200),
        (7, 6400),
        (8, 12800),
        (9, 25600),
        (10, 51200),
    };

    private bool _inCombat; // 战斗中移动速度下降
    private bool _inShop; // 在商店附近一定范围内不会被攻击, 会自动回血, 可以购买道具

    private EnvironmentParameters _ResetParams;

    public override void Initialize()
    {
        _AgentRb = GetComponent<Rigidbody>();
        _ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        var localVelocity = transform.InverseTransformDirection(_AgentRb.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        sensor.AddObservation(_hp);
        sensor.AddObservation(_maxHp);
        sensor.AddObservation(_materialNum);
        sensor.AddObservation(_level);
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
        }

        var buyPotionCommand = discreteActions[0] > 0;
        if (buyPotionCommand)
        {
        }

        var buyExpCommand = discreteActions[0] > 0;
        if (buyExpCommand)
        {
        }

        var atkAgentCommand = discreteActions[0] > 0;
        if (atkAgentCommand)
        {
        }

        var atkMonsterCommand = discreteActions[0] > 0;
        if (atkMonsterCommand)
        {
        }

        if (_inCombat) // 战斗中 速度上限12.5f
        {
            if (_AgentRb.velocity.sqrMagnitude > 12.5f)
            {
                _AgentRb.velocity *= 0.95f;
            }
        }
        else // 非战斗中, 速度上线25f
        {
            if (_AgentRb.velocity.sqrMagnitude > 25f)
            {
                _AgentRb.velocity *= 0.95f;
            }
        }
    }

    private void UseHpPotion()
    {
        if (_hpPotionCnt > 0)
        {
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
        }
    }

    private void BuyExpScroll(int cnt)
    {
        if (_gold > _expScrollPrice * cnt)
        {
            _gold -= _expScrollPrice * cnt;
            _exp += _expScrollEffect * cnt;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // var continuousActionsOut = actionsOut.ContinuousActions;
        // if (Input.GetKey(KeyCode.D))
        // {
        //     continuousActionsOut[2] = 1;
        // }
        // if (Input.GetKey(KeyCode.W))
        // {
        //     continuousActionsOut[0] = 1;
        // }
        // if (Input.GetKey(KeyCode.A))
        // {
        //     continuousActionsOut[2] = -1;
        // }
        // if (Input.GetKey(KeyCode.S))
        // {
        //     continuousActionsOut[0] = -1;
        // }
        // var discreteActionsOut = actionsOut.DiscreteActions;
        // discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public override void OnEpisodeBegin()
    {
        // Unfreeze();
        // Unpoison();
        // Unsatiate();
        // _AgentRb.velocity = Vector3.zero;
        // myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        // transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range),
        //     2f, Random.Range(-m_MyArea.range, m_MyArea.range))
            // + area.transform.position;
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
}
