// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using Android.OS;

namespace Ouya.Console.Api
{
    public class OuyaRequestException : Exception
    {
        Bundle _bundle;

        public int ErrorCode
        {
            get
            {
                return HResult;
            }
        }

        public Bundle Bundle
        {
            get
            {
                return _bundle;
            }
        }

        internal OuyaRequestException(int errorCode, string errorMessage, Bundle bundle)
            : base(errorMessage)
        {
            HResult = errorCode;
            _bundle = bundle;
        }
    }
}