using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WoWprojekt.Authorization;

public class RequestMethodAuthorizationFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Allow endpoints that explicitly allow anonymous
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
        {
            return;
        }

        var method = context.HttpContext.Request.Method?.ToUpperInvariant() ?? string.Empty;

        // Safe methods: allow anonymous reads
        if (method == "GET" || method == "HEAD" || method == "OPTIONS")
        {
            return;
        }

        // For unsafe methods require admin role
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated == true && user.IsInRole("admin"))
        {
            return;
        }

        // If not authenticated -> challenge; if authenticated but not admin -> forbid
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
        }
        else
        {
            context.Result = new ForbidResult();
        }

        await Task.CompletedTask;
    }
}
