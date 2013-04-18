using System.Threading.Tasks;
using Android.Runtime;
using System.Collections.Generic;
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

        public Task<string> RequestGamerUuid()
        {
            var tcs = new TaskCompletionSource<string>();
            RequestGamerUuid(new StringListener(tcs));
            return tcs.Task;
        }

        public Task<IList<Product>> RequestProductList(IList<Purchasable> purchasables)
        {
            var tcs = new TaskCompletionSource<IList<Product>>();
            RequestProductList(purchasables, new ProductListListener(tcs));
            return tcs.Task;
        }

        public Task<bool> RequestPurchase(Product product, string uniquePurchaseId)
        {
            var tcs = new TaskCompletionSource<bool>();
            var purchasable = PurchaseUtils.CreatePurchasable(product, uniquePurchaseId);
            RequestPurchase(purchasable, new PurchaseListener(tcs, PurchaseUtils, product, uniquePurchaseId));
            return tcs.Task;
        }

        public Task<IList<Receipt>> RequestReceipts()
        {
            var tcs = new TaskCompletionSource<IList<Receipt>>();
            RequestReceipts(new ReceiptsListener(tcs, PurchaseUtils));
            return tcs.Task;
        }
    }
}