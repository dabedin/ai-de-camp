

## Configuring Secrets

The example require credentials to access Azure OpenAI.

If you have set up those credentials as secrets within Secret Manager or through environment variables for other samples from the solution in which this project is found, they will be re-used.

### To set your secrets with Secret Manager:

Use these commands:

```
dotnet user-secrets init

dotnet user-secrets set "AzureOpenAI:ApiKey" "..."
dotnet user-secrets set "AzureOpenAI:Endpoint" "..."
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt35-appA" # or any other name set for the deployment
dotnet user-secrets set "AzureOpenAI:ModelId" "gpt-3.5-turbo" # or any other function callable model.
```

### To set your secrets with environment variables

Use these names:

```
# Azure OpenAI
AzureOpenAI_ApiKey
AzureOpenAI_Endpoint
AzureOpenAI_DeploymentName
AzureOpenAI_ModelId
```