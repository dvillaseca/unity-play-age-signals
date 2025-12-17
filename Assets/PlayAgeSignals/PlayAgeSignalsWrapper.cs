using System;
using UnityEngine;

namespace TinyBytes.PlayAgeSignals
{
	public static class PlayAgeSignalsWrapper
	{
#if UNITY_ANDROID
		// Updated to match the Java package name
		private const string WRAPPER_CLASS = "com.tinybytes.playagesignals.PlayAgeSignalsUnityWrapper";
#endif
		/// <summary>
		/// Requests the age signals.
		/// </summary>
		/// <param name="onSuccess">
		/// Callback invoked upon successful retrieval of age signals.
		/// <br/><b>WARNING:</b> This callback might be invoked on a thread other than the Unity main thread.
		/// Do not perform Unity API calls (e.g., UI updates, accessing GameObjects) directly inside this callback.
		/// Dispatch to the main thread if necessary.
		/// </param>
		/// <param name="onError">
		/// Callback invoked upon an error.
		/// <br/><b>WARNING:</b> This callback might be invoked on a thread other than the Unity main thread.
		/// Do not perform Unity API calls (e.g., UI updates, accessing GameObjects) directly inside this callback.
		/// Dispatch to the main thread if necessary.
		/// </param>
		/// <param name="testResponse">Optional test response to mock the underlying service response.</param>
		public static void Request(Action<AgeSignalsResultData> onSuccess, Action<AgeSignalsError> onError, AgeSignalsResultData? testResponse = null)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

				AndroidJavaObject javaWrapper = new AndroidJavaObject(WRAPPER_CLASS, context, testResponse.HasValue);

				if (testResponse.HasValue)
				{
					var data = testResponse.Value;
					long dateMillis = 0;
					if (data.MostRecentApprovalDate != DateTime.MinValue)
					{
						dateMillis = new DateTimeOffset(data.MostRecentApprovalDate).ToUnixTimeMilliseconds();
					}

					javaWrapper.Call("setTestResponse", (int)data.userStatus, data.ageLower, data.ageUpper, data.installId, dateMillis);
				}

				javaWrapper.Call("requestAgeSignals", new AgeSignalsCallbackProxy(
					jsonResult =>
					{
						try
						{
							AgeSignalsResultData resultData = JsonUtility.FromJson<AgeSignalsResultData>(jsonResult);
							onSuccess?.Invoke(resultData);
						}
						catch (Exception e)
						{
							onError?.Invoke(new AgeSignalsError
							{
								errorCode = AgeSignalsErrorCode.INTERNAL_ERROR,
								message = $"JSON parsing failed: {e.Message}"
							});
						}
					},
					(errorCode, message) =>
					{
						onError?.Invoke(new AgeSignalsError
						{
							errorCode = (AgeSignalsErrorCode)errorCode,
							message = message
						});
					},
					javaWrapper
				));
			}
			catch (Exception e)
			{
				onError?.Invoke(new AgeSignalsError
				{
					errorCode = AgeSignalsErrorCode.INTERNAL_ERROR,
					message = $"Request failed: {e.Message}"
				});
			}
#else
			if (testResponse.HasValue)
			{
				onSuccess?.Invoke(testResponse.Value);
			}
			else
			{
				onError?.Invoke(new AgeSignalsError
				{
					errorCode = AgeSignalsErrorCode.INTERNAL_ERROR,
					message = "Play Age Signals API is only available on Android devices"
				});
			}
#endif
		}

