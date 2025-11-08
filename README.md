[![Build solution](https://github.com/panuoksala/streamdeck-fabric-plugin/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/panuoksala/streamdeck-fabric-plugin/actions/workflows/dotnet-build.yml)

# Stream Deck Microsoft Fabric plugin

Source code of Microsoft Fabric plugin by Panu Oksala for [Elgato Stream Deck][Stream Deck].
Project is created by using the [Stream Deck C# Toolkit][Stream Deck C# Toolkit Homepage].
Use GitHub issues to submit any bugs / feature requests.

The plugin is now available at [Elgato marketplace](https://marketplace.elgato.com/product/fabric-deck-42f2b301-2354-4a97-ac4a-7c1a77ee1d27)

## How to use this plugin

Install the plugin from Stream Deck Store. Use Interactive login method (ie. browser) or Entra service principal to authenticate againsts Fabric.

### Configurations

| Setting                   | Description|
|---------------------------|------------|
| Title                     |Overlay text on top of the Microsoft Fabric icon.|
| Tenant Id                 |Fabric tenant identifier (GUID).|
| Workspace Id              |The workspace identifier. After *groups* word in Fabric URL.|
| Fabric Resource Id        |Notebook or Data pipeline identifier (copy from URL).|
| Login Method              |Interactive to use your own credentials, or service principal for app registration.|
| Status update frequency   |How often device will update the display after action invocation. Set longer if you are getting incorrect information.|

After successfull action run the Stream Deck button will show OK sign for a short while.
If Stream Deck shows red question icon on top right corner of the button, check logs from plugin folder for more details.

## Contribution guide

1. Install Stream Deck application
2. Clone the repository
3. Build with Visual Studio
4. Visual Studio should automatically add Microsoft Fabric button into your Stream Deck app / device.
5. To debug the app just run it and attach debugger into StreamDeck application (you might have two so try both).
6. If you experience problems try to run the Visual Studio in Administrator mode.
7. Build/Rebuild will stop StreamDeck app and relaunch it automatically with updated plugin.

If you have problems to build plugin after debugging, quick fix is to restart Visual Studio.


## References

* [Stream Deck C# Toolkit Homepage](https://github.com/FritzAndFriends/StreamDeckToolkit)
* [Stream Deck Page][Stream Deck]
* [Stream Deck SDK Documentation][Stream Deck SDK]

<!-- References -->
[Stream Deck]: https://www.elgato.com/en/gaming/stream-deck "Elgato's Stream Deck landing page for the hardware, software, and SDK"
[Stream Deck C# Toolkit Homepage]: https://github.com/FritzAndFriends/StreamDeckToolkit "C# Stream Deck library"
[Stream Deck software]: https://www.elgato.com/gaming/downloads "Download the Stream Deck software"
[Stream Deck SDK]: https://developer.elgato.com/documentation/stream-deck "Elgato's online SDK documentation"
[Style Guide]: https://developer.elgato.com/documentation/stream-deck/sdk/style-guide/ "The Stream Deck SDK Style Guide"
[Manifest file]: https://developer.elgato.com/documentation/stream-deck/sdk/manifest "Definition of elements in the manifest.json file"
