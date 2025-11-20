using System;
using UnityEngine;

namespace TinyBytes.PlayAgeSignals
{
    public class PlayAgeSignalsExample : MonoBehaviour
    {
        private PlayAgeSignalsWrapper ageSignalsWrapper;
        private int retryCount = 0;
        private const int MAX_RETRIES = 3;

        private void Start()
        {
            ageSignalsWrapper = new PlayAgeSignalsWrapper();
            RequestAgeSignals();
        }

        private void RequestAgeSignals()
        {
            ageSignalsWrapper.Initialize(OnAgeSignalsSuccess, OnAgeSignalsError, new AgeSignalsResultData()
            {
                ageLower = 13,
                ageUpper = 18,
                userStatus = AgeSignalsVerificationStatus.VERIFIED,
                installId = "googleplay",
                MostRecentApprovalDate = DateTime.Now
            });
        }

        private void OnAgeSignalsSuccess(AgeSignalsResultData result)
        {
            Debug.Log($"Age Signals Success!");
            Debug.Log($"User Status: {result.userStatus}");
            Debug.Log($"Age Range: {result.ageLower}-{result.ageUpper}");
            Debug.Log($"Install ID: {result.installId}");
            Debug.Log($"Most Recent Approval: {result.MostRecentApprovalDate.ToString() ?? "None"}");
            Debug.Log($"Is Supervised: {result.IsSupervised}");
            Debug.Log($"Is Verified: {result.IsVerified}");

            if (result.ageLower < 18)
            {
                Debug.Log("User is a minor - apply appropriate restrictions");
            }

            retryCount = 0;
        }

        private void OnAgeSignalsError(AgeSignalsError error)
        {
            Debug.LogError($"Age Signals Error: {error.errorCode} - {error.message}");

            if (error.IsRetryable && retryCount < MAX_RETRIES)
            {
                retryCount++;
                float delay = Mathf.Pow(2, retryCount);
                Debug.Log($"Retrying in {delay} seconds... (Attempt {retryCount}/{MAX_RETRIES})");
                Invoke(nameof(RequestAgeSignals), delay);
                return;
            }

            if (retryCount >= MAX_RETRIES)
            {
                Debug.LogError("Max retries reached. Please try again later.");
            }

            HandleErrorCode(error.errorCode);
        }

        private void HandleErrorCode(AgeSignalsErrorCode errorCode)
        {
            switch (errorCode)
            {
                case AgeSignalsErrorCode.API_NOT_AVAILABLE:
                case AgeSignalsErrorCode.PLAY_STORE_VERSION_OUTDATED:
                    Debug.Log("Please update your Play Store app");
                    break;

                case AgeSignalsErrorCode.PLAY_STORE_NOT_FOUND:
                    Debug.Log("Please install or enable the Play Store");
                    break;

                case AgeSignalsErrorCode.NETWORK_ERROR:
                    Debug.Log("Please check your internet connection");
                    break;

                case AgeSignalsErrorCode.PLAY_SERVICES_NOT_FOUND:
                case AgeSignalsErrorCode.PLAY_SERVICES_VERSION_OUTDATED:
                    Debug.Log("Please install, update, or enable Play Services");
                    break;

                case AgeSignalsErrorCode.APP_NOT_OWNED:
                    Debug.Log("This app must be installed from Google Play");
                    break;

                case AgeSignalsErrorCode.INTERNAL_ERROR:
                    Debug.Log("An internal error occurred. Please try again later");
                    break;
            }
        }
    }
}

