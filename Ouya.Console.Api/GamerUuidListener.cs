// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Android.OS;
using Android.Runtime;
using Org.Json;

namespace Ouya.Console.Api
{
    class GamerUuidListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        const string gamerUuidFileName = "gameruuid.dat";

        TaskCompletionSource<string> _tcs;

        public GamerUuidListener(TaskCompletionSource<string> tcs)
        {
            _tcs = tcs;
        }

        public void OnCancel()
        {
            _tcs.SetCanceled();
        }

        public void OnFailure(int errorCode, string errorMessage, Bundle optionalData)
        {
            // If we have a cached result, return that
            string gamerUuid = string.Empty;
            try
            {
                // Parse gamerUuid from file
                gamerUuid = FromCache();
            }
            catch (Exception e)
            {
                OuyaFacade.Log("Error decaching gamerUuid: " + e.Message);
                _tcs.SetException(new OuyaRequestException(errorCode, errorMessage, optionalData));
            }

            _tcs.SetResult(gamerUuid);
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var str = result.JavaCast<Java.Lang.String>().ToString();
            // Parse gamerUuid into string
            string gamerUuid = string.Empty;
            try
            {
                gamerUuid = str;
                // Cache the gamerUuid to file for later use when the network may not be accessible
                ToCache(gamerUuid);
            }
            catch (Exception e)
            {
                OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Error retrieving gamerUuid: " + e.Message, Bundle.Empty);
            }

            _tcs.SetResult(gamerUuid);
        }

        // Save gamerUuid to file.
        static void ToCache(string gamerUuid)
        {
            OuyaFacade.Log("Caching gamerUuid");
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var writer = new StreamWriter(store.OpenFile(gamerUuidFileName, FileMode.OpenOrCreate)))
                {
                    writer.Write(gamerUuid);
                }
            }
        }

        // Load the cached gamerUuid from file
        static internal string FromCache()
        {
            OuyaFacade.Log("Returning cached gamerUuid");
            string gamerUuid = null;
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(gamerUuidFileName))
                {
                    using (var reader = new StreamReader(store.OpenFile(gamerUuidFileName, FileMode.Open)))
                    {
                        gamerUuid = reader.ReadToEnd();
                        try
                        {
                            Guid.Parse(gamerUuid);
                        }
                        catch (Exception)
                        {
                            OuyaFacade.Log("Incorrectly formatted gamerUuid");
                            gamerUuid = null;
                        }
                    }
                }
            }
            return gamerUuid;
        }
    }
}