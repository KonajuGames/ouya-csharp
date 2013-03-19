using System.Threading.Tasks;
using Android.App;
using Android.OS;

namespace Ouya.Console.Api
{
    public partial class OuyaAuthenticationHelper
    {
        public Task HandleError(Activity activity, int errorCode, string errorMessage, Bundle bundle, int authActivityId, out bool handled)
        {
            var tcs = new TaskCompletionSource<bool>();
            handled = HandleError(activity, errorCode, errorMessage, bundle, authActivityId, new VoidListener(tcs));
            return tcs.Task;
        }
    }
}