using System.Threading.Tasks;
using Android.Runtime;
using System.Collections.Generic;

namespace Ouya.Console.Api
{
    public partial class OuyaFacade
    {
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
        public Task<string> RequestPurchase(Purchasable purchasable)
        {
            var tcs = new TaskCompletionSource<string>();
            RequestPurchase(purchasable, new StringListener(tcs));
            return tcs.Task;
        }

        public Task<string> RequestReceipts()
        {
            var tcs = new TaskCompletionSource<string>();
            RequestReceipts(new StringListener(tcs));
            return tcs.Task;
        }
    }
}