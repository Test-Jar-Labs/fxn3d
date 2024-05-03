//
//  FXNPredictor.h
//  Function
//
//  Created by Yusuf Olokoba on 10/14/2023.
//  Copyright © 2024 NatML Inc. All rights reserved.
//

#pragma once

#include <Function/FXNConfiguration.h>
#include <Function/FXNPrediction.h>
#include <Function/FXNPredictionStream.h>

#pragma region --Types--
/*!
 @struct FXNPredictor
 
 @abstract Predictor.

 @discussion Predictor.
*/
struct FXNPredictor;
typedef struct FXNPredictor FXNPredictor;
#pragma endregion


#pragma region --Lifecycle--
/*!
 @function FXNPredictorCreate

 @abstract Create a predictor.

 @discussion Create a predictor.

 @param configuration
 Predictor configuration.

 @param predictor
 Created predictor.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictorCreate (
    FXNConfiguration* configuration,
    FXNPredictor** predictor
);

/*!
 @function FXNPredictorRelease

 @abstract Release a predictor.

 @discussion Release a predictor.

 @param predictor
 Predictor.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictorRelease (FXNPredictor* predictor);
#pragma endregion


#pragma region --Operations--
/*!
 @function FXNPredictorCreatePrediction

 @abstract Create a prediction.

 @discussion Create a prediction.

 @param predictor
 Predictor.

 @param inputs
 Prediction inputs.

 @param prediction
 Prediction.
 You MUST release the prediction with `FXNPredictionRelease` when no longer needed.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictorCreatePrediction (
    FXNPredictor* predictor,
    FXNValueMap* inputs,
    FXNPrediction** prediction
);

/*!
 @function FXNPredictorStreamPrediction

 @abstract Create a streaming prediction.

 @discussion Create a streaming prediction.
 NOTE: This API is currently experimental.

 @param predictor
 Predictor.

 @param inputs
 Prediction inputs.

 @param prediction
 Prediction stream.
 You MUST release the prediction stream with `FXNPredictionStreamRelease` when no longer needed.
*/
FXN_BRIDGE FXN_EXPORT FXNStatus FXN_API FXNPredictorStreamPrediction (
    FXNPredictor* predictor,
    FXNValueMap* inputs,
    FXNPredictionStream** stream
);
#pragma endregion