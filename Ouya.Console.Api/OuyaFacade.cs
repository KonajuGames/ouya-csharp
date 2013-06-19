// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ouya.Csharp;

namespace Ouya.Console.Api
{
    public partial class OuyaFacade
    {
        public PurchaseUtils PurchaseUtils { get; private set; }

        public void Init(Android.Content.Context context, string developerUuid, byte[] applicationKey)
        {
            Init(context, developerUuid);
            PurchaseUtils = new PurchaseUtils(applicationKey);
        }

        [Obsolete("Obsoleted by RequestGamerUuidAsync")]
        public Task<string> RequestGamerUuid()
        {
            return RequestGamerUuidAsync();
        }

        public Task<string> RequestGamerUuidAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            RequestGamerUuid(new StringListener(tcs));
            return tcs.Task;
        }

        [Obsolete("Obsoleted by RequestProductListAsync")]
        public Task<IList<Product>> RequestProductList(IList<Purchasable> purchasables)
        {
            return RequestProductListAsync(purchasables);
        }

        public Task<IList<Product>> RequestProductListAsync(IList<Purchasable> purchasables)
        {
            var tcs = new TaskCompletionSource<IList<Product>>();
            RequestProductList(purchasables, new ProductListListener(tcs));
            return tcs.Task;
        }

        [Obsolete("Obsoleted by RequestPurchaseAsync")]
        public Task<bool> RequestPurchase(Product product, string uniquePurchaseId)
        {
            return RequestPurchaseAsync(product, uniquePurchaseId);
        }

        public Task<bool> RequestPurchaseAsync(Product product, string uniquePurchaseId)
        {
            var tcs = new TaskCompletionSource<bool>();
            var purchasable = PurchaseUtils.CreatePurchasable(product, uniquePurchaseId);
            RequestPurchase(purchasable, new PurchaseListener(tcs, PurchaseUtils, product, uniquePurchaseId));
            return tcs.Task;
        }

        [Obsolete("Obsoleted by RequestReceiptsAsync")]
        public Task<IList<Receipt>> RequestReceipts()
        {
            return RequestReceiptsAsync();
        }

        public Task<IList<Receipt>> RequestReceiptsAsync()
        {
            var tcs = new TaskCompletionSource<IList<Receipt>>();
            RequestReceipts(new ReceiptsListener(tcs, PurchaseUtils));
            return tcs.Task;
        }
    }
}