﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.RateLimiting.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Domain.RateLimiting.AspNetCore
{
    /// <summary>
    ///     Action filter which rate limits requests using the action/controllers rate limit entry attribute.
    /// </summary>
    public class RateLimitingFilter : IAsyncAuthorizationFilter
    {
        private readonly IRateLimiter _rateLimiter;
        
        public RateLimitingFilter(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        }

        private static void AddUpdateRateLimitingSuccessHeaders(HttpContext context, RateLimitingResult result)
        {
            var successheaders = new Dictionary<string, string>()
            {
                {RateLimitHeaders.CallsRemaining, result.CallsRemaining.ToString()},
                {RateLimitHeaders.Limit, result.CacheKey.AllowedCallRate.ToString() }
            };

            foreach (var successheader in successheaders.Keys)
            {
                if (context.Response.Headers.ContainsKey(successheader))
                {
                    context.Response.Headers[successheader] = new StringValues(
                        context.Response.Headers[successheader].ToArray()
                            .Append(successheaders[successheader]).ToArray());
                }
                else
                {
                    context.Response.Headers.Add(successheader, new StringValues(
                        new string[] { successheaders[successheader] }));
                }
            }
        }

        private static void TooManyRequests(AuthorizationFilterContext context, 
            RateLimitingResult result, string violatedPolicyName = "")
        {
            var throttledResponseParameters =
                RateLimiter.GetThrottledResponseParameters(result, violatedPolicyName);
            context.HttpContext.Response.StatusCode = ThrottledResponseParameters.StatusCode;

            foreach (var header in throttledResponseParameters.RateLimitHeaders.Keys)
            {
                context.HttpContext.Response.Headers.Add(header, 
                    throttledResponseParameters.RateLimitHeaders[header]);
            }
            
            context.Result = new ContentResult()
            {
                Content = throttledResponseParameters.Message
            };
        }
        
        private static IList<AllowedCallRate> GetCustomAttributes(ActionDescriptor actionDescriptor)
        {
            var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null)
                return null;

            var policies = controllerActionDescriptor.MethodInfo.GetCustomAttributes<AllowedCallRate>(true)?.ToList();
            if (policies == null || !policies.Any())
                policies = controllerActionDescriptor.ControllerTypeInfo.
                    GetCustomAttributes<AllowedCallRate>(true)?.ToList();

            return policies;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext actionContext)
        {
            var context = actionContext;
            await _rateLimiter.LimitRequestAsync(
                new RateLimitingRequest(
                    actionContext.ActionDescriptor.AttributeRouteInfo.Template,
                    actionContext.HttpContext.Request.Path,
                    actionContext.HttpContext.Request.Method,
                    (header) => context.HttpContext.Request.Headers[header],
                    actionContext.HttpContext.User,
                    actionContext.HttpContext.Request.Body),
                () => GetCustomAttributes(actionContext.ActionDescriptor),
                actionContext.HttpContext.Request.Host.Value,
                async rateLimitingResult =>
                {
                    AddUpdateRateLimitingSuccessHeaders(actionContext.HttpContext, rateLimitingResult);
                    await Task.FromResult<object>(null);
                },
                async (rateLimitingResult, violatedPolicyName) =>
                {
                    TooManyRequests(actionContext, rateLimitingResult, violatedPolicyName);
                    await Task.FromResult<object>(null);
                }, 
                null).ConfigureAwait(false);
        }
    }
}