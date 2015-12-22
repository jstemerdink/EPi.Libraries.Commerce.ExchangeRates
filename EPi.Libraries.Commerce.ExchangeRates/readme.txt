The service you want to use needs to be injected. You can use the Fixer provider I created in EPi.Libraries.Commerce.ExchangeRates.Fixer, 
the provider for currencylayer.com in EPi.Libraries.Commerce.ExchangeRates.CurrencyLayer or write your own for the service you would like to use. 

In that case you will need to implement IExchangeRateService and add the following attribute to your class  
[ServiceConfiguration(typeof(IExchangeRateService), Lifecycle = ServiceInstanceScope.Singleton)]

You can uses the base class ExchangeRateServiceBase for the service. It will give you some extra functionality, like getting the name for the currency.