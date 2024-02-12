/* 
*   Function
*   Copyright © 2024 NatML Inc. All rights reserved.
*/

namespace Function.Types {

    using System;
    using Internal;

    /// <summary>
    /// Function user.
    /// </summary>
    [Preserve, Serializable]
    public class User : Profile {

        /// <summary>
        /// User email address.
        /// </summary>
        public string email;
    }
}