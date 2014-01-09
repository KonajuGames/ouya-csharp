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
    class GamerInfoListener : global::Java.Lang.Object, IOuyaResponseListener
    {
        const string gamerInfoFileName = "gamerinfo.dat";
        const int gamerInfoVersion = 1;

        TaskCompletionSource<GamerInfo> _tcs;

        public GamerInfoListener(TaskCompletionSource<GamerInfo> tcs)
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
            GamerInfo gamerInfo = null;
            try
            {
                // Parse gamerUuid from file
                gamerInfo = FromCache();
            }
            catch (Exception e)
            {
                OuyaFacade.Log("Error decaching gamerInfo: " + e.Message);
                _tcs.SetException(new OuyaRequestException(errorCode, errorMessage, optionalData));
            }

            _tcs.SetResult(gamerInfo);
        }

        public void OnSuccess(global::Java.Lang.Object result)
        {
            var gamerInfo = result.JavaCast<GamerInfo>();
            try
            {
                // Cache the gamerInfo to file for later use when the network may not be accessible
                ToCache(gamerInfo);
            }
            catch (Exception e)
            {
                OnFailure(OuyaErrorCodes.ThrowDuringOnSuccess, "Error retrieving gamerInfo: " + e.Message, Bundle.Empty);
            }

            _tcs.SetResult(gamerInfo);
        }

        // Save gamerInfo to file.
        static void ToCache(GamerInfo gamerInfo)
        {
            OuyaFacade.Log("Caching gamerInfo uuid:" + gamerInfo.Uuid + " name:" + gamerInfo.Username);
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var writer = new BinaryWriter(store.OpenFile(gamerInfoFileName, FileMode.OpenOrCreate)))
                    {
                        writer.Write(gamerInfoVersion);
                        writer.Write(gamerInfo.Uuid);
                        writer.Write(gamerInfo.Username);
                    }
                }
            }
            catch (Exception e)
            {
                OuyaFacade.Log("Error caching gamerInfo: " + e.Message);
            }
        }

        // Load the cached gamerInfo from file
        static internal GamerInfo FromCache()
        {
            OuyaFacade.Log("Returning cached gamerInfo");
            GamerInfo gamerInfo = null;
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(gamerInfoFileName))
                    {
                        using (var reader = new BinaryReader(store.OpenFile(gamerInfoFileName, FileMode.Open)))
                        {
                            var version = reader.ReadInt32();
                            switch (version)
                            {
                                case 1:
                                    {
                                        var uuid = reader.ReadString();
                                        try
                                        {
                                            Guid.Parse(uuid);
                                            var userName = reader.ReadString();
                                            gamerInfo = new GamerInfo(uuid, userName);
                                        }
                                        catch (Exception e2)
                                        {
                                            OuyaFacade.Log("Incorrectly formatted gamerUuid: " + e2.Message);
                                        }
                                    }
                                    break;

                                default:
                                    throw new InvalidDataException("Unknown cache version number " + version);
                            }
                        }
                    }
                }
            }
            catch (Exception e1)
            {
                OuyaFacade.Log("Error decaching gamerInfo: " + e1.Message);
            }
            return gamerInfo;
        }
    }
}