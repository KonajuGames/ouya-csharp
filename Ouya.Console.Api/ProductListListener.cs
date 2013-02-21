using System.Threading.Tasks;
using Android.Runtime;
using System.Collections.Generic;

namespace Ouya.Console.Api
{
    class ProductListListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        TaskCompletionSource<IList<Product>> tcs;

        public ProductListListener(TaskCompletionSource<IList<Product>> tcs)
        {
            this.tcs = tcs;
        }

        public void OnCancel()
        {
            tcs.SetCanceled();
        }

        public void OnFailure(int errorCode, string errorMessage, global::Android.OS.Bundle optionalData)
        {
            tcs.SetException(new OuyaRequestException(errorCode, errorMessage));
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var list = result.JavaCast<JavaList<Product>>();
            tcs.SetResult(list);
        }
    }
}