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