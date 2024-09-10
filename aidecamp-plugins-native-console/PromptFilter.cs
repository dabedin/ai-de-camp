using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

class PromptFilterTrace : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        // Example: get function information
        var functionName = context.Function.Name;

        await next(context);

        // Example: override rendered prompt before sending it to AI
        //context.RenderedPrompt = "Respond with following text: Prompt from filter.";

        Console.WriteLine($"PromptFilterExample: {functionName}");
        Console.WriteLine($"PromptFilterExample: {context.RenderedPrompt}");
    }
}