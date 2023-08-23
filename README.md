# BTC_CryptoExchange_BestOffer

Description:

An application that parses JSON-structured file into each crypto-exchanger’s order books. Order book consists of a bunch of bids/asks, where the user can possibly buy or sell a certain amount of BTC.
Also, for each exchanger, there’s a unique user’s balance for both BTC and EUR.

Whenever the user wants to buy a certain amount of BTC, the algorithm first analyzes every exchanger’s order book to form a list of the most profitable deals. 
In addition, this list takes into account balance limits and will form trades in such a way that they do not go beyond the limits of the balance.

After the list of best orders for each crypto-exchanger’s order book was formed, the program will compare all lists and choose the most profitable combination of transactions among all possible exchangers,
thus creating an offer that will bring the highest possible profit in the case of a sale, and in the case of a purchase - will select the cheapest possible option.


Instruction:

CryptoExchangeApp - main calculation logic
CryptoExchangeConsoleApp - console-mode project for working with CryptoExchangeApp using Console (Can be Started in IDE)
CryptoExchangeWebApi - ASP.NET Core Web API project, for working with CryptoExchangeApp using endpoint in Swagger (Can be Started in IDE)
CryptoExchangeApp.Tests - xUnit-based project that tests accuracy of file parsing and correct sorting and selection of the best offer.



