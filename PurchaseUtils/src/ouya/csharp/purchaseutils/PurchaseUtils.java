/*
 * Copyright (C) 2012 OUYA, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package ouya.csharp.purchaseutils;

import org.json.JSONException;
import org.json.JSONObject;

import android.util.Base64;
import android.util.Log;
import tv.ouya.console.api.*;
import javax.crypto.BadPaddingException;
import javax.crypto.Cipher;
import javax.crypto.IllegalBlockSizeException;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.SecretKey;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.security.*;
import java.security.spec.X509EncodedKeySpec;
import java.text.ParseException;
import java.util.*;

public class PurchaseUtils
{
	private static final String LOG_TAG = "Ouya.CSharp.PurchaseUtils";
	private PublicKey mPublicKey;
	private boolean mSetTestMode;
	
	public PurchaseUtils(byte[] applicationKey, boolean setTestMode)
	{
		mSetTestMode = setTestMode;
		
        // Create a PublicKey object from the key data downloaded from the developer portal.
        try {
            X509EncodedKeySpec keySpec = new X509EncodedKeySpec(applicationKey);
            KeyFactory keyFactory = KeyFactory.getInstance("RSA");
            mPublicKey = keyFactory.generatePublic(keySpec);
        } catch (Exception e) {
            Log.e(LOG_TAG, "Unable to create encryption key", e);
        }		
	}
	
    public Purchasable CreatePurchasable(String productId, String uniquePurchaseId) 
    {
    	Log.e(LOG_TAG, "CreatePurchasable started: " + productId);
    	
        SecureRandom sr = null;
		try {
			sr = SecureRandom.getInstance("SHA1PRNG");
		} catch (NoSuchAlgorithmException e) {
			Log.e(LOG_TAG, "SecureRandom.getInstance failed", e);
			return null;
		}
    	
        JSONObject purchaseRequest = new JSONObject();
        try {
			purchaseRequest.put("uuid", uniquePurchaseId);
	        purchaseRequest.put("identifier", productId);
	        if (mSetTestMode) {	        	
	        	purchaseRequest.put("testing", "true"); // This value is only needed for testing, not setting it results in a live purchase
	        }
		} catch (JSONException e) {
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		}
        
        String purchaseRequestJson = purchaseRequest.toString();

        byte[] keyBytes = new byte[16];
        sr.nextBytes(keyBytes);
        SecretKey key = new SecretKeySpec(keyBytes, "AES");

        byte[] ivBytes = new byte[16];
        sr.nextBytes(ivBytes);
        IvParameterSpec iv = new IvParameterSpec(ivBytes);
        
        Cipher cipher;
        byte[] payload = null;
        byte[] encryptedKey = null;
		try 
		{
			cipher = Cipher.getInstance("AES/CBC/PKCS5Padding", "BC");
	        cipher.init(Cipher.ENCRYPT_MODE, key, iv);
	        payload = cipher.doFinal(purchaseRequestJson.getBytes("UTF-8"));

	        cipher = Cipher.getInstance("RSA/ECB/PKCS1Padding", "BC");
	        cipher.init(Cipher.ENCRYPT_MODE, mPublicKey);
	        encryptedKey = cipher.doFinal(keyBytes);
		} 
		catch (NoSuchAlgorithmException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		} 
		catch (NoSuchProviderException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		}
		catch (NoSuchPaddingException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		}
		catch (InvalidKeyException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		} 
		catch (InvalidAlgorithmParameterException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		}
		catch (IllegalBlockSizeException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		}
		catch (BadPaddingException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		} 
		catch (UnsupportedEncodingException e) { 
			Log.e(LOG_TAG, "Unable to create purchase request", e);
			return null;
		}
    	
        Purchasable purchasable =
                new Purchasable(
                        productId,
                        Base64.encodeToString(encryptedKey, Base64.NO_WRAP),
                        Base64.encodeToString(ivBytes, Base64.NO_WRAP),
                        Base64.encodeToString(payload, Base64.NO_WRAP) );
    	
    	Log.e(LOG_TAG, "CreatePurchasable completed: " + productId);
        
        return purchasable;
    }
    
    public List<Receipt> CreateReceiptsFromResponse(String receiptsResponse)
    {
        OuyaEncryptionHelper helper = new OuyaEncryptionHelper();
        List<Receipt> receipts;
        try {
            JSONObject response = new JSONObject(receiptsResponse);
            if(response.has("key") && response.has("iv")) {
                receipts = helper.decryptReceiptResponse(response, mPublicKey);
            } else {
                receipts = helper.parseJSONReceiptResponse(receiptsResponse);
            }
        } catch (ParseException e) { 
			Log.e(LOG_TAG, "Unable to create purchase receipts", e);
			return null;
		} catch (JSONException e) { 
			Log.e(LOG_TAG, "Unable to create purchase receipts", e);
			return null;
		} catch (GeneralSecurityException e) { 
			Log.e(LOG_TAG, "Unable to create purchase receipts", e);
			return null;
		} catch (IOException e) { 
			Log.e(LOG_TAG, "Unable to create purchase receipts", e);
			return null;
		}
        Collections.sort(receipts, new Comparator<Receipt>() {
            @Override
            public int compare(Receipt lhs, Receipt rhs) {
                return rhs.getPurchaseDate().compareTo(lhs.getPurchaseDate());
            }
        });

        return receipts;
    }

    public boolean IsPurchaseResponseMatching(String purchaseResponse, String productId, String uniquePurchaseId)
    {
    	try
    	{
	        OuyaEncryptionHelper helper = new OuyaEncryptionHelper();
	    	
	        JSONObject response = new JSONObject(purchaseResponse);
	        if (response.has("key") && response.has("iv")) 
	        {
	            String id = helper.decryptPurchaseResponse(response, mPublicKey);
	        	if (id != uniquePurchaseId)
	        	{
	        		return false;
	        	}
	        } 
	        else 
	        {
	            Product p = new Product(response);
	            if (p.getIdentifier() != productId)
	            {
	            	return false;
	            }
	        }
	    } catch (ParseException e) { 
			Log.e(LOG_TAG, "Unable to determine whether purchase response matches", e);
			return false;
		} catch (JSONException e) { 
			Log.e(LOG_TAG, "Unable to determine whether purchase response matches", e);
			return false;
		} catch (IOException e) {
			Log.e(LOG_TAG, "Unable to determine whether purchase response matches", e);
	    	return false;
	    } catch (GeneralSecurityException e) { 
			Log.e(LOG_TAG, "Unable to determine whether purchase response matches", e);
			return false;
		}
    	return true;
    }
}
