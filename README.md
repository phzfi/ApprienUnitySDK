# ApprienUnitySDK
Apprien Unity SDK is a lightweight client for Apprien Automatic Pricing API for mobile game companies and by using Unity3D. 
Apprien increases your In-App Purchases revenue by 20-40% by optimizing the prices by country, time of the day and customer segment.

It typically takes roughly 2-4h to integrate Apprien Unity SDK to your game.

The compiled footprint of ApprienUnitySDK is roughly 2KB.

Apprien.cs is a standalone Unity Monobehavior "library" to use Apprien Game API. You could call it rather a REST client
than a SDK, actually it's just one Plain Old C# Class (Assets/Apprien/Scripts/Apprien.cs) (PoJO). Other files are for Unity Editor integration.

# Features

In case of any failure, ApprienUnitySDK should revert always to the base SKU and show the fixed prices.

# Pre-requisites

You need to obtain an OAuth2 Access Token from Apprien. Secondly you need to setup Google and Apple integrations. 
Please contact sales@apprien.com to get the integration instructions for different stores.

Third Apprien provides your Quality Assurance -department generic Apprien Game Testing Documentation and Test Cases how to
detect typical issues with your game and Apprien integration to ensure as smooth as possible user experience. By incorporating the
Test Cases to your game's testing plan, you can ensure that Apprien integration works smoothly for your players.

# Setup

1) Open your Game Project

2) Copy and import Assets/Apprien/Scripts/Apprien.cs to your project.

3) Store Manager 

You need to integrate Apprien SDK to your Store Manager. The overview is that you need to first fetch the Apprien price variants
for each base SKU, then display the variant prices (SKUs). If the player purchases the variant, you need to deliver the goods
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

4) Receipts (Optional, but recommended)

Apprien requires the data of the purchased transactions to perform the analysis. We can obtain the transaction history also from 
Google Play and Apple iTunes, but it takes 24h or more to get the data. A faster way is to send the data straight away to Apprien
from the client. This enables the pricing to be updated in real time (or by every 15 mins). 

Also, if you are using a Store (e.g. Chinese) where Apprien doesn't yet have backend-to-backend integration, you can use client side
integration to enable dynamic pricing.

5) Fraud Management -backend (Optional)

A few gaming companies are using fraud management backends to verify real purchases from fraudulent ones (sent by hackers). Often the
fraud management backends are written in various programming languages such as C#, Java, Node.js, Go, Python or PHP. 

The problem is that the fraud management backend typically refuses the purchases of Apprien variant SKUs because their names don't match 
to the expected ones. However, you can overcome this issue by passing the SKU name through the GetBaseSku() -function, which returns the
base SKU -name. For example if the customer purchased a variant by name "z_base_sku_name.apprien_500_dfa3", the GetBaseSku() -function
returns the expected "base_sku_name".

While we are working to implement adaptations for all commonly used programming languages, you can convert the GetBaseSku() function from
Apprien.cs (bottom) to your own language, since it works by using simple string manipulation available for all languages.

6) Testing

Please test the integration by following the generic Apprien game test cases.

# Compability Notes

Unity4

    Unity4 doesn't use the same HTTP -libraries than Unity5, so you need to convert the HTTP requests to use e.g. .Net -libraries.

Unity5

    This is the version Apprien SDK was initially developed for, so it should work the best.
    
Unity2017.2/2017.3

    Works ok

Unity2018.1

    TODO, not released yet.

OpenIAB

    We managed to get it to work, TODO to add notes here.

# Coding Convention

Please use Visual Studio formating rules (autoformat).

# Tests

Please use Microsoft.VisualStudio.TestTools.UnitTesting and run the tests on Visual Studio.

# Troubleshooting

Apprien prices won't show up.

See Apprien Unity SDK Test Plan to check for typical errors.

# Links

See https://www.apprien.com for more information

See https://game.apprien.com for Game API documentation

# Contributions by

Special thanks to

Daniel Liljeqvist @ Nitro Games

Jari Pauna @ Tunnelground

Kristian Lauttamus @ Apprien

Jaakko Holopainen @ PHZ Game Studios

# Support
Please contact support@apprien.com or open a ticket on https://support.phz.fi/

# License
Apprien is a SaaS service with a separate end user agreement. However, Apprien Unity SDK (this project) is open source,
see LICENSE for more information and be free to contribute!

Trademark® Apprien
 
Copyright© Apprien Ltd 2015-2018

