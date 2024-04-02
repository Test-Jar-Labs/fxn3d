/*
*   Function
*   Copyright © 2024 NatML Inc. All rights reserved.
*/

#nullable enable

namespace Function.Types {

    using Internal;

    /// <summary>
    /// Tensor.
    /// </summary>
    [Preserve]
    public unsafe readonly struct Tensor<T> where T : unmanaged {

        #region --Client API--
        /// <summary>
        /// Tensor data.
        /// </summary>
        public readonly T[] data;

        /// <summary>
        /// Tensor shape.
        /// </summary>
        public readonly int[] shape;

        /// <summary>
        /// Create a tensor.
        /// </summary>
        /// <param name="data">Tensor data.</param>
        /// <param name="shape">Tensor shape.</param>
        public Tensor (T[] data, int[] shape) {
            this.data = data;
            this.nativeData = null;
            this.shape = shape;
        }
        #endregion


        #region --Operations--
        private readonly T* nativeData;

        public Tensor (T* data, int[] shape) { // Zero copy into `FXNValue`
            this.data = null!;
            this.nativeData = data;
            this.shape = shape;
        }

        public ref T GetPinnableReference () => ref (nativeData == null ? ref data[0] : ref *nativeData);
        #endregion
    }
}