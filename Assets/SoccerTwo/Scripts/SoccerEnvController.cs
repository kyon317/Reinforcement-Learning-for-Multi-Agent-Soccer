using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Integrated Soccer Environment Controller supporting both group rewards (POCA/PPO) and individual rewards (SAC).
/// </summary>
public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    
    /// <summary>
    /// Enable evaluation logging (optional feature from ml-agents workspace)
    /// </summary>
    [Tooltip("Enable Evaluation Logging")] public bool enableEvaluationLogging = false;
    
    [Tooltip("Evaluation Log File Path")] public string evaluationLogPath = "evaluation_results.txt";
    
    private bool isInferenceMode = false;

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;

    // Multi-agent groups for POCA/PPO compatibility
    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;

    void Start()
    {
        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        
        // Initialize TeamManager (for POCA/PPO compatibility)
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);
        
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            
            // Register agents to groups (for POCA/PPO compatibility)
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        
        // Check if agents are in inference mode (for evaluation logging)
        if (enableEvaluationLogging)
        {
            CheckInferenceMode();
            
            // Initialize evaluation log file if enabled
            if (isInferenceMode)
            {
                InitializeEvaluationLog();
            }
        }
        
        ResetScene();
    }
    
    void CheckInferenceMode()
    {
        // Check if any agent is using an ONNX model (inference mode)
        foreach (var item in AgentsList)
        {
            var behaviorParams = item.Agent.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            if (behaviorParams != null && behaviorParams.Model != null)
            {
                isInferenceMode = true;
                Debug.Log("Inference mode detected - evaluation logging enabled");
                return;
            }
        }
        isInferenceMode = false;
    }
    
    void InitializeEvaluationLog()
    {
        // Create or clear the evaluation log file
        string fullPath = Path.Combine(Application.dataPath, "..", evaluationLogPath);
        File.WriteAllText(fullPath, "");
        Debug.Log($"Evaluation log initialized at: {fullPath}");
    }
    
    void LogMatchResult(int result)
    {
        if (!enableEvaluationLogging || !isInferenceMode)
            return;
            
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "..", evaluationLogPath);
            File.AppendAllText(fullPath, result.ToString() + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to log match result: {e.Message}");
        }
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            // Log timeout/draw (0) if evaluation logging is enabled
            if (enableEvaluationLogging && isInferenceMode)
            {
                LogMatchResult(0);
            }
            
            // Support both group-based (POCA/PPO) and individual (SAC) episode ending
            // Individual episode ending for SAC compatibility
            foreach (var item in AgentsList)
            {
                item.Agent.EndEpisode();
            }
            
            // Group episode interruption for POCA/PPO compatibility
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            
            ResetScene();
        }
    }

    public void ResetBall()
    {
        var randomPosX = Random.Range(-2.5f, 2.5f);
        var randomPosZ = Random.Range(-2.5f, 2.5f);

        ball.transform.position = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    public void GoalTouched(Team scoredTeam)
    {
        float winningReward = Mathf.Max(1f, 2 - (float)m_ResetTimer / MaxEnvironmentSteps);
        float losingReward = -1f;

        // Log match result if evaluation logging is enabled
        if (enableEvaluationLogging && isInferenceMode)
        {
            if (scoredTeam == Team.Blue)
            {
                LogMatchResult(1);  // Blue win
            }
            else
            {
                LogMatchResult(2);  // Purple win
            }
        }

        // Support both group rewards (POCA/PPO) and individual rewards (SAC)
        if (scoredTeam == Team.Blue)
        {
            // Group rewards for POCA/PPO compatibility
            m_BlueAgentGroup.AddGroupReward(winningReward);
            m_PurpleAgentGroup.AddGroupReward(losingReward);
            
            // Individual rewards for SAC compatibility (also works with PPO)
            foreach (var item in AgentsList)
            {
                if (item.Agent.team == Team.Blue)
                {
                    item.Agent.AddReward(winningReward);
                }
                else
                {
                    item.Agent.AddReward(losingReward);
                }
            }
        }
        else
        {
            // Group rewards for POCA/PPO compatibility
            m_PurpleAgentGroup.AddGroupReward(winningReward);
            m_BlueAgentGroup.AddGroupReward(losingReward);
            
            // Individual rewards for SAC compatibility (also works with PPO)
            foreach (var item in AgentsList)
            {
                if (item.Agent.team == Team.Purple)
                {
                    item.Agent.AddReward(winningReward);
                }
                else
                {
                    item.Agent.AddReward(losingReward);
                }
            }
        }
        
        // End episodes - support both group and individual ending
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        
        // Also end episodes individually for SAC compatibility
        foreach (var item in AgentsList)
        {
            item.Agent.EndEpisode();
        }
        
        ResetScene();
    }

    public void ResetScene()
    {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }
}