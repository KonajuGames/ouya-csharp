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
                receipts = FromCache();
            }
            catch (Exception e)
            {
                OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Error decrypting receipts: " + e.Message, Bundle.Empty);
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
                receipts = ReceiptsFromResponse(str);
                // Cache the receipts to file for later use when the network may not be accessible
                ToCache(receipts);
            }
            catch (Exception e)
            {
                OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Error decrypting receipts: " + e.Message, Bundle.Empty);
            }

            _tcs.SetResult(receipts);
        }

        IList<Receipt> ReceiptsFromResponse(string receiptsResponse)
        {
            IList<Receipt> receipts = null;
            using (var helper = new OuyaEncryptionHelper())
            {
                using (var response = new JSONObject(receiptsResponse))
                {
                    OuyaFacade.Log(response.ToString(2));
                    OuyaFacade.Log("Decrypting receipts response");
                    receipts = helper.DecryptReceiptResponse(response, _publicKey);
                }
            }
            return receipts;
        }

        // Encrypt the receipts and save them to file.
        void ToCache(IList<Receipt> receipts)
        {
            OuyaFacade.Log("Caching receipts");
            var json = new JSONObject();
            foreach (var receipt in receipts)
            {
                var r = new JSONObject();
                r.Put("identifier", receipt.Identifier);
                r.Put("priceInCents", receipt.PriceInCents);
                r.Put("purchaseDate", receipt.PurchaseDate.ToGMTString());
                r.Put("generatedDate", receipt.GeneratedDate.ToGMTString());
                r.Put("gamerUuid", receipt.Gamer);
                r.Put("uuid", receipt.Uuid);
                json.Accumulate("receipts", r);
            }
            var text = json.ToString();
            var encryptedReceipts = CryptoHelper.Encrypt(text, _gamerUuid);
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var writer = new StreamWriter(store.OpenFile(receiptsFileName, FileMode.OpenOrCreate)))
                {
                    writer.Write(encryptedReceipts);
                }
            }
        }

        // Load the cached receipts from file and return the decrypted result.
        IList<Receipt> FromCache()
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
                var decryptedReceipts = CryptoHelper.Decrypt(encryptedReceipts, _gamerUuid);
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
                        var gamerUuid = node.GetString("gamerUuid");
                        var uuid = node.GetString("uuid");
                        var receipt = new Receipt(identifier, priceInCents, purchaseDate, generatedDate, gamerUuid, uuid);
                        receipts.Add(receipt);
                    }
                }
            }
            return receipts;
        }
    }
}