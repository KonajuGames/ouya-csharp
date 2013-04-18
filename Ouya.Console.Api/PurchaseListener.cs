using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Runtime;
using Ouya.Csharp;

namespace Ouya.Console.Api
{
    class PurchaseListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        TaskCompletionSource<bool> _tcs;
        PurchaseUtils _purchaseUtils;
        Product _product;
        string _uniquePurchaseId;

        public PurchaseListener(TaskCompletionSource<bool> tcs, PurchaseUtils purchaseUtils, Product product, string uniquePurchaseId)
        {
            _tcs = tcs;
            _purchaseUtils = purchaseUtils;
            _product = product;
            _uniquePurchaseId = uniquePurchaseId;
        }

        public void OnCancel()
        {
            _tcs.SetCanceled();
        }

        public void OnFailure(int errorCode, string errorMessage, global::Android.OS.Bundle optionalData)
        {
            _tcs.SetException(new OuyaRequestException(errorCode, errorMessage));
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var str = result.JavaCast<Java.Lang.String>();
            bool purchaseSucceeded = _purchaseUtils.IsPurchaseResponseMatching(str.ToString(), _product, _uniquePurchaseId);
            _tcs.SetResult(purchaseSucceeded);
        }
    }
}