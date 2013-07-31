// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Runtime;
using System.IO.IsolatedStorage;
using System.IO;
using Java.Security;
using Org.Json;
using Android.OS;
using System;

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
            var str = FromCache();
            if (!string.IsNullOrEmpty(str))
            {
                // Parse receipts into a list
                IList<Receipt> receipts = null;
                try
                {
                    receipts = ReceiptsFromResponse(str);
                }
                catch (Exception e)
                {
                    OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Error decrypting receipts: " + e.Message, Bundle.Empty);
                }

                _tcs.SetResult(receipts);
            }
            else
            {
                // Otherwise throw the exception
                _tcs.SetException(new OuyaRequestException(errorCode, errorMessage, optionalData));
            }
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var str = result.JavaCast<Java.Lang.String>().ToString();
            // Cache the receipts to file
            ToCache(str);
            // Parse receipts into a list
            IList<Receipt> receipts = null;
            try
            {
                receipts = ReceiptsFromResponse(str);
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
                    receipts = helper.DecryptReceiptResponse(response, _publicKey);
                }
            }
            return receipts;
        }

        /// <summary>
        /// Encrypt the receipts and save them to file.
        /// </summary>
        /// <param name="receipts">The plaintext receipts</param>
        void ToCache(string receipts)
        {
            var encryptedReceipts = CryptoHelper.Encrypt(receipts, _gamerUuid);
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var writer = new StreamWriter(store.OpenFile(receiptsFileName, FileMode.OpenOrCreate)))
                {
                    writer.Write(encryptedReceipts);
                }
            }
        }

        /// <summary>
        /// Load the cached receipts from file and return the decrypted result.
        /// </summary>
        /// <returns>The plaintext receipts</returns>
        string FromCache()
        {
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
                return CryptoHelper.Decrypt(encryptedReceipts, _gamerUuid);
            return string.Empty;
        }
    }
}