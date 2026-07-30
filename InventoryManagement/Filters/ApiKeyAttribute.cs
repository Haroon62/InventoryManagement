using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace InventoryManagement.Filters;

/// <summary>
/// A simple API Key filter that checks for an "X-Api-Key" header in the request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Check if the API key header exists in the request
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key was not provided.");
            return;
        }

        // 2. Fetch the expected API key from appsettings.json
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration.GetValue<string>("ApiKey");

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new StatusCodeResult(500); // Server configuration error
            return;
        }

        // 3. Compare the keys
        if (!apiKey.Equals(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("Unauthorized client.");
            return;
        }

        // 4. Continue to the action if authorized
        await next();
    }
}
