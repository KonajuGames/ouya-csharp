// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Runtime;
using System;
using Org.Json;
using Java.Security;
using Android.OS;

namespace Ouya.Console.Api
{
    class PurchaseListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        TaskCompletionSource<bool> _tcs;
        IPublicKey _publicKey;
        Product _product;
        string _uniquePurchaseId;

        public PurchaseListener(TaskCompletionSource<bool> tcs, IPublicKey publicKey, Product product, string uniquePurchaseId)
        {
            _tcs = tcs;
            _publicKey = publicKey;
            _product = product;
            _uniquePurchaseId = uniquePurchaseId;
        }

        public void OnCancel()
        {
            _tcs.SetCanceled();
        }

        public void OnFailure(int errorCode, string errorMessage, Bundle optionalData)
        {
            _tcs.SetException(new OuyaRequestException(errorCode, errorMessage, optionalData));
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var str = result.JavaCast<Java.Lang.String>().ToString();
            using (var helper = new OuyaEncryptionHelper())
            {
                var response = new JSONObject(str);
                var id = helper.DecryptPurchaseResponse(response, _publicKey);
                if (id != _uniquePurchaseId)
                    OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Received purchase ID does not match what we expected to receive", Bundle.Empty);
                _tcs.SetResult(true);
            }
        }
    }
}