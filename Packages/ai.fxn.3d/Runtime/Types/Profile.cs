/* 
*   Function
*   Copyright © 2023 NatML Inc. All rights reserved.
*/

#nullable enable
#pragma warning disable 8618

namespace Function.Types {

    /// <summary>
    /// Function user profile.
    /// </summary>
    public class Profile {

        /// <summary>
        /// Username.
        /// </summary>
        public string username;

        /// <summary>
        /// Date created.
        /// </summary>
        public string created;

        /// <summary>
        /// User display name.
        /// </summary>
        public string? name;

        /// <summary>
        /// User avatar.
        /// </summary>
        public string? avatar;

        /// <summary>
        /// User bio.
        /// </summary>
        public string? bio;

        /// <summary>
        /// User website.
        /// </summary>
        public string? website;

        /// <summary>
        /// User GitHub handle.
        /// </summary>
        public string? github;
    }
}