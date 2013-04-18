using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Runtime;
using Ouya.Csharp;

namespace Ouya.Console.Api
{
    class ReceiptsListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        TaskCompletionSource<IList<Receipt>> _tcs;
        PurchaseUtils _purchaseUtils;

        public ReceiptsListener(TaskCompletionSource<IList<Receipt>> tcs, PurchaseUtils purchaseUtils)
        {
            _tcs = tcs;
            _purchaseUtils = purchaseUtils;
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
            var receipts = _purchaseUtils.CreateReceiptsFromResponse(str.ToString());
            _tcs.SetResult(receipts);
        }
    }
}