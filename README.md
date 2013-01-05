ouya-csharp
===========

C# bindings for the OUYA Development Kit

To get your Mono for Android application showing up in the OUYA Launcher, you will need to:
- set the project to use API Level 16
- add the Launcher image (732x412 for games, 412x412 for apps) in Resources/Drawable-xhdpi/ouya_icon.png
- add the following IntentFilter attribute to your Activity class

    [IntentFilter(new[] { Intent.ActionMain }
        , Categories = new[] { Intent.CategoryLauncher, "ouya.intent.category.GAME" })]

  Change GAME to APP for an app.

Deploy the app.  Now it will show up in the OUYA Launcher.