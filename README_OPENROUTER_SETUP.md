# OpenRouter API Setup Guide

This project integrates OpenRouter with Microsoft Semantic Kernel to provide AI capabilities for robot personalities.
This is not for setting up OpenRouter with Github Copilot.

## Configuration

### Setting up the OpenRouter API Key

The OpenRouter API key needs to be configured securely and should never be committed to the repository.

#### For Local Development

1. **Using User Secrets (Recommended)**:
   ```bash
   cd AIRobotControl.Server
   dotnet user-secrets init
   dotnet user-secrets set "OpenRouter:ApiKey" "YOUR_API_KEY_HERE"
   ```

2. **Using Environment Variables**:
   ```bash
   export OpenRouter__ApiKey="YOUR_API_KEY_HERE"  # Linux/Mac
   set OpenRouter__ApiKey=YOUR_API_KEY_HERE       # Windows CMD
   $env:OpenRouter__ApiKey="YOUR_API_KEY_HERE"    # PowerShell
   ```

3. **Using appsettings.Development.json** (NOT recommended for production):
   Add to `appsettings.Development.json`:
   ```json
   {
     "OpenRouter": {
       "ApiKey": "YOUR_API_KEY_HERE"
     }
   }
   ```
   **Important**: Never commit this file with your actual API key!

#### For DevContainers

When using DevContainers, you need to forward the API key from your host environment:

1. **Create a `.env` file** in the root of your repository (add it to `.gitignore`):
   ```
   OPENROUTER_API_KEY=YOUR_API_KEY_HERE
   ```

2. **Configure DevContainer** to forward the environment variable:
   In `.devcontainer/devcontainer.json`, add:
   ```json
   {
     "remoteEnv": {
       "OpenRouter__ApiKey": "${localEnv:OPENROUTER_API_KEY}"
     }
   }
   ```

3. **Alternative: Using GitHub Codespaces Secrets**:
   - Go to your GitHub repository settings
   - Navigate to Secrets and variables > Codespaces
   - Add a new repository secret named `OPENROUTER_API_KEY`
   - The secret will be automatically available in Codespaces

#### For Production Deployment

1. **Azure Key Vault** (Recommended for Azure deployments):
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri($"https://{vaultName}.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

2. **Environment Variables on the server**:
   Set the environment variable on your production server through your hosting provider's configuration panel.

## Getting an OpenRouter API Key

1. Visit [OpenRouter.ai](https://openrouter.ai)
2. Sign up for an account
3. Navigate to your API Keys section
4. Create a new API key
5. Copy the key (it starts with `sk-or-v1-`)

## Testing the Configuration

Run the integration tests to verify your configuration:

```bash
cd AIRobotControl.Server.Tests
dotnet test --filter "FullyQualifiedName~SemanticKernelOpenRouterTests"
```

## Available Models

The following models are configured and tested to work with this implementation:

- **Free Models**:
  - `google/gemma-2-9b-it:free` - Google's Gemma 2 9B model
  - `meta-llama/llama-3.2-3b-instruct:free` - Meta's Llama 3.2 3B model
  
- **Premium Models** (requires credits):
  - `openai/gpt-5-nano` - OpenAI's GPT-5 nano model
  - `openai/gpt-4-turbo` - OpenAI's GPT-4 Turbo
  - `anthropic/claude-3-haiku` - Anthropic's Claude 3 Haiku

## Configuration Options

The AI system can be configured in `appsettings.json`:

```json
{
  "AI": {
    "Enabled": true,
    "DefaultModel": "google/gemma-2-9b-it:free",
    "MaxTokens": 500,
    "Temperature": 0.7
  }
}
```

## Security Best Practices

1. **Never commit API keys** to your repository
2. **Use different API keys** for development, testing, and production
3. **Rotate API keys regularly**
4. **Monitor usage** through the OpenRouter dashboard
5. **Set spending limits** in your OpenRouter account

## Troubleshooting

### Common Issues

1. **404 Not Found Error**: 
   - Verify the model name is correct
   - Check if the model requires function calling capability
   - Ensure your API key is valid

2. **Authentication Error**:
   - Verify your API key is correctly set
   - Check if the API key has the necessary permissions

3. **Rate Limiting**:
   - OpenRouter has rate limits based on your plan
   - Consider implementing retry logic with exponential backoff

### Debug Logging

Enable debug logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.SemanticKernel": "Debug"
    }
  }
}
```