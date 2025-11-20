# Play Age Signals for Unity

A Unity wrapper for the [Google Play Age Signals API](https://developer.android.com/games/agereq/age-signals). This package allows you to request age verification signals from the Google Play Store to help comply with age-appropriate design codes and regulations.

## Installation

### via Git URL

1. Open the Package Manager in Unity (**Window > Package Manager**).
2. Click the **+** button and select **Add package from git URL...**.
3. Enter the git URL of this repository with the path query parameter:
   `https://github.com/dvillaseca/unity-play-age-signals.git?path=/Assets/PlayAgeSignals`
   
   *(Note: The `path` parameter is required because the package files are in a subdirectory)*

### Prerequisites

- **External Dependency Manager for Unity (EDM4U)**: This package uses `PlayAgeSignalsDependencies.xml` to resolve the Android Maven dependency (`com.google.android.play:age-signals`). Ensure you have EDM4U installed in your project to automatically download the required Android libraries.
- **Android**: This plugin is designed for Android devices.

## Usage

The main entry point is the `PlayAgeSignalsWrapper` static class.

### 1. Request Signals

Call `PlayAgeSignalsWrapper.Request`. You must provide success and error callbacks.

**Important:** The callbacks might be invoked on a background thread. If you need to interact with the Unity API (e.g., update UI), ensure you dispatch to the main thread.

```csharp
using TinyBytes.PlayAgeSignals;

public class MyAgeVerifier : MonoBehaviour
{
    void Start()
    {
        RequestAgeInfo();
    }

    public void RequestAgeInfo()
    {
        PlayAgeSignalsWrapper.Request(
            OnSuccess, 
            OnError
            // Optional: Pass test data here for Editor/testing
            // new AgeSignalsResultData { userStatus = AgeSignalsVerificationStatus.VERIFIED, ... }
        );
    }
}
```

### 2. Handle Success

The success callback receives `AgeSignalsResultData`.

```csharp
private void OnSuccess(AgeSignalsResultData result)
{
    // WARNING: This might be on a background thread.
    // Use a dispatcher if you need to touch Unity objects.

    Debug.Log($"User Status: {result.userStatus}");
    
    if (result.IsVerified)
    {
        Debug.Log($"Verified Age Range: {result.ageLower}-{result.ageUpper}");
    }
    
    // Check for supervision (Parental Controls)
    if (result.IsSupervised)
    {
        Debug.Log("User is supervised.");
    }
}
```

### 3. Handle Errors

The error callback receives `AgeSignalsError`.

```csharp
private void OnError(AgeSignalsError error)
{
    // WARNING: This might be on a background thread.

    Debug.LogError($"Error {error.errorCode}: {error.message}");

    if (error.IsRetryable)
    {
        // Implement retry logic (e.g., exponential backoff)
        Invoke(nameof(RequestAgeInfo), 2.0f);
    }
    else
    {
        // Handle non-recoverable errors (e.g. show specific UI)
        switch (error.errorCode)
        {
            case AgeSignalsErrorCode.PLAY_STORE_NOT_FOUND:
                // Prompt user to install/enable Play Store
                break;
            // ... handle other codes
        }
    }
}
```

## Testing

You can test the integration in the Unity Editor or on a device by passing `AgeSignalsResultData` as the third argument to `Request`.

```csharp
var testData = new AgeSignalsResultData
{
    userStatus = AgeSignalsVerificationStatus.VERIFIED,
    ageLower = 13,
    ageUpper = 18
};

PlayAgeSignalsWrapper.Request(OnSuccess, OnError, testData);
```

## License

[GPL-3.0](LICENSE)

