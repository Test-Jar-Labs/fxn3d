/* 
*   Function
*   Copyright © 2024 NatML Inc. All rights reserved.
*/

#nullable enable

namespace Function.Services {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using API;
    using Types;

    /// <summary>
    /// Manage predictor environment variables.
    /// </summary>
    public sealed class EnvironmentVariableService {

        #region --Client API--
        /// <summary>
        /// List the current user's environment variables.
        /// </summary>
        /// <param name="organization">Organization username.</param>
        public async Task<EnvironmentVariable[]?> List (string? organization = null) {
            var response = await client.Query<UserWithEnvironmentVariablesResponse>(
                @$"query ($input: UserInput) {{
                    user (input: $input) {{
                        ... on User {{
                            environmentVariables {{
                                {Fields}
                            }}
                        }}
                        ... on Organization {{
                            environmentVariables {{
                                {Fields}
                            }}
                        }}
                    }}
                }}",
                new () {
                    ["input"] = !string.IsNullOrEmpty(organization) ? new UserService.UserInput { username = organization } : null,
                }
            );
            return response.user?.environmentVariables;
        }

        /// <summary>
        /// Create an environment variables.
        /// </summary>
        /// <param name="name">Environment variable name.</param>
        /// <param name="value">Environment variable value.</param>
        /// <param name="organization">Organization username.</param>
        public async Task<EnvironmentVariable> Create (
            string name,
            string value,
            string? organization = null
        ) {
            var response = await client.Query<CreateEnvironmentVariableResponse>(
                @$"mutation ($input: CreateEnvironmentVariableInput!) {{
                    createEnvironmentVariable (input: $input) {{
                        {Fields}
                    }}
                }}",
                new () {
                    ["input"] = new CreateEnvironmentVariableInput { name = name, value = value, organization = organization }
                }
            );
            return response.createEnvironmentVariable;
        }

        /// <summary>
        /// Delete an environment variables.
        /// </summary>
        /// <param name="name">Environment variable name.</param>
        /// <param name="organization">Organization username.</param>
        public async Task<bool> Delete (
            string name,
            string? organization = null
        ) {
            var response = await client.Query<DeleteEnvironmentVariableResponse>(
                @$"mutation ($input: DeleteEnvironmentVariableInput!) {{
                    deleteEnvironmentVariable (input: $input)
                }}",
                new () {
                    ["input"] = new DeleteEnvironmentVariableInput { name = name, organization = organization }
                }
            );
            return response.deleteEnvironmentVariable;
        }
        #endregion


        #region --Operations--
        private readonly IFunctionClient client;
        public static string Fields = @$"
        name
        ";

        internal EnvironmentVariableService (IFunctionClient client) => this.client = client;
        #endregion


        #region --Types--

        public sealed class CreateEnvironmentVariableInput {
            public string name;
            public string value;
            public string? organization;
        }

        public sealed class DeleteEnvironmentVariableInput {
            public string name;
            public string? organization;
        }

        private sealed class UserWithEnvironmentVariablesResponse {
            public UserWithEnvironmentVariables? user;

            public sealed class UserWithEnvironmentVariables {
                public EnvironmentVariable[] environmentVariables;
            }
        }

        private sealed class CreateEnvironmentVariableResponse {
            public EnvironmentVariable createEnvironmentVariable;
        }

        private sealed class DeleteEnvironmentVariableResponse {
            public bool deleteEnvironmentVariable;
        }
        #endregion
    }
}