using UnityEngine;

/// <summary>
/// Training policy types for Soccer environment.
/// POCA and PPO use the same reward structure, SAC uses a different one.
/// </summary>
public enum SoccerTrainingPolicy
{
    POCA,  // Uses group rewards and simple reward structure
    PPO,   // Uses individual rewards but same structure as POCA
    SAC    // Uses individual rewards with complex shaped rewards
}

/// <summary>
/// Configuration for different training policies
/// </summary>
[System.Serializable]
public class SoccerPolicyConfig
{
    [Tooltip("The training policy type")]
    public SoccerTrainingPolicy policyType = SoccerTrainingPolicy.SAC;
    
    [Tooltip("Whether to use complex shaped rewards (only for SAC)")]
    public bool useComplexRewards = true;
    
    [Tooltip("Behavior name for the trainer (should match YAML config)")]
    public string behaviorName = "SoccerTwos";
}
