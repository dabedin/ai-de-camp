using Microsoft.SemanticKernel;
using System.ComponentModel;
public class TimeInformationPlugin
{
    [KernelFunction]
    [Description("Retrieves the current time in UTC.")]
    public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
}