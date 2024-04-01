/* 
*   Function
*   Copyright © 2024 NatML Inc. All rights reserved.
*/

#nullable enable
#pragma warning disable 8618

namespace Function.Types {

    using System;
    using Internal;

    /// <summary>
    /// Predictor environment variable.
    /// </summary>
    [Preserve, Serializable]
    public class EnvironmentVariable {

        /// <summary>
        /// Environment variable name.
        /// </summary>
        public string name;

        /// <summary>
        /// Environment variable value.
        /// </summary>
        public string? value;
    }
}