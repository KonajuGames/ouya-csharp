// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System.Threading.Tasks;
using Android.Runtime;

namespace Ouya.Console.Api
{
    class VoidListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        TaskCompletionSource<bool> tcs;

        public VoidListener(TaskCompletionSource<bool> tcs)
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
            tcs.SetResult(true);
        }
    }
}