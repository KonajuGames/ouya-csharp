// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using Android.OS;

namespace Ouya.Console.Api
{
    /// <summary>
    /// Exception thrown by OUYA requests when errors occur.
    /// </summary>
    public class OuyaRequestException : Exception
    {
        Bundle _bundle;

        /// <summary>
        /// Gets the error code associated with the error.
        /// </summary>
        public int ErrorCode
        {
            get
            {
                return HResult;
            }
        }

        /// <summary>
        /// Gets the bundle that may hold additional error information.
        /// </summary>
        public Bundle Bundle
        {
            get
            {
                return _bundle;
            }
        }

        /// <summary>
        /// Internal constructor to create the OuyaRequestionException object.
        /// </summary>
        /// <param name="errorCode">The error code associated with the error.</param>
        /// <param name="errorMessage">The error message associated with the error.</param>
        /// <param name="bundle">The bundle that may hold additional error information.</param>
        internal OuyaRequestException(int errorCode, string errorMessage, Bundle bundle)
            : base(errorMessage)
        {
            HResult = errorCode;
            _bundle = bundle;
        }
    }
}