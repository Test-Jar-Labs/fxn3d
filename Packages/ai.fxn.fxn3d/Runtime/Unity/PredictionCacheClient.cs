/* 
*   Function
*   Copyright © 2024 NatML Inc. All rights reserved.
*/

#nullable enable

namespace Function.API {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CachedPrediction = Internal.FunctionSettings.CachedPrediction;

    /// <summary>
    /// Function API client for Unity Engine.
    /// This uses Unity APIs for performing web requests.
    /// Furthermore, this handles partial prediction caching for on-device predictors.
    /// </summary>
    internal sealed class PredictionCacheClient : UnityClient {

        #region --Client API--
        /// <summary>
        /// Create the client.
        /// </summary>
        /// <param name="url">Function API URL.</param>
        /// <param name="accessKey">Function access key.</param>
        /// <param name="cache">Prediction cache.</param>
        public PredictionCacheClient (
            string url,
            string? accessKey,
            List<CachedPrediction>? cache = default
        ) : base(url, accessKey) => this.cache = cache ?? new();

        /// <summary>
        /// Perform a request to a Function REST endpoint.
        /// </summary>
        /// <typeparam name="T">Deserialized response type.</typeparam>
        /// <param name="method">HTTP request method.</param>
        /// <param name="path">Endpoint path.</param>
        /// <param name="payload">Request body.</param>
        /// <param name="headers">Request body.</param>
        /// <returns>Deserialized response.</returns>
        public override async Task<T> Request<T> ( // INCOMPLETE // Move predictor caching logic into separate class.
            string method,
            string path,
            object? payload = default,
            Dictionary<string, string>? headers = default
        ) {
            path = path.TrimStart('/');
            // Check prediction
            if (!path.StartsWith(@"predict"))
                return await base.Request<T>(method, path, payload: payload, headers: headers);
            // Get cached prediction
            var url = $"{this.url}/{path}";
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');
            var tag = string.Join("/", segments[2], segments[3]);
            var platform = (headers?.TryGetValue(@"fxn-client", out var p) ?? false) ? p : null;
            var cachedPrediction = cache.FirstOrDefault(p => p.platform == platform && p.prediction.tag == tag);
            // Check
            if (cachedPrediction == null)
                return await base.Request<T>(method, path, payload: payload, headers: headers);
            // Update path
            var qs = $"partialPrediction={cachedPrediction.prediction.id}";
            var prefix = path.Contains("?") ? "&" : "?";
            path = $"{path}{prefix}{qs}";
            // Request
            return await base.Request<T>(method, path, payload: payload, headers: headers);
        }
        #endregion


        #region --Operations--
        private readonly List<CachedPrediction> cache;
        #endregion
    }
}