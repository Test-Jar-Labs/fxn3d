//
//  FXNPrediction.h
//  Function
//
//  Created by Yusuf Olokoba on 11/23/2023.
//  Copyright © 2024 NatML Inc. All rights reserved.
//

#pragma once

#include <stdint.h>
#include "FXNValueMap.h"

#pragma region --Types--
/*!
 @struct FXNPrediction
 
 @abstract Prediction.

 @discussion Prediction.
*/
struct FXNPrediction;
typedef struct FXNPrediction FXNPrediction;
#pragma endregion


#pragma region --Lifecycle--
/*!
 @function FXNPredictionRelease

 @abstract Release a prediction.

 @discussion Release a prediction.

 @param prediction
 Prediction.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionRelease (FXNPrediction* prediction);
#pragma endregion


#pragma region --Operations--
/*!
 @function FXNPredictionGetID

 @abstract Get the prediction ID.

 @discussion Get the prediction ID.

 @param prediction
 Prediction.

 @param destination
 Destination buffer.

 @param size
 Destination buffer size.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionGetID (
    FXNPrediction* prediction,
    char* destination,
    int32_t size
);

/*!
 @function FXNPredictionGetLatency

 @abstract Get the prediction latency.

 @discussion Get the prediction latency.

 @param prediction
 Prediction.

 @param latency
 Prediction latency in milliseconds.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionGetLatency (
    FXNPrediction* prediction,
    double* latency
);

/*!
 @function FXNPredictionGetResults

 @abstract Get the prediction results.

 @discussion Get the prediction results.

 @param prediction
 Prediction.

 @param map
 Prediction output value map. Do NOT release this value map as it is owned by the prediction.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionGetResults (
    FXNPrediction* prediction,
    FXNValueMap** map
);

/*!
 @function FXNPredictionGetError

 @abstract Get the prediction error.

 @discussion Get the prediction error.

 @param prediction
 Prediction.

 @param error
 Destination buffer.

 @param size
 Destination buffer size.

 @returns `FXN_OK` if an error has been copied.
 `FXN_ERROR_INVALID_OPERATION` if no error exists.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionGetError (
    FXNPrediction* prediction,
    char* error,
    int32_t size
);

/*!
 @function FXNPredictionGetLogs

 @abstract Get the prediction logs.

 @discussion Get the prediction logs.

 @param prediction
 Prediction.

 @param logs
 Destination buffer.

 @param size
 Destination buffer size.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionGetLogs (
    FXNPrediction* prediction,
    char* logs,
    int32_t size
);

/*!
 @function FXNPredictionGetLogLength

 @abstract Get the prediction log length.

 @discussion Get the prediction log length.

 @param prediction
 Prediction.

 @param length
 Logs length.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictionGetLogLength (
    FXNPrediction* prediction,
    int32_t* length
);
#pragma endregion