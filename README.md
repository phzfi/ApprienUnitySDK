# ApprienUnitySDK
Apprien Unity SDK is a lightweight client for Apprien Automatic Pricing API for mobile game companies using Unity3D. 
Apprien increases your In-App Purchases revenue by 20-40% by optimizing the prices by country, time of the day and customer segment.

It typically takes roughly 2-4h to integrate Apprien Unity SDK with your game.

The minimal compiled footprint of ApprienUnitySDK is roughly 2KB.

Apprien.cs is a standalone C# class library that uses Apprien Game API. Other files are for Unity Editor integration and unit tests. An example store without purchasing logic is available in ExampleStoreUIController.cs

## Features

In case of any network or backend failure, ApprienUnitySDK will always revert to the base IAP ID and show the fixed prices.

## Pre-requisites

You need to obtain an OAuth2 Access Token from Apprien. You also need to setup app store integrations by providing Apprien the credentials to access these platforms.

Currently Apprien supports the following platforms
* Google Play Store
* Apple App Store (WIP)

Please contact sales@apprien.com to get the integration instructions for different stores.

Apprien provides your Quality Assurance department generic Apprien Game Testing Documentation and Test Cases on how to
detect typical issues with your game and Apprien integration. By incorporating the
Test Cases to your game's testing plan, you can ensure that Apprien integration works smoothly for your players.

## Setup

__1) Acquire the authentication token__ as described above in the Pre-requisites section

__2) Open your project__ in a supported Unity version (see [Compatibility Notes](#compatibility-notes) below)

__3) Import Apprien__ 
  1) You can either use our prepared `.unityPackage` archives containing everything required __(recommended)__, or
  2) Copy `Assets/Apprien/Scripts/Apprien.cs` to your project.

__4) Add integration to your Store Manager__

You need to integrate ApprienUnitySDK to your Store Manager. The overview is that you need to first fetch the Apprien price variants
for each base IAP id, then display the variant prices (SKUs). If the player purchases the variant, you need to deliver the goods
by converting the variant name back to the base product name.

Typically the IAP product names (SKUs) have been  hard coded somewhere in your game (store manager). When normally you would show 
the base SKU name to your player, with Apprien you would call FetchApprienPrice() for every base SKU -name. This function calls 
the Apprien Game API, and returns a product variant by name 'z_base_sku_name.apprien_500_dfa3' for 'base_sku_name'. The actual 
prices are stored and fetched then from Google and Apple by the Unity Store Manager.

The variants are copies of the base_sku_name with exactly the same descriptions and other information, except the price, which 
varies from variant to variant and country to country.

The player should be delivered the same amount of 'goods' (e.g. gems, gold) for the variants than for the base_sku_name. You can
achieve this by passing the purchased variant sjy name through GetBaseSku() -function that converts it back to the base_sku_name
for the delivery of goods.

You should call the FetchApprienPrices() on game initialization, and it's a good idea not to call it again during the session, 
unless you want faster price update cycles. In priciple the pricing will change after every purchase, or by every 15 mins (on
Apprien API).

__5) Receipts (Optional, but recommended)__

Apprien requires the data of the purchased transactions to perform the analysis. We can obtain the transaction history also from 
Google Play and Apple iTunes, but it takes 24h or more to get the data. A faster way is to send the data straight away to Apprien
from the client. This enables the pricing to be updated in real time (or by every 15 mins). 

Also, if you are using a Store (e.g. Chinese) where Apprien doesn't yet have backend-to-backend integration, you can use client side
integration to enable dynamic pricing.

__6) Fraud Management backend (Optional)__

A few gaming companies are using fraud management backends to verify real purchases from fraudulent ones (sent by hackers). Often the
fraud management backends are written in various programming languages such as C#, Java, Node.js, Go, Python or PHP. 

The problem is that the fraud management backend typically refuses the purchases of Apprien variant SKUs because their names don't match 
to the expected ones. However, you can overcome this issue by passing the SKU name through the GetBaseSku() -function, which returns the
base SKU -name. For example if the customer purchased a variant by name "z_base_sku_name.apprien_500_dfa3", the GetBaseSku() -function
returns the expected "base_sku_name".

While we are working to implement adaptations for all commonly used programming languages, you can convert the GetBaseSku() function from
Apprien.cs (bottom) to your own language, since it works by using simple string manipulation available for all languages.

__7) Testing__

Please test the integration by following the generic Apprien game test cases.

## Compatibility Notes

The SDK sends a web request to fetch a variant IAP ID that has the current optimal price set in the supported Stores. The SDK uses UnityWebRequest for maximal platform support, and thus Unity 4.x and below are not currently supported in the SDK. Unity 4.x users can still access Apprien Game API for optimal prices, but the SDK does not support it (yet).

Supported Unity versions:
* Unity 5.x
* Unity 2017.x
* Unity 2018.x

The SDK supports the official `UnityEngine.Purchasing` module for defining products. See the [SDK documentation](#sdk-documentation) below for using the SDK with your product model.

## SDK documentation



## Tests

The included unit tests can be ran using Unity Test Runner. If your project is configured to use .NET 4.x, additional unit tests are included with the Mock4Net library to mock the Apprien Game API.

## Troubleshooting
    Apprien prices won't show up in game. Only default fixed prices are visible.

See Apprien Unity SDK Test Plan to check for typical errors.

## Links

See https://www.apprien.com for more information

See https://game.apprien.com for Game API documentation

## Contributions by

Special thanks to

Daniel Liljeqvist @ Nitro Games

Jari Pauna @ Tunnelground

Kristian Lauttamus @ Apprien

Jaakko Holopainen @ PHZ Game Studios

Henri Niva @ Apprien

## Support
Please contact support@apprien.com or open a ticket on https://support.phz.fi/

## License
Apprien is a SaaS service with a separate end user agreement. However, Apprien Unity SDK (this project) is open source,
see LICENSE for more information and be free to contribute!

Trademark® Apprien
 
Copyright© Apprien Ltd 2015-2018

