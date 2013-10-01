// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Android.OS;
using Android.Runtime;
using Java.Security;
using Org.Json;

namespace Ouya.Console.Api
{
    class ReceiptsListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        const string receiptsFileName = "receipts.dat";

        TaskCompletionSource<IList<Receipt>> _tcs;
        IPublicKey _publicKey;
        string _gamerUuid;

        public ReceiptsListener(TaskCompletionSource<IList<Receipt>> tcs, IPublicKey publicKey, string gamerUuid)
        {
            _tcs = tcs;
            _publicKey = publicKey;
            _gamerUuid = gamerUuid;
        }

        public void OnCancel()
        {
            _tcs.SetCanceled();
        }

        public void OnFailure(int errorCode, string errorMessage, Bundle optionalData)
        {
            // If we have a cached result, return that
            IList<Receipt> receipts = null;
            try
            {
                // Parse receipts into a list
                receipts = FromCache(_gamerUuid);
            }
            catch (Exception e)
            {
                OuyaFacade.Log("Error decaching receipts: " + e.Message);
                _tcs.SetException(new OuyaRequestException(errorCode, errorMessage, optionalData));
            }

            _tcs.SetResult(receipts);
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var str = result.JavaCast<Java.Lang.String>().ToString();
            // Parse receipts into a list
            IList<Receipt> receipts = null;
            try
            {
                receipts = ReceiptsFromResponse(str, _publicKey);
                // Cache the receipts to file for later use when the network may not be accessible
                ToCache(receipts, _gamerUuid);
            }
            catch (Exception e)
            {
                OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Error decrypting receipts: " + e.Message, Bundle.Empty);
            }

            _tcs.SetResult(receipts);
        }

        static IList<Receipt> ReceiptsFromResponse(string receiptsResponse, IPublicKey publicKey)
        {
            IList<Receipt> receipts = null;
            using (var helper = new OuyaEncryptionHelper())
            {
                using (var response = new JSONObject(receiptsResponse))
                {
                    OuyaFacade.Log("Decrypting receipts response");
                    receipts = helper.DecryptReceiptResponse(response, publicKey);
                }
            }
            return receipts;
        }

        // Encrypt the receipts and save them to file.
        static void ToCache(IList<Receipt> receipts, string gamerUuid)
        {
            OuyaFacade.Log("Caching receipts");
            var json = new JSONObject();
            var array = new JSONArray();
            foreach (var receipt in receipts)
            {
                var r = new JSONObject();
                r.Put("identifier", receipt.Identifier);
                // PriceInCents is now deprecated. Use LocalPrice and CurrencyCode instead.
                // Retain field for compatibility.
                r.Put("priceInCents", 0);
                r.Put("purchaseDate", receipt.PurchaseDate.ToGMTString());
                r.Put("generatedDate", receipt.GeneratedDate.ToGMTString());
                r.Put("gamerUuid", receipt.Gamer);
                r.Put("uuid", receipt.Uuid);
                r.Put("localPrice", receipt.LocalPrice);
                r.Put("currencyCode", receipt.Currency);
                array.Put(r);
            }
            json.Accumulate("receipts", array);
            var text = json.ToString();
            var encryptedReceipts = CryptoHelper.Encrypt(text, gamerUuid);
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var writer = new StreamWriter(store.OpenFile(receiptsFileName, FileMode.OpenOrCreate)))
                {
                    writer.Write(encryptedReceipts);
                }
            }
        }

        // Load the cached receipts from file and return the decrypted result.
        static internal IList<Receipt> FromCache(string gamerUuid)
        {
            OuyaFacade.Log("Returning cached receipts");
            IList<Receipt> receipts = null;
            string encryptedReceipts = string.Empty;
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(receiptsFileName))
                {
                    using (var reader = new StreamReader(store.OpenFile(receiptsFileName, FileMode.Open)))
                    {
                        encryptedReceipts = reader.ReadToEnd();
                    }
                }
            }
            if (!string.IsNullOrEmpty(encryptedReceipts))
            {
                var decryptedReceipts = CryptoHelper.Decrypt(encryptedReceipts, gamerUuid);
                var json = new JSONObject(decryptedReceipts);
                var list = json.OptJSONArray("receipts");
                if (list != null)
                {
                    receipts = new List<Receipt>(list.Length());
                    for (int i = 0; i < list.Length(); ++i)
                    {
                        var node = list.GetJSONObject(i);
                        var identifier = node.GetString("identifier");
                        var priceInCents = node.GetInt("priceInCents");
                        var purchaseDate = new Java.Util.Date(node.GetString("purchaseDate"));
                        var generatedDate = new Java.Util.Date(node.GetString("generatedDate"));
                        var gamer = node.GetString("gamerUuid");
                        var uuid = node.GetString("uuid");
                        // Cater for reading old receipts written with pre-1.0.8
                        double localPrice = priceInCents / 100.0;
                        string currencyCode = "USD";
                        try
                        {
                            localPrice = node.GetDouble("localPrice");
                            currencyCode = node.GetString("currencyCode");
                        }
                        catch (JSONException)
                        {
                            OuyaFacade.Log("Older receipt found. Assuming USD price.");
                        }
                        var receipt = new Receipt(identifier, priceInCents, purchaseDate, generatedDate, gamer, uuid, localPrice, currencyCode);
                        receipts.Add(receipt);
                    }
                }
            }
            // Return an empty list if nothing was found
            if (receipts == null)
                receipts = new List<Receipt>();
            return receipts;
        }
    }
}