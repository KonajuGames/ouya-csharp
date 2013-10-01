ouya-csharp
===========

C# bindings for the OUYA Development Kit 1.0.8

To get your Mono for Android game showing up in the OUYA Launcher, you will need to:
- set the project to use API Level 16
- add the Launcher image (732x412 pixels) in Resources/Drawable-xhdpi/ouya_icon.png
- add the following IntentFilter attribute to your Activity class

    [IntentFilter(new[] { Intent.ActionMain }
        , Categories = new[] { Intent.CategoryLauncher, OuyaIntent.CategoryGame })]
        
- add your application key `key.der` file to your project under Resources/Raw.

Deploy the game to the device.  Now it will show up in the OUYA Launcher.


Disclaimer: This project is a community-supported extension to the OUYA Development Kit and is in no way connected to OUYA, Inc. The OUYA name is owned by OUYA, Inc.


Prerequisites
-------------

The library uses the new async/await keywords that were introduced in Xamarin.Android 4.8.  In order to use these keywords in Visual Studio 2010, you will need the [Async CTP version 3.0](http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=9983) installed.


Examples
--------

Using the new async/await style APIs is very simple.  This example retrieves the products from the Store, initiates a purchase for the first item and if successful, retrieves the receipts.

```cs
    products = await facade.RequestProductListAsync("__TEST__01", "__TEST__02");
    var purchaseResult = await facade.RequestPurchaseAsync(products[0]);
    if (purchaseResult)
        receipts = await facade.RequestReceiptsAsync();
```

Error handling for this example simply involves wrapping it in a `try..catch` block.


Features
--------

The gamer UUID and the receipts are now cached on the device.  This allows the gamer UUID and the receipts to be retrieved even if there is no network connection.  This requires no code support from the game as this is all done entirely within the OUYA C# library.  The receipts are stored in an encrypted file to minimize potential for modification or falsification.
