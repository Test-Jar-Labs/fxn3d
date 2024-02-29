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
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using API;
    using Internal;
    using Types;
    using Dtype = Types.Dtype;
    using Status = Internal.Function.Status;
    using ValueFlags = Internal.Function.ValueFlags;

    /// <summary>
    /// Make predictions.
    /// </summary>
    public sealed class PredictionService {

        #region --Client API--
        /// <summary>
        /// Create a prediction.
        /// </summary>
        /// <param name="tag">Predictor tag.</param>
        /// <param name="inputs">Input values. This only applies to `CLOUD` predictions.</param>
        /// <param name="rawOutputs">Skip parsing output values into plain values. This only applies to `CLOUD` predictions.</param>
        /// <param name="dataUrlLimit">Return a data URL if a given output value is smaller than this size. This only applies to `CLOUD` predictions.</param>
        /// <param name="acceleration">Prediction acceleration. This only applies to `EDGE` predictions.</param>
        /// <param name="device">Prediction device. Do not set this unless you know what you are doing. This only applies to `EDGE` predictions.</param>
        public async Task<Prediction> Create (
            string tag,
            Dictionary<string, object>? inputs = null,
            bool rawOutputs = false,
            int? dataUrlLimit = null,
            Acceleration acceleration = default,
            IntPtr device = default
        ) {
            // Check cache
            if (cache.TryGetValue(tag, out var p))
                return Predict(tag, p, inputs);
            // Collect inputs
            var key = Guid.NewGuid().ToString();
            var values = inputs != null ?
                (await Task.WhenAll(inputs.Select(async pair => (name: pair.Key, value: await ToValue(pair.Value, pair.Key, key: key))))).ToDictionary(pair => pair.name, pair => pair.value as object) :
                null;
            // Query
            var prediction = await client.Request<Prediction>(
                @"POST",
                $"/predict/{tag}?dataUrlLimit={dataUrlLimit}&rawOutputs=true",
                values,
                new () {
                    [@"fxn-client"] = clientId,
                    [@"fxn-configuration-token"] = ConfigurationId
                }
            );
            // Parse results
            prediction.results = await ParseResults(prediction.results, rawOutputs);
            // Create edge prediction
            if (prediction.type == PredictorType.Edge) {
                var predictor = await Load(prediction, acceleration, device);
                cache.Add(prediction.tag, predictor);
                prediction = Predict(tag, predictor, inputs);
            }
            // Return
            return prediction;
        }

        /// <summary>
        /// Create a streaming prediction.
        /// </summary>
        /// <param name="tag">Predictor tag.</param>
        /// <param name="inputs">Input values. Only applies to `CLOUD` predictions.</param>
        /// <param name="rawOutputs">Skip parsing output values into plain values.</param>
        /// <param name="dataUrlLimit">Return a data URL if a given output value is smaller than this size. Only applies to `CLOUD` predictions.</param>
        public async IAsyncEnumerable<Prediction> Stream (
            string tag,
            Dictionary<string, object>? inputs = null,
            bool rawOutputs = false,
            int? dataUrlLimit = null
        ) {
            // Collect inputs
            var key = Guid.NewGuid().ToString();
            var values = inputs != null ?
                (await Task.WhenAll(inputs.Select(async pair => (name: pair.Key, value: await ToValue(pair.Value, pair.Key, key: key))))).ToDictionary(pair => pair.name, pair => pair.value as object) :
                null;
            // Stream
            var path = $"/predict/{tag}?stream=true&rawOutputs=true&dataUrlLimit={dataUrlLimit}";
            await foreach (var prediction in client.Stream<Prediction?>(path, values)) {
                // Collect results
                prediction.results = await ParseResults(prediction.results, rawOutputs);
                // Yield
                yield return prediction;
            }
        }

        /// <summary>
        /// Delete an edge predictor that is loaded in memory.
        /// </summary>
        /// <param name="tag">Predictor tag.</param>
        /// <returns>Whether the edge predictor was successfully deleted from memory.</returns>
        public Task<bool> Delete (string tag) {
            // Check
            if (!cache.TryGetValue(tag, out var predictor))
                return Task.FromResult(false);
            // Release
            predictor.ReleasePredictor().Throw();
            // Return
            return Task.FromResult(true);
        }

        /// <summary>
        /// Convert a Function value to a plain object.
        /// </summary>
        /// <param name="value">Function value.</param>
        /// <returns>Plain object or `Value` if the value cannot be converted to a plain object.</returns>
        public async Task<object?> ToObject (Value value) {
            // Null
            if (value.type == Dtype.Null)
                return null;
            // Download
            var stream = await storage.Download(value.data);
            // Switch
            switch (value.type) {
                case Dtype.Float32: return ToObject<float>(stream, value.shape);
                case Dtype.Float64: return ToObject<double>(stream, value.shape);
                case Dtype.Int8:    return ToObject<sbyte>(stream, value.shape);
                case Dtype.Int16:   return ToObject<short>(stream, value.shape);
                case Dtype.Int32:   return ToObject<int>(stream, value.shape);
                case Dtype.Int64:   return ToObject<long>(stream, value.shape);
                case Dtype.Uint8:   return ToObject<byte>(stream, value.shape);
                case Dtype.Uint16:  return ToObject<ushort>(stream, value.shape);
                case Dtype.Uint32:  return ToObject<uint>(stream, value.shape);
                case Dtype.Uint64:  return ToObject<ulong>(stream, value.shape);
                case Dtype.Bool:    return ToObject<bool>(stream, value.shape);
                case Dtype.String:  return new StreamReader(stream).ReadToEnd();
                case Dtype.Binary:  return stream;
                case Dtype.List:    return JsonConvert.DeserializeObject<List<object>>(new StreamReader(stream).ReadToEnd());
                case Dtype.Dict:    return JsonConvert.DeserializeObject<Dictionary<string, object>>(new StreamReader(stream).ReadToEnd());
                default:            return value;
            }
        }

        /// <summary>
        /// Create a Function prediction value from an input object.
        /// </summary>
        /// <param name="value">Input object.</param>
        /// <param name="name">Value name.</param>
        /// <param name="type">Value type. This only applies to `Stream` input objects.</param>
        /// <param name="minUploadSize">Values larger than this size in bytes will be uploaded.</param>
        /// <returns>Function value.</returns>
        public async Task<Value> ToValue (
            object value,
            string name,
            Dtype? type = null,
            int minUploadSize = 4096,
            string? key = null
        ) => value switch {
            Value           x => x,
            float           x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Float32, shape = new int[0] },
            double          x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Float64, shape = new int[0] },
            sbyte           x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int8, shape = new int[0] },
            short           x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int16, shape = new int[0] },
            int             x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int32, shape = new int[0] },
            long            x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int64, shape = new int[0] },
            byte            x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint8, shape = new int[0] },
            ushort          x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint16, shape = new int[0] },
            uint            x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint32, shape = new int[0] },
            ulong           x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint64, shape = new int[0] },
            bool            x => new Value { data = await storage.Upload(name, new [] { x }.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Bool, shape = new int[0] },
            float[]         x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Float32, shape = new [] { x.Length } },
            double[]        x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Float64, shape = new [] { x.Length } },
            sbyte[]         x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int8, shape = new [] { x.Length } },
            short[]         x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int16, shape = new [] { x.Length } },
            int[]           x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int32, shape = new [] { x.Length } },
            long[]          x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int64, shape = new [] { x.Length } },
            byte[]          x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint8, shape = new [] { x.Length } },
            ushort[]        x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint16, shape = new [] { x.Length } },
            uint[]          x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint32, shape = new [] { x.Length } },
            ulong[]         x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint64, shape = new [] { x.Length } },
            bool[]          x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Bool, shape = new [] { x.Length } },
            Tensor<float>   x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Float32, shape = x.shape },
            Tensor<double>  x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Float64, shape = x.shape },
            Tensor<sbyte>   x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int8, shape = x.shape },
            Tensor<short>   x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int16, shape = x.shape },
            Tensor<int>     x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int32, shape = x.shape },
            Tensor<long>    x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Int64, shape = x.shape },
            Tensor<byte>    x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint8, shape = x.shape },
            Tensor<ushort>  x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint16, shape = x.shape },
            Tensor<uint>    x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint32, shape = x.shape },
            Tensor<ulong>   x => new Value { data = await storage.Upload(name, x.data.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Uint64, shape = x.shape },
            string          x => new Value { data = await storage.Upload(name, x.ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.String },
            Stream          x => new Value { data = await storage.Upload(name, x, UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = type ?? Dtype.Binary },
            IList           x => new Value { data = await storage.Upload(name, JsonConvert.SerializeObject(x).ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.List },
            IDictionary     x => new Value { data = await storage.Upload(name, JsonConvert.SerializeObject(x).ToStream(), UploadType.Value, dataUrlLimit: minUploadSize, key: key), type = Dtype.Dict },
            null              => new Value { type = Dtype.Null },
            _                 => throw new InvalidOperationException($"Cannot create a Function value from value '{value}' of type {value.GetType()}"),
        };
        #endregion


        #region --Operations--
        private readonly IFunctionClient client;
        private readonly StorageService storage;
        private readonly string? clientId;
        private readonly string cachePath;
        private readonly Dictionary<string, IntPtr> cache;

        internal PredictionService (
            IFunctionClient client,
            StorageService storage,
            string? clientId,
            string? cachePath
        ) {
            this.client = client;
            this.storage = storage;
            this.clientId = clientId;        
            this.cachePath = cachePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".fxn",
                "cache"
            );
            this.cache = new Dictionary<string, IntPtr>();
        }

        private async Task<IntPtr> Load (Prediction prediction, Acceleration acceleration, IntPtr device) {
            // Create configuration
            Function.CreateConfiguration(out var configuration).Throw();
            configuration.SetConfigurationTag(prediction.tag).Throw();
            configuration.SetConfigurationToken(prediction.configuration).Throw();
            configuration.SetConfigurationAcceleration(acceleration).Throw();
            configuration.SetConfigurationDevice(device).Throw();
            await Task.WhenAll(prediction.resources.Select(async resource => {
                if (resource.type == @"fxn")
                    return;
                var path = await Retrieve(resource);
                lock (prediction)
                    configuration.AddConfigurationResource(resource.type, path).Throw();
            }));
            // Create predictor
            Function.CreatePredictor(configuration, out var predictor).Throw();
            configuration.ReleaseConfiguration().Throw();            
            // Return
            return predictor;
        }

        private async Task<string> Retrieve (PredictionResource resource) {
            // Check cache
            Directory.CreateDirectory(cachePath);
            var path = Path.Combine(cachePath, GetResourceName(resource.url));
            if (File.Exists(path))
                return path;
            // Download
            using var dataStream = await client.Download(resource.url);
            using var fileStream = File.Create(path);
            if (clientId == @"browser")
                dataStream.CopyTo(fileStream); // Workaround for lack of pthreads on browser
            else
                await dataStream.CopyToAsync(fileStream);
            // Return
            return path;
        }

        private Prediction Predict (string tag, IntPtr predictor, Dictionary<string, object> inputs) {
            IntPtr inputMap = default;
            IntPtr prediction = default;
            try {
                // Marshal inputs
                Function.CreateValueMap(out inputMap).Throw();
                foreach (var pair in inputs)
                    inputMap.SetValueMapValue(pair.Key, ToValue(pair.Value)).Throw();
                // Predict
                predictor.Predict(inputMap, out prediction).Throw();
                // Get prediction id
                var idBuffer = new StringBuilder(2048);
                prediction.GetPredictionID(idBuffer, idBuffer.Capacity).Throw();
                var id = idBuffer.ToString();
                // Get prediction error
                var errorBuffer = new StringBuilder(2048);
                var error = prediction.GetPredictionError(errorBuffer, errorBuffer.Length) == Status.Ok ? errorBuffer.ToString() : null;
                // Get latency and logs
                prediction.GetPredictionLatency(out var latency);
                prediction.GetPredictionLogLength(out var logsLength);
                var logBuffer = new StringBuilder(logsLength + 1);
                var logs = prediction.GetPredictionLogs(logBuffer, logBuffer.Capacity) == Status.Ok ? logBuffer.ToString() : null;  
                // Marshal outputs
                prediction.GetPredictionResults(out var outputMap).Throw();
                outputMap.GetValueMapSize(out var count).Throw();
                var results = new List<object?>();
                var name = new StringBuilder(2048);
                for (var idx = 0; idx < count; ++idx) {
                    name.Clear();
                    outputMap.GetValueMapKey(idx, name, name.Capacity).Throw();
                    outputMap.GetValueMapValue(name.ToString(), out var value).Throw();
                    results.Add(ToObject(value));
                }                              
                // Create prediction
                return new Prediction {
                    id = id.ToString(),
                    tag = tag,
                    type = PredictorType.Edge,
                    created = DateTime.UtcNow,
                    results = results.ToArray(),
                    latency = latency, 
                    error = error,
                    logs = logs,
                };
            } finally {
                inputMap.ReleaseValueMap().Throw();
                prediction.ReleasePrediction().Throw();
            }
        }

        private async Task<object?[]?> ParseResults (object[]? values, bool raw) {
            // Check
            if (values == null)
                return null;
            // Convert
            var results = await Task.WhenAll(values.Select(async r => {
                var value = (r as JObject).ToObject<Value>();
                return raw ? value : await ToObject(value);
            }));
            // Return
            return results;
        }
        #endregion


        #region --Utilities--

        private static string ConfigurationId {
            get {
                var sb = new StringBuilder(2048);
                Function.GetConfigurationUniqueID(sb, sb.Capacity).Throw();
                return sb.ToString();
            }
        }

        private static unsafe IntPtr ToValue (object? value) {
            switch (value) {
                case IntPtr x:          return x;
                case float x:           return ToValue(&x);
                case double x:          return ToValue(&x);
                case sbyte x:           return ToValue(&x);
                case short x:           return ToValue(&x);   
                case int x:             return ToValue(&x);
                case long x:            return ToValue(&x);
                case byte x:            return ToValue(&x);
                case ushort x:          return ToValue(&x);
                case uint x:            return ToValue(&x);
                case ulong x:           return ToValue(&x);
                case bool x:            return ToValue(&x);
                case float[] x:         return ToValue(x);
                case double[] x:        return ToValue(x);
                case sbyte[] x:         return ToValue(x);
                case short[] x:         return ToValue(x);   
                case int[] x:           return ToValue(x);
                case long[] x:          return ToValue(x);
                case byte[] x:          return ToValue(x);
                case ushort[] x:        return ToValue(x);
                case uint[] x:          return ToValue(x);
                case ulong[] x:         return ToValue(x);
                case bool[] x:          return ToValue(x);
                case Tensor<float> x:   return ToValue(x.data, x.shape);
                case Tensor<double> x:  return ToValue(x.data, x.shape);
                case Tensor<sbyte> x:   return ToValue(x.data, x.shape);
                case Tensor<short> x:   return ToValue(x.data, x.shape);
                case Tensor<int> x:     return ToValue(x.data, x.shape);
                case Tensor<long> x:    return ToValue(x.data, x.shape);
                case Tensor<byte> x:    return ToValue(x.data, x.shape);
                case Tensor<ushort> x:  return ToValue(x.data, x.shape);
                case Tensor<uint> x:    return ToValue(x.data, x.shape);
                case Tensor<ulong> x:   return ToValue(x.data, x.shape);
                case Tensor<bool> x:    return ToValue(x.data, x.shape);
                case string x:
                    Function.CreateStringValue(x, out var str).Throw();
                    return str;
                case IList x:
                    Function.CreateListValue(JsonConvert.SerializeObject(x), out var list).Throw();
                    return list;
                case IDictionary x:
                    Function.CreateListValue(JsonConvert.SerializeObject(x), out var dict).Throw();
                    return dict;
                case Stream stream:
                    Function.CreateBinaryValue(stream.ToArray(), stream.Length, ValueFlags.CopyData, out var binary).Throw();
                    return binary;
                case null:
                    Function.CreateNullValue(out var nullptr).Throw();
                    return nullptr;
                default: throw new InvalidOperationException($"Cannot create a Function value from value '{value}' of type {value.GetType()}");
            }
        }

        private static unsafe object? ToObject (IntPtr value) {
            // Null
            value.GetValueType(out var dtype).Throw();
            if (dtype == Dtype.Null)
                return null;
            // Get data and shape
            value.GetValueData(out var data).Throw();
            value.GetValueDimensions(out var dims).Throw();
            var shape = new int[dims];
            value.GetValueShape(shape, dims).Throw();
            // Deserialize
            switch (dtype) {
                case Dtype.Float32: return ToObject<float>(data, shape);
                case Dtype.Float64: return ToObject<double>(data, shape);
                case Dtype.Int8:    return ToObject<sbyte>(data, shape);
                case Dtype.Int16:   return ToObject<short>(data, shape);
                case Dtype.Int32:   return ToObject<int>(data, shape);
                case Dtype.Int64:   return ToObject<long>(data, shape);
                case Dtype.Uint8:   return ToObject<byte>(data, shape);
                case Dtype.Uint16:  return ToObject<ushort>(data, shape);
                case Dtype.Uint32:  return ToObject<uint>(data, shape);
                case Dtype.Uint64:  return ToObject<ulong>(data, shape);
                case Dtype.Bool:    return ToObject<bool>(data, shape);
                case Dtype.String:  return Marshal.PtrToStringUTF8(data);
                case Dtype.Binary:  return new MemoryStream(ToArray<byte>(data, shape));
                case Dtype.List:    return JsonConvert.DeserializeObject<object[]>(Marshal.PtrToStringUTF8(data));
                case Dtype.Dict:    return JsonConvert.DeserializeObject<Dictionary<string, object>>(Marshal.PtrToStringUTF8(data));
                default:            throw new InvalidOperationException($"Cannot convert Function value to object because value type is unsupported: {dtype}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe IntPtr ToValue<T> (T* data, int[]? shape = null) where T : unmanaged {
            Function.CreateArrayValue(
                data,
                shape,
                shape?.Length ?? 0,
                typeof(T).ToDtype(),
                ValueFlags.CopyData,
                out var result
            ).Throw();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe IntPtr ToValue<T> (T[] array, int[]? shape = null) where T : unmanaged {
            fixed (T* data = array)
                return ToValue(data, shape ?? new [] { array.Length });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ToObject<T> (MemoryStream stream, int[] shape) where T : unmanaged {
            var data = stream.ToArray<T>();
            return shape.Length <= 1 ? shape.Length < 1 ? data[0] : data : new Tensor<T>(data, shape);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe object ToObject<T> (IntPtr data, int[] shape) where T : unmanaged {
            if (shape.Length == 0)
                return *(T*)data;
            var array = ToArray<T>(data, shape);
            return shape.Length > 1 ? new Tensor<T>(array, shape) : array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe T[] ToArray<T> (IntPtr data, int[] shape) where T : unmanaged {
            var count = shape.Aggregate(1, (a, b) => a * b);
            var result = new T[count];
            fixed (void* dst = result)
                Buffer.MemoryCopy((void*)data, dst, count * sizeof(T), count * sizeof(T));
            return result;
        }

        internal static string GetResourceName (string url) {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimEnd('/');            
            var name = path.Substring(path.LastIndexOf('/') + 1);
            return name;
        }
        #endregion


        #region --Types--

        public sealed class CreatePredictionInput {
            public string tag;
            public string client;
            public ValueInput[]? inputs;
            public int? dataUrlLimit;
            public string? configuration;
            public string? device;
            public string? clientVersion;
        }

        public sealed class ValueInput : Value {
            public string name;
        }
        #endregion
    }
}