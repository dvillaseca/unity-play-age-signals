package com.tinybytes.playagesignals;

import android.content.Context;
import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.play.agesignals.AgeSignalsManager;
import com.google.android.play.agesignals.AgeSignalsManagerFactory;
import com.google.android.play.agesignals.AgeSignalsRequest;
import com.google.android.play.agesignals.AgeSignalsResult;
import com.google.android.play.agesignals.model.AgeSignalsVerificationStatus;
import com.google.android.play.agesignals.testing.FakeAgeSignalsManager;
import com.google.android.play.agesignals.AgeSignalsException;
import java.util.Date;
import org.json.JSONObject;

public class PlayAgeSignalsUnityWrapper {

    private AgeSignalsManager ageSignalsManager;

    // Callback interface for Unity
    public interface AgeSignalsCallback {
        void onSuccess(String jsonResult);

        void onError(int errorCode, String message);
    }

    public PlayAgeSignalsUnityWrapper(Context context, boolean debug) {
        if (!debug) {
            ageSignalsManager = AgeSignalsManagerFactory.create(context);
        } else {
            ageSignalsManager = new FakeAgeSignalsManager();
        }
    }

    public void setTestResponse(int userStatus, int ageLower, int ageUpper, String installId, long dateMillis) {
        if (ageSignalsManager instanceof FakeAgeSignalsManager) {
            AgeSignalsResult.Builder builder = AgeSignalsResult.builder();

            builder.setUserStatus(userStatus);

            if (ageLower > 0)
                builder.setAgeLower(ageLower);
            if (ageUpper > 0)
                builder.setAgeUpper(ageUpper);
            if (installId != null && !installId.isEmpty())
                builder.setInstallId(installId);
            if (dateMillis > 0)
                builder.setMostRecentApprovalDate(new Date(dateMillis));

            ((FakeAgeSignalsManager) ageSignalsManager).setNextAgeSignalsResult(builder.build());
        }
    }

    public void requestAgeSignals(final AgeSignalsCallback callback) {
        AgeSignalsRequest request = AgeSignalsRequest.builder().build();

        ageSignalsManager.checkAgeSignals(request)
                .addOnSuccessListener(new OnSuccessListener<AgeSignalsResult>() {
                    @Override
                    public void onSuccess(AgeSignalsResult result) {
                        try {
                            JSONObject json = new JSONObject();
                            // Get raw values safely
                            
                            // userStatus can be null (empty) if the user is not in an affected region
                            // We map this to 5 (NOT_APPLICABLE) for Unity
                            Object userStatus = result.userStatus();
                            if (userStatus == null) {
                                json.put("userStatus", 5);
                            } else {
                                json.put("userStatus", userStatus);
                            }

                            Object ageLower = result.ageLower();
                            if (ageLower == null) {
                                json.put("ageLower", -1);
                            } else {
                                json.put("ageLower", ageLower);
                            }

                            Object ageUpper = result.ageUpper();
                            if (ageUpper == null) {
                                json.put("ageUpper", -1);
                            } else {
                                json.put("ageUpper", ageUpper);
                            }

                            Date date = result.mostRecentApprovalDate();
                            if (date != null) {
                                json.put("mostRecentApprovalDate", date.getTime());
                            } else {
                                json.put("mostRecentApprovalDate", 0);
                            }

                            String installId = result.installId();
                            if (installId != null) {
                                json.put("installId", installId);
                            } else {
                                json.put("installId", JSONObject.NULL);
                            }

                            callback.onSuccess(json.toString());
                        } catch (Exception e) {
                            callback.onError(-100, "JSON serialization failed: " + e.getMessage());
                        }
                    }
                })
                .addOnFailureListener(new OnFailureListener() {
                    @Override
                    public void onFailure(Exception e) {
                        int errorCode = -100; // Internal error default
                        if (e instanceof AgeSignalsException) {
                            errorCode = ((AgeSignalsException) e).getErrorCode();
                        }
                        callback.onError(errorCode, e.getMessage());
                    }
                });
    }
}
