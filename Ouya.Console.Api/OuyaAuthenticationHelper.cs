// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System.Threading.Tasks;
using Android.App;
using Android.OS;

namespace Ouya.Console.Api
{
    public partial class OuyaAuthenticationHelper
    {
        public Task HandleError(Activity activity, int errorCode, string errorMessage, Bundle bundle, int authActivityId, out bool handled)
        {
            var tcs = new TaskCompletionSource<bool>();
            handled = HandleError(activity, errorCode, errorMessage, bundle, authActivityId, new VoidListener(tcs));
            return tcs.Task;
        }
    }
}