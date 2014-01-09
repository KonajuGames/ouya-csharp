// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using Java.Security;
using Java.Security.Spec;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Org.Json;

namespace Ouya.Console.Api
{
    /// <summary>
    /// Entry point for the OUYA API. Applications should use the singleton instance.
    /// </summary>
    public partial class OuyaFacade
    {
        // Local copy of gamer uuid for receipt caching
        string _gamerUuid;

        // Public key for decrypting responses
        IPublicKey _publicKey;

        // Default timeout of 30 seconds
        int timeout = 30000; //*/System.Threading.Timeout.Infinite;

        /// <summary>
        /// The timeout for the asynchronous requests, specified in milliseconds.
        /// </summary>
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
                if (timeout < 0)
                    timeout = 0;
            }
        }

        [Conditional("DEBUG")]
        static internal void Log(string msg)
        {
            Android.Util.Log.Debug("OUYA-C#", msg);
        }

        /// <summary>
        /// Initializes the facade. Should only be called once.
        /// </summary>
        /// <param name="context">An Android Context object. Usually the Activity object.</param>
        /// <param name="developerUuid">The developer UUID, which is obtained from the developer portal.</param>
        public void Init(Android.Content.Context context, string developerUuid)
        {
            // Load the key from the resources
            byte[] applicationKey = null;
            var resId = context.Resources.GetIdentifier("key", "raw", context.PackageName);
            using (var stream = context.Resources.OpenRawResource(resId))
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    applicationKey = ms.ToArray();
                }
            }
            
            // Generate the public key from the application key
            using (var keySpec = new X509EncodedKeySpec(applicationKey))
            {
                using (var keyFactory = KeyFactory.GetInstance("RSA"))
                {
                    _publicKey = keyFactory.GeneratePublic(keySpec);
                }
            }

            InitInternal(context, developerUuid);
        }

        /// <summary>
        /// Requests the current gamer's UUID.
        /// </summary>
        /// <returns>The UUID of the gamer to whom the console is currently registered.</returns>
        public async Task<string> RequestGamerUuidAsync()
        {
            if (!String.IsNullOrEmpty(_gamerUuid))
                return _gamerUuid;
            var tcs = new TaskCompletionSource<string>();
            var listener = new GamerUuidListener(tcs);
            try
            {
                RequestGamerUuid(listener);
                _gamerUuid = await tcs.Task.TimeoutAfter(timeout);
            }
            catch (Exception e)
            {
                Log(e.GetType().Name + ": " + e.Message);
                _gamerUuid = GamerUuidListener.FromCache();
            }
            return _gamerUuid;
        }

        /// <summary>
        /// Returns a list of Product objects that describe the products (including current price) associated with the specified list of Purchasables.
        /// </summary>
        /// <param name="purchasables">One or more string IDs that identify the products to be returned.</param>
        /// <returns>The list of Product objects.</returns>
        public async Task<IList<Product>> RequestProductListAsync(params string[] purchasables)
        {
            var list = new JavaList<Purchasable>();
            foreach (var purchasable in purchasables)
            {
                list.Add(new Purchasable(purchasable));
            }
            return await RequestProductListAsync(list);
        }

        /// <summary>
        /// Returns a list of Product objects that describe the products (including current price) associated with the specified list of Purchasables.
        /// </summary>
        /// <param name="purchasables">A list of Purchasable objects that identify the products to be returned.</param>
        /// <returns>The list of Product objects.</returns>
        public async Task<IList<Product>> RequestProductListAsync(IList<Purchasable> purchasables)
        {
            var tcs = new TaskCompletionSource<IList<Product>>();
            var listener = new ProductListListener(tcs);
            RequestProductList(purchasables, listener);
            return await tcs.Task.TimeoutAfter(timeout);
        }

        /// <summary>
        /// Requests that the specified Purchasable be purchased on behalf of the current user.
        /// The IAP client service is responsible for identifying the user and requesting credentials as appropriate,
        /// as well as providing all of the UI for the purchase flow. When purchases are successful, a Product object
        /// is returned that describes the product that was purchased.
        /// </summary>
        /// <param name="product">The Purchasable object that describes the item to be purchased.</param>
        /// <returns>Returns true if the purchase was successful.</returns>
        public async Task<bool> RequestPurchaseAsync(Product product)
        {
            if (ReferenceEquals(product, null))
                throw new ArgumentNullException("product");

            var tcs = new TaskCompletionSource<bool>();

            // Create the Purchasable object from the supplied product
            var sr = SecureRandom.GetInstance("SHA1PRNG");

            // This is an ID that allows you to associate a successful purchase with
            // it's original request. The server does nothing with this string except
            // pass it back to you, so it only needs to be unique within this instance
            // of your app to allow you to pair responses with requests.
            var uniqueId = sr.NextLong().ToString("X");

            JSONObject purchaseRequest = new JSONObject();
            purchaseRequest.Put("uuid", uniqueId);
            purchaseRequest.Put("identifier", product.Identifier);
            var purchaseRequestJson = purchaseRequest.ToString();

            byte[] keyBytes = new byte[16];
            sr.NextBytes(keyBytes);
            var key = new SecretKeySpec(keyBytes, "AES");

            byte[] ivBytes = new byte[16];
            sr.NextBytes(ivBytes);
            var iv = new IvParameterSpec(ivBytes);

            Cipher cipher = Cipher.GetInstance("AES/CBC/PKCS5Padding", "BC");
            cipher.Init(CipherMode.EncryptMode, key, iv);
            var payload = cipher.DoFinal(Encoding.UTF8.GetBytes(purchaseRequestJson));

            cipher = Cipher.GetInstance("RSA/ECB/PKCS1Padding", "BC");
            cipher.Init(CipherMode.EncryptMode, _publicKey);
            var encryptedKey = cipher.DoFinal(keyBytes);

            var purchasable = new Purchasable(
                        product.Identifier,
                        Convert.ToBase64String(encryptedKey, Base64FormattingOptions.None),
                        Convert.ToBase64String(ivBytes, Base64FormattingOptions.None),
                        Convert.ToBase64String(payload, Base64FormattingOptions.None));

            var listener = new PurchaseListener(tcs, _publicKey, product, uniqueId);
            RequestPurchase(purchasable, listener);
            // No timeout for purchase as it shows a user dialog
            return await tcs.Task;
        }

        /// <summary>
        /// Requests the current receipts from the Store. If the Store is not available, the cached
        /// receipts from a previous call are returned.
        /// </summary>
        /// <returns>The list of Receipt objects.</returns>
        public async Task<IList<Receipt>> RequestReceiptsAsync()
        {
            // We need the gamer UUID for the encryption of the cached receipts, so if the dev
            // hasn't retrieved the gamer UUID yet, we'll grab it now.
            var task = Task<IList<Receipt>>.Factory.StartNew(() =>
                {
                    if (string.IsNullOrEmpty(_gamerUuid))
                        _gamerUuid = RequestGamerUuidAsync().Result;
                    // No gamerUuid means no receipts
                    if (string.IsNullOrEmpty(_gamerUuid))
                        return null;
                    var tcs = new TaskCompletionSource<IList<Receipt>>();
                    var listener = new ReceiptsListener(tcs, _publicKey, _gamerUuid);
                    RequestReceipts(listener);
                    return tcs.Task.TimeoutAfter(timeout).Result;
                });
            try
            {
                return await task;
            }
            catch (Exception e)
            {
                Log(e.GetType().Name + ": " + e.Message);
            }
            return ReceiptsListener.FromCache(_gamerUuid);
        }
    }
}