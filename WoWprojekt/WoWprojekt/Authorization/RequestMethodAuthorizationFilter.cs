using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WoWprojekt.Authorization;

public class RequestMethodAuthorizationFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            return;

        var method = context.HttpContext.Request.Method?.ToUpperInvariant() ?? string.Empty;

        if (method == "GET" || method == "HEAD" || method == "OPTIONS")
            return;

        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            await Task.CompletedTask;
            return;
        }

        if (user.IsInRole("admin"))
            return;

        // Moderator može samo Edit POST
        if (user.IsInRole("moderator"))
        {
            var routeData = context.RouteData;
            var action = routeData.Values["action"]?.ToString()?.ToLowerInvariant();
            if (method == "POST" && action == "edit")
                return;
        }

        context.Result = new ForbidResult();

        await Task.CompletedTask;
    }
}
