namespace AIRobotControl.Server.AI.Configuration;

public class AISettings
{
    public bool Enabled { get; set; } = false;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "google/gemma-2-9b-it:free";
    public int MaxTokens { get; set; } = 500;
    public double Temperature { get; set; } = 0.7;
    public bool EnableMcp { get; set; } = true;
}