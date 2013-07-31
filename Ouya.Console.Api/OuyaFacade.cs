// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Java.Security;
using Java.Security.Spec;
using Android.Runtime;
using Org.Json;
using Javax.Crypto.Spec;
using Javax.Crypto;
using System.Text;

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

        /// <summary>
        /// Initializes the facade. Should only be called once.
        /// </summary>
        /// <param name="context">An Android Context object. Usually the Activity object.</param>
        /// <param name="developerUuid">The developer UUID, which is obtained from the developer portal.</param>
        /// <param name="applicationKey">The application key obtained from the developer portal.</param>
        public void Init(Android.Content.Context context, string developerUuid, byte[] applicationKey)
        {
            // Generate the public key from the application key
            using (var keySpec = new X509EncodedKeySpec(applicationKey))
            {
                using (var keyFactory = KeyFactory.GetInstance("RSA"))
                {
                    _publicKey = keyFactory.GeneratePublic(keySpec);
                }
            }

            Init(context, developerUuid);
        }

        /// <summary>
        /// Requests the current gamer's UUID.
        /// </summary>
        /// <returns>The UUID of the gamer to whom the console is currently registered.</returns>
        public Task<string> RequestGamerUuidAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            RequestGamerUuid(new StringListener(tcs));
            tcs.Task.ContinueWith(t => ReceivedGamerUuid(t), TaskContinuationOptions.OnlyOnRanToCompletion);
            return tcs.Task;
        }

        void ReceivedGamerUuid(Task<string> task)
        {
            _gamerUuid = task.Result;
        }

        /// <summary>
        /// Returns a list of Product objects that describe the products (including current price) associated with the specified list of Purchasables.
        /// </summary>
        /// <param name="purchasables">The string IDs that identify the products to be returned.</param>
        /// <returns>The list of Product objects.</returns>
        public Task<IList<Product>> RequestProductListAsync(params string[] purchasables)
        {
            var list = new JavaList<Purchasable>();
            foreach (var purchasable in purchasables)
            {
                list.Add(new Purchasable(purchasable));
            }
            return RequestProductListAsync(list);
        }

        /// <summary>
        /// Returns a list of Product objects that describe the products (including current price) associated with the specified list of Purchasables.
        /// </summary>
        /// <param name="purchasables">The Purchasable objects that identify the products to be returned.</param>
        /// <returns>The list of Product objects.</returns>
        public Task<IList<Product>> RequestProductListAsync(IList<Purchasable> purchasables)
        {
            var tcs = new TaskCompletionSource<IList<Product>>();
            RequestProductList(purchasables, new ProductListListener(tcs));
            return tcs.Task;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public Task<bool> RequestPurchaseAsync(Product product)
        {
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

            RequestPurchase(purchasable, new PurchaseListener(tcs, _publicKey, product, uniqueId));
            return tcs.Task;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IList<Receipt>> RequestReceiptsAsync()
        {
            var tcs = new TaskCompletionSource<IList<Receipt>>();
            RequestReceipts(new ReceiptsListener(tcs, _publicKey, _gamerUuid));
            return tcs.Task;
        }
    }
}