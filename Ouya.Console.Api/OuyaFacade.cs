using System.Threading.Tasks;
using Android.Runtime;
using System.Collections.Generic;
using Ouya.Csharp.Purchaseutils;

namespace Ouya.Console.Api
{
    public partial class OuyaFacade
    {
        public PurchaseUtils PurchaseUtils { get; private set; }

        public void Init(Android.Content.Context context, string developerUuid, byte[] applicationKey, bool setTestMode)
        {
            Init(context, developerUuid);
            PurchaseUtils = new PurchaseUtils(applicationKey, setTestMode);
            if (setTestMode)
            {
                SetTestMode();
            }
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

        // TODO: Figure out why this function does not work. PurchaseUtils.CreatePurchasable
        // ALWAYS seems to crash in the JNI method call unless it's called right at the start 
        // of the application, eg in Activity.OnCreate(). Not sure why. Instead use 
        // OuyaFacade.PurchaseUtils to create the purchasables up-front and later use the
        // built in RequestPurchase for the actual purchase.
        //public Task<bool> RequestPurchase(string productId, string uniquePurchaseId)
        //{
        //    var tcs = new TaskCompletionSource<bool>();
        //    var purchasable = PurchaseUtils.CreatePurchasable(productId, uniquePurchaseId);
        //    RequestPurchase(purchasable, new PurchaseListener(tcs, PurchaseUtils, productId, uniquePurchaseId));
        //    return tcs.Task;
        //}

        public Task<IList<Receipt>> RequestReceipts()
        {
            var tcs = new TaskCompletionSource<IList<Receipt>>();
            RequestReceipts(new ReceiptsListener(tcs, PurchaseUtils));
            return tcs.Task;
        }
    }
}