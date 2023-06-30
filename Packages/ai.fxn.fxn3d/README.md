# Function for Unity Engine

![function logo](https://raw.githubusercontent.com/fxnai/.github/main/logo_wide.png)

Run AI prediction functions (a.k.a "predictors") in your Unity apps and games. With Function, you can build AI-powered apps by creating and composing GPU-accelerated predictors that run in the cloud. In a few steps:

## Installing Function
Add the following items to your Unity project's `Packages/manifest.json`:
```json
{
  "scopedRegistries": [
    {
      "name": "Function",
      "url": "https://registry.npmjs.com",
      "scopes": ["ai.fxn"]
    }
  ],
  "dependencies": {
    "ai.fxn.fxn3d": "0.0.1"
  }
}
```

## Retrieving your Access Key
Head over to [fxn.ai](https://fxn.ai) to create an account by logging in. Once you do, generate an access key:

![generate access key](https://raw.githubusercontent.com/fxnai/.github/main/access_key.gif)

Then add it to your Unity project in `Project Settings > Function`:

![add access key to Unity](https://raw.githubusercontent.com/fxnai/fxn3d/main/settings.gif)

## Making a Prediction
First, create a Function client and specify your access key:
```csharp
using Function;

// Create a Function client
var fxn = FunctionUnity.Create();
```

Then make a prediction:
```csharp
// Make a prediction
var prediction = await fxn.Predictions.Create(
    tag: "@natml/greeting-sample",
    inputs: new () {
        ["name"] = "Rhea"
    }
) as CloudPrediction;
// Log the result
Debug.Log(prediction.results[0]);
```

___

## Requirements
- Unity 2021.2+

## Supported Platforms
- Android API Level 24+
- iOS 14+
- macOS 10.15+ (Apple Silicon and Intel)
- Windows 10+ (64-bit only)
- WebGL:
  - Chrome 91+
  - Firefox 90+
  - Safari 16.4+

## Useful Links
- [Discover predictors to use in your apps](https://fxn.ai/explore).
- [Join our Discord community](https://fxn.ai/community).
- [Check out our docs](https://docs.fxn.ai).
- Learn more about us [on our blog](https://blog.fxn.ai).
- Reach out to us at [hi@fxn.ai](mailto:hi@fxn.ai).

Thank you very much!