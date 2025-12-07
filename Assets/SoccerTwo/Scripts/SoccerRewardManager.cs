using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Manages reward calculation for Soccer agents based on training policy.
/// Extracted from AgentSoccer for better code organization.
/// </summary>
public class SoccerRewardManager
{
    private AgentSoccer m_Agent;
    private SoccerTrainingPolicy m_PolicyType;
    private bool m_UseComplexRewards;
    
    // Reward coefficients
    private float m_Existential;
    private float m_BallTouch;
    private float m_ShootDistanceThreshold = 5f;
    
    // Tracking variables
    private GameObject m_Ball;
    private Vector3 m_PreviousBallPosition;
    private Vector3 m_OpposingGoalPosition;
    private Vector3 m_OwnGoalPosition;
    private float m_PreviousBallDistanceValue;
    
    public SoccerRewardManager(AgentSoccer agent, SoccerTrainingPolicy policyType, bool useComplexRewards)
    {
        m_Agent = agent;
        m_PolicyType = policyType;
        m_UseComplexRewards = useComplexRewards;
    }
    
    public void Initialize(float existential, GameObject ball, Vector3 opposingGoalPos, Vector3 ownGoalPos)
    {
        m_Existential = existential;
        m_Ball = ball;
        m_OpposingGoalPosition = opposingGoalPos;
        m_OwnGoalPosition = ownGoalPos;
        
        if (m_Ball != null)
        {
            m_PreviousBallDistanceValue = Vector3.Distance(m_Agent.transform.position, m_Ball.transform.position);
            m_PreviousBallPosition = m_Ball.transform.position;
        }
    }
    
    public void SetBallTouch(float ballTouch)
    {
        m_BallTouch = ballTouch;
    }
    
    public void UpdateTracking()
    {
        if (m_Ball != null)
        {
            m_PreviousBallDistanceValue = Vector3.Distance(m_Agent.transform.position, m_Ball.transform.position);
            m_PreviousBallPosition = m_Ball.transform.position;
        }
    }
    
    /// <summary>
    /// Calculate and add step rewards based on policy type
    /// </summary>
    public void CalculateStepRewards(AgentSoccer.Position position)
    {
        if (m_PolicyType == SoccerTrainingPolicy.SAC && m_UseComplexRewards)
        {
            CalculateSACStepRewards(position);
        }
        else
        {
            CalculatePOCOStepRewards(position);
        }
    }
    
    /// <summary>
    /// Simple reward structure for POCA/PPO
    /// </summary>
    private void CalculatePOCOStepRewards(AgentSoccer.Position position)
    {
        if (position == AgentSoccer.Position.Goalie)
        {
            m_Agent.AddReward(m_Existential);
        }
        else if (position == AgentSoccer.Position.Striker)
        {
            m_Agent.AddReward(-m_Existential);
        }
    }
    
