using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum Team
{
    Blue = 0,
    Purple = 1
}

/// <summary>
/// Integrated Soccer Agent supporting multiple training policies (POCA, PPO, SAC).
/// </summary>
public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [Header("Training Policy Configuration")]
    [Tooltip("Training policy configuration. Can be set manually or auto-detected from BehaviorName.")]
    public SoccerPolicyConfig policyConfig = new SoccerPolicyConfig();
    
    [Tooltip("Auto-detect policy type from BehaviorName (overrides manual config)")]
    public bool autoDetectPolicy = true;

    [HideInInspector]
    public Team team;
    float m_KickPower;
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;
    
    // Object references
    GameObject m_Ball;
    GameObject m_OwnGoal;
    GameObject m_OpposingGoal;
    Vector3 m_OpposingGoalPosition;
    Vector3 m_OwnGoalPosition;
    
    // Reward manager
    SoccerRewardManager m_RewardManager;
    SoccerTrainingPolicy m_CurrentPolicy;
    bool m_UseComplexRewards;

    void Start()
    {
        FindBallObject();
        FindGoalObjects();
    }

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        
        // Detect policy type from BehaviorName if auto-detect is enabled
        if (autoDetectPolicy && m_BehaviorParameters != null)
        {
            DetectPolicyFromBehaviorName();
        }
        else
        {
            m_CurrentPolicy = policyConfig.policyType;
            m_UseComplexRewards = policyConfig.useComplexRewards;
        }
        
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }
        
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        
        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        if (Academy.Instance != null)
        {
            m_ResetParams = Academy.Instance.EnvironmentParameters;
        }
        
        FindBallObject();
        FindGoalObjects();
        
        // Initialize reward manager
        InitializeRewardManager();
    }
    
    /// <summary>
    /// Detect policy type from BehaviorName
    /// </summary>
    private void DetectPolicyFromBehaviorName()
    {
        if (m_BehaviorParameters == null) return;
        
        string behaviorName = m_BehaviorParameters.BehaviorName.ToLower();
        
        // Check for SAC indicators
        if (behaviorName.Contains("sac") || behaviorName.Contains("soft"))
        {
            m_CurrentPolicy = SoccerTrainingPolicy.SAC;
            m_UseComplexRewards = true;
        }
        // Check for PPO indicators
        else if (behaviorName.Contains("ppo") || behaviorName.Contains("proximal"))
        {
            m_CurrentPolicy = SoccerTrainingPolicy.PPO;
            m_UseComplexRewards = false;
        }
        // Check for POCA indicators
        else if (behaviorName.Contains("poca") || behaviorName.Contains("coord"))
        {
            m_CurrentPolicy = SoccerTrainingPolicy.POCA;
            m_UseComplexRewards = false;
        }
        else
        {
            // Default to config values
            m_CurrentPolicy = policyConfig.policyType;
            m_UseComplexRewards = policyConfig.useComplexRewards;
        }
    }
    
    /// <summary>
    /// Initialize the reward manager based on current policy
    /// </summary>
    private void InitializeRewardManager()
    {
        m_RewardManager = new SoccerRewardManager(this, m_CurrentPolicy, m_UseComplexRewards);
        m_RewardManager.Initialize(m_Existential, m_Ball, m_OpposingGoalPosition, m_OwnGoalPosition);
    }
    
    private void FindBallObject()
    {
        m_Ball = GameObject.FindGameObjectWithTag("ball");
    }
    
    private void FindGoalObjects()
    {
        if (m_BehaviorParameters == null)
        {
            return;
        }
        
        if (team == Team.Blue)
        {
            m_OwnGoal = GameObject.FindGameObjectWithTag("blueGoal");
            m_OpposingGoal = GameObject.FindGameObjectWithTag("purpleGoal");
        }
        else if (team == Team.Purple)
        {
            m_OwnGoal = GameObject.FindGameObjectWithTag("purpleGoal");
            m_OpposingGoal = GameObject.FindGameObjectWithTag("blueGoal");
        }
        
        if (m_OwnGoal != null)
        {
            m_OwnGoalPosition = m_OwnGoal.transform.position;
        }
        
        if (m_OpposingGoal != null)
        {
            m_OpposingGoalPosition = m_OpposingGoal.transform.position;
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
                ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Find missing objects if needed
        if (m_Ball == null || m_OwnGoal == null || m_OpposingGoal == null)
        {
            FindBallObject();
            FindGoalObjects();
            
            // Re-initialize reward manager if objects were just found
            if (m_RewardManager != null && m_Ball != null)
            {
                m_RewardManager.Initialize(m_Existential, m_Ball, m_OpposingGoalPosition, m_OwnGoalPosition);
            }
        }

        // Calculate rewards using reward manager
        if (m_RewardManager != null)
        {
            m_RewardManager.CalculateStepRewards(position);
        }
        else
        {
            // Fallback to basic rewards if reward manager not initialized
            if (position == Position.Goalie)
            {
                AddReward(m_Existential);
            }
            else if (position == Position.Striker)
            {
                AddReward(-m_Existential);
            }
        }
        
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }
    
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        
        if (c.gameObject.CompareTag("ball"))
        {
            // Calculate collision rewards using reward manager
            if (m_RewardManager != null)
            {
                m_RewardManager.CalculateCollisionRewards(c, position);
            }
            else
            {
                // Fallback: basic touch reward
                float touchReward = 0.2f * m_BallTouch;
                if (m_BallTouch == 0f)
                {
                    touchReward = 0.2f;
                }
                AddReward(touchReward);
            }
            
            // Apply kick force
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
            
            // Update reward manager tracking
            if (m_RewardManager != null)
            {
                m_RewardManager.UpdateTracking();
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        if (m_ResetParams != null)
        {
            m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
            if (m_RewardManager != null)
            {
                m_RewardManager.SetBallTouch(m_BallTouch);
            }
        }
        
        FindBallObject();
        FindGoalObjects();
        
        // Re-initialize reward manager with updated positions
        if (m_Ball != null && m_OwnGoalPosition != Vector3.zero && m_OpposingGoalPosition != Vector3.zero)
        {
            if (m_RewardManager != null)
            {
                m_RewardManager.Initialize(m_Existential, m_Ball, m_OpposingGoalPosition, m_OwnGoalPosition);
                m_RewardManager.UpdateTracking();
            }
        }
    }
}