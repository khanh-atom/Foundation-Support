using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Foundation.Infrastructure.Cms
{
    /// <summary>
    /// Middleware to rewrite requests for adminPlugin.js/css to admin-plugin.js/css
    /// This is a workaround for Advanced.CMS.ApprovalReviews bug where the view expects
    /// adminPlugin.js/css but the build outputs admin-plugin.js/css
    /// </summary>
    public class AdminPluginFileRewriteMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminPluginFileRewriteMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            // Rewrite adminPlugin.js to admin-plugin.js
            if (path != null && path.Contains("/advanced-cms-approval-reviews/") && path.Contains("/ClientResources/dist/"))
            {
                if (path.EndsWith("adminPlugin.js", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = path.Replace("adminPlugin.js", "admin-plugin.js", System.StringComparison.OrdinalIgnoreCase);
                }
                else if (path.EndsWith("adminPlugin.css", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = path.Replace("adminPlugin.css", "admin-plugin.css", System.StringComparison.OrdinalIgnoreCase);
                }
            }

            await _next(context);
        }
    }
}