    /// <summary>
    /// Complex reward structure for SAC
    /// </summary>
    private void CalculateSACStepRewards(AgentSoccer.Position position)
    {
        // Existential reward
        if (position == AgentSoccer.Position.Goalie)
        {
            m_Agent.AddReward(m_Existential);
        }
        else if (position == AgentSoccer.Position.Striker)
        {
            m_Agent.AddReward(-m_Existential * 0.1f);
        }
        
        // Encourage approaching the ball with shaped reward
        if (m_Ball != null && position != AgentSoccer.Position.Goalie)
        {
            float currentBallDistance = Vector3.Distance(m_Agent.transform.position, m_Ball.transform.position);
            
            // Reward for getting closer to the ball
            float distanceReward = (m_PreviousBallDistanceValue - currentBallDistance) * 0.05f;
            m_Agent.AddReward(distanceReward);
            
            // Reward for facing towards the ball
            Vector3 directionToBall = (m_Ball.transform.position - m_Agent.transform.position).normalized;
            float dotProduct = Vector3.Dot(m_Agent.transform.forward, directionToBall);
            float facingReward = Mathf.Max(0f, dotProduct) * 0.005f;
            m_Agent.AddReward(facingReward);
            
            m_PreviousBallDistanceValue = currentBallDistance;
            
            // Bonus for being close to the ball (encourages ball control)
            if (currentBallDistance < 3f)
            {
                float proximityBonus = 0.01f * (5f - currentBallDistance) / 5f;
                m_Agent.AddReward(proximityBonus);
            }
        }
        
        // Ball progress reward (push toward opposing goal) and regression penalty (toward own goal)
        if (m_Ball != null && m_OwnGoalPosition != Vector3.zero && m_OpposingGoalPosition != Vector3.zero)
        {
            Vector3 currentBallPos = m_Ball.transform.position;
            
            // Calculate ball progress toward opposing goal
            float prevDistanceToOpposingGoal = Vector3.Distance(m_PreviousBallPosition, m_OpposingGoalPosition);
            float currentDistanceToOpposingGoal = Vector3.Distance(currentBallPos, m_OpposingGoalPosition);
            float ballProgress = prevDistanceToOpposingGoal - currentDistanceToOpposingGoal;
            
            // Reward for pushing ball toward opposing goal
            if (ballProgress > 0f)
            {
                float pushReward = ballProgress * 0.05f;
                m_Agent.AddReward(pushReward);
                
                // Additional reward if agent is controlling the ball (close to ball)
                float distanceToBall = Vector3.Distance(m_Agent.transform.position, currentBallPos);
                if (distanceToBall < 3f)
                {
                    m_Agent.AddReward(pushReward * 1.5f);  // Extra reward for controlling ball while pushing
                }
            }
            
            // Penalty for ball moving toward own goal
            if (m_OwnGoalPosition != Vector3.zero)
            {
                float prevDistanceToOwnGoal = Vector3.Distance(m_PreviousBallPosition, m_OwnGoalPosition);
                float currentDistanceToOwnGoal = Vector3.Distance(currentBallPos, m_OwnGoalPosition);
                float ballRegress = prevDistanceToOwnGoal - currentDistanceToOwnGoal;
                
                if (ballRegress > 0f)
                {
                    float regressionPenalty = ballRegress * 0.03f;
                    m_Agent.AddReward(-regressionPenalty);
                }
            }
            
            // Update previous ball position for next frame
            m_PreviousBallPosition = currentBallPos;
            
            // Shooting zone bonus (ball is within shooting distance)
            if (currentDistanceToOpposingGoal < m_ShootDistanceThreshold)
            {
                float shootZoneBonus = (m_ShootDistanceThreshold - currentDistanceToOpposingGoal) / m_ShootDistanceThreshold * 0.01f;
                m_Agent.AddReward(shootZoneBonus);
            }
        }
    }
    
    /// <summary>
    /// Calculate and add collision rewards
    /// </summary>
    public void CalculateCollisionRewards(Collision collision, AgentSoccer.Position position)
    {
        if (collision.gameObject.CompareTag("ball"))
        {
            // Touch reward
            float touchReward = 0.2f * m_BallTouch;
            if (m_BallTouch == 0f)
            {
                touchReward = 0.2f;
            }
            m_Agent.AddReward(touchReward);
            
            // Update ball position after collision (for progress tracking)
            if (m_Ball != null)
            {
                m_PreviousBallDistanceValue = Vector3.Distance(m_Agent.transform.position, m_Ball.transform.position);
                m_PreviousBallPosition = m_Ball.transform.position;
            }
            
            // Shooting reward (only for SAC with complex rewards)
            if (m_PolicyType == SoccerTrainingPolicy.SAC && m_UseComplexRewards && m_OpposingGoalPosition != Vector3.zero)
            {
                Vector3 ballPos = collision.gameObject.transform.position;
                float distanceToOpposingGoal = Vector3.Distance(ballPos, m_OpposingGoalPosition);
                
                if (distanceToOpposingGoal < m_ShootDistanceThreshold)
                {
                    // Shooting reward based on distance to goal (closer = more reward)
                    float shootReward = 0.5f * (m_ShootDistanceThreshold - distanceToOpposingGoal) / m_ShootDistanceThreshold;
                    m_Agent.AddReward(shootReward);
                    
                    // Check if kick direction is aligned with goal
                    Vector3 ballToGoal = (m_OpposingGoalPosition - ballPos).normalized;
                    Vector3 kickDirection = (collision.contacts[0].point - m_Agent.transform.position).normalized;
                    float alignment = Vector3.Dot(ballToGoal, kickDirection);
                    
                    // Additional reward if kick direction is toward goal (alignment > 0.5)
                    if (alignment > 0.5f)
                    {
                        m_Agent.AddReward(shootReward * 0.5f);  // Direction reward for accurate shooting
                    }
                }
            }
        }
    }
}
