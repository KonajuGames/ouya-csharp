// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;

namespace Ouya.Console.Api
{
    public class OuyaRequestException : Exception
    {
        internal OuyaRequestException(int errorCode, string errorMessage)
            : base(errorMessage)
        {
            HResult = errorCode;
        }
    }
}