#if UNITY_ANDROID
		private class AgeSignalsCallbackProxy : AndroidJavaProxy
		{
			private Action<string> onSuccessAction;
			private Action<int, string> onErrorAction;
			private AndroidJavaObject keeper; //this is for keeping the class alive and avoid the GC to collect it

			public AgeSignalsCallbackProxy(Action<string> onSuccess, Action<int, string> onError, AndroidJavaObject keeper)
				: base("com.tinybytes.playagesignals.PlayAgeSignalsUnityWrapper$AgeSignalsCallback")
			{
				this.onSuccessAction = onSuccess;
				this.onErrorAction = onError;
				this.keeper = keeper;
			}

			public void onSuccess(string jsonResult)
			{
				// Callback from Java thread, need to dispatch to main thread if interacting with Unity API
				// However, for pure data processing, it's fine. The consumer of PlayAgeSignalsWrapper
				// should handle thread safety if they update UI.
				// But typically Unity callbacks from AndroidJavaProxy are invoked on the main thread attached to JNI.
				onSuccessAction?.Invoke(jsonResult);
			}

			public void onError(int errorCode, string message)
			{
				onErrorAction?.Invoke(errorCode, message);
			}
		}
#endif
	}

		[Serializable]
		public struct AgeSignalsResultData
		{
			public AgeSignalsVerificationStatus userStatus;
			public int ageLower;
			public int ageUpper;
			public string installId;
			public long mostRecentApprovalDate;

			public DateTime MostRecentApprovalDate
			{
				get
				{
					if (mostRecentApprovalDate > 0)
					{
						return DateTimeOffset.FromUnixTimeMilliseconds(mostRecentApprovalDate).DateTime.ToLocalTime();
					}
					return DateTime.MinValue;
				}
				set
				{
					mostRecentApprovalDate = new DateTimeOffset(value).ToUnixTimeMilliseconds();
				}
			}

			public bool IsSupervised => userStatus == AgeSignalsVerificationStatus.SUPERVISED ||
										userStatus == AgeSignalsVerificationStatus.SUPERVISED_APPROVAL_PENDING;

			public bool IsVerified => userStatus == AgeSignalsVerificationStatus.VERIFIED;

			public bool IsUnverified => userStatus == AgeSignalsVerificationStatus.UNKNOWN;

			public bool IsNotApplicable => userStatus == AgeSignalsVerificationStatus.NOT_APPLICABLE;
		}

		[Serializable]
		public struct AgeSignalsError
		{
			public AgeSignalsErrorCode errorCode;
			public string message;

			public bool IsRetryable => errorCode switch
			{
				AgeSignalsErrorCode.API_NOT_AVAILABLE => true,
				AgeSignalsErrorCode.PLAY_STORE_NOT_FOUND => true,
				AgeSignalsErrorCode.NETWORK_ERROR => true,
				AgeSignalsErrorCode.PLAY_SERVICES_NOT_FOUND => true,
				AgeSignalsErrorCode.CANNOT_BIND_TO_SERVICE => true,
				AgeSignalsErrorCode.PLAY_STORE_VERSION_OUTDATED => true,
				AgeSignalsErrorCode.PLAY_SERVICES_VERSION_OUTDATED => true,
				AgeSignalsErrorCode.CLIENT_TRANSIENT_ERROR => true,
				AgeSignalsErrorCode.APP_NOT_OWNED => false,
				AgeSignalsErrorCode.INTERNAL_ERROR => true,
				_ => false
			};
		}

		public enum AgeSignalsVerificationStatus
		{
			VERIFIED = 0,
			SUPERVISED = 1,
			SUPERVISED_APPROVAL_PENDING = 2,
			SUPERVISED_APPROVAL_DENIED = 3,
			UNKNOWN = 4,
			NOT_APPLICABLE = 5
		}

		public enum AgeSignalsErrorCode
		{
			API_NOT_AVAILABLE = -1,
			PLAY_STORE_NOT_FOUND = -2,
			NETWORK_ERROR = -3,
			PLAY_SERVICES_NOT_FOUND = -4,
			CANNOT_BIND_TO_SERVICE = -5,
			PLAY_STORE_VERSION_OUTDATED = -6,
			PLAY_SERVICES_VERSION_OUTDATED = -7,
			CLIENT_TRANSIENT_ERROR = -8,
			APP_NOT_OWNED = -9,
			INTERNAL_ERROR = -100
		}
	}
