using System;
using System.Collections.Generic;

namespace Domain.RateLimiting.Core
{
    /// <summary>
    /// Represents the rate limiting policy for a single endpoint
    /// </summary>
    public class RateLimitPolicyParameters
    {
        private const string AllHttpMethods = "*";
        private const string AllRequestPaths = "*";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestKey"></param> 
        /// <param name="policies"></param>
        /// <param name="canOverrideIfNoPollicies"></param>
        public RateLimitPolicyParameters(string requestKey, IList<RateLimitPolicy> policies, 
            bool canOverrideIfNoPollicies = true) : 
            this(requestKey, AllRequestPaths, AllHttpMethods, policies, canOverrideIfNoPollicies)
        { }
        public RateLimitPolicyParameters(string requestKey, bool canOverrideIfNoPollicies = true) :
            this(requestKey, AllRequestPaths, AllHttpMethods, new List<RateLimitPolicy>(), canOverrideIfNoPollicies)
        { }

        public RateLimitPolicyParameters(string requestKey, string httpMethod, IList<RateLimitPolicy> policies, 
            bool canOverrideIfNoPollicies = true) :
            this(requestKey, AllRequestPaths, httpMethod, policies, canOverrideIfNoPollicies)
        { }

        public RateLimitPolicyParameters(string requestKey, IList<RateLimitPolicy> policies, string path,
            bool canOverrideIfNoPollicies = true) :
            this(requestKey, path, AllHttpMethods, policies, canOverrideIfNoPollicies)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        /// <param name="requestKey">The requestKey provided by the client.</param>
        /// <param name="routeTemplate">The route template.</param>
        /// <param name="policies">The policies.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="canOverrideIfNoPollicies"></param>
        /// <exception cref="ArgumentOutOfRangeException">limit</exception>
        /// <exception cref="ArgumentNullException"><paramref name="routeTemplate" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">limit</exception>
        public RateLimitPolicyParameters(string requestKey, string routeTemplate, string httpMethod, 
            IList<RateLimitPolicy> policies, bool canOverrideIfNoPollicies = true)
        {
            if (string.IsNullOrWhiteSpace(requestKey)) throw new ArgumentNullException(nameof(requestKey), 
                "requestKey cannot be null or whitespace");

            if (requestKey.Length == 0) throw new ArgumentOutOfRangeException(nameof(requestKey), 
                "requestKey cannot be empty");

            //if (routeTemplate == null) throw new ArgumentNullException(nameof(routeTemplate));
            //if (routeTemplate.Length == 0) throw new ArgumentOutOfRangeException(nameof(routeTemplate), 
            //    "the routeTemplate to rate limit cannot be empty");

            if (string.IsNullOrWhiteSpace(routeTemplate) || routeTemplate.Length == 0)
                routeTemplate = AllRequestPaths;

            if (string.IsNullOrWhiteSpace(httpMethod))
                httpMethod = AllHttpMethods;

            RequestKey = requestKey;
            RouteTemplate = routeTemplate;
            HttpMethod = httpMethod;
            Policies = policies;
            CanOverrideIfNoPollicies = canOverrideIfNoPollicies;
            Key = new RateLimitingPolicyParametersKey(RequestKey, routeTemplate, httpMethod);
        }

        /// <summary>
        /// Gets the policies hash key.
        /// </summary>
        /// <value>The policies hash key.
        /// </value>
        public RateLimitingPolicyParametersKey Key { get; }

        /// <summary>
        /// The policies to apply
        /// </summary>
        /// <value>The policies to apply</value>
        public IList<RateLimitPolicy> Policies { get; }

        public bool CanOverrideIfNoPollicies { get; }

        /// <summary>
        /// Gets the path the path to apply the specified rate limit</summary>
        /// <value>The path to rate limit.</value>
        public string RouteTemplate { get; }

        /// <summary>
        /// Get the http method to limit on for the specified path
        /// </summary>
        /// <value>The HTTP method.</value>
        public string HttpMethod { get; }

        /// <summary>
        /// 
        /// </summary>
        public string RequestKey { get; }
    }
}