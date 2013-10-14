// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;

namespace Ouya.Console.Api
{
    /// <summary>
    /// Async support for the OuyaAuthenticationHelper class.
    /// </summary>
    public partial class OuyaAuthenticationHelper
    {
        /// <summary>
        /// Handles errors asynchronously.
        /// </summary>
        /// <param name="activity">Reference to the current Android activity.</param>
        /// <param name="errorCode">The error code returned by the previous operation.</param>
        /// <param name="errorMessage">An error message to be displayed to the user.</param>
        /// <param name="bundle">The bundle holding any additional error information.</param>
        /// <param name="authActivityId">The ID to give the re-authentication activity if it needs to be started.</param>
        /// <param name="handled">Set to true if the OUYA system handled the error.</param>
        /// <returns>The task object for asynchronous operation.</returns>
        public Task HandleErrorAsync(Activity activity, int errorCode, string errorMessage, Bundle bundle, int authActivityId, out bool handled)
        {
            var tcs = new TaskCompletionSource<bool>();
            handled = HandleError(activity, errorCode, errorMessage, bundle, authActivityId, new VoidListener(tcs));
            return tcs.Task;
        }
    }
}