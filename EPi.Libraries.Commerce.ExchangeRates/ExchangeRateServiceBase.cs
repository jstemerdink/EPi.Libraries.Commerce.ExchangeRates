// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateServiceBase.cs" company="Jeroen Stemerdink">
//      Copyright © 2019 Jeroen Stemerdink.
//      Permission is hereby granted, free of charge, to any person obtaining a copy
//      of this software and associated documentation files (the "Software"), to deal
//      in the Software without restriction, including without limitation the rights
//      to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//      copies of the Software, and to permit persons to whom the Software is
//      furnished to do so, subject to the following conditions:
//
//      The above copyright notice and this permission notice shall be included in all
//      copies or substantial portions of the Software.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//      SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EPi.Libraries.Commerce.ExchangeRates
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;

    using EPiServer.Logging;

    using Mediachase.Commerce;
    using Mediachase.Commerce.Markets;

    using Microsoft.Extensions.Configuration;

    /// <summary>
    ///     Class ExchangeRateServiceBase.
    /// </summary>
    public abstract class ExchangeRateServiceBase : IExchangeRateService
    {
        /// <summary>
        /// A Http Client Instance
        /// </summary>
        /// <remarks>HttpClient is intended to be instantiated once per application, rather than per-use. But as we don't know if there's one we need to create one</remarks>
        protected static readonly HttpClient Client = new HttpClient();

        /// <summary>
        ///     The <see cref="IMarketService"/> instance.
        /// </summary>
        protected IMarketService MarketService;

        /// <summary>
        ///     The <see cref="ILogger"/> instance.
        /// </summary>
        protected ILogger Log = LogManager.GetLogger();
        
        /// <summary>
        /// The configuration
        /// </summary>
        protected IConfiguration Configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExchangeRateServiceBase" /> class.
        /// </summary>
        /// <param name="marketService">The market service.</param>
        /// <param name="configuration">The configuration.</param>
        protected ExchangeRateServiceBase(IMarketService marketService, IConfiguration configuration)
        {
            this.MarketService = marketService;
            this.Configuration = configuration;
        }

        /// <summary>
        ///     Gets the exchange rates.
        /// </summary>
        /// /// <param name="messages">A <see cref="List{T}"/> of messages.</param>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="CurrencyConversion"/>.</returns>
        public abstract ReadOnlyCollection<CurrencyConversion> GetExchangeRates(out List<string> messages);

        /// <summary>Gets the available currency codes.</summary>
        /// <returns>A <see cref="List{T}"/> of currency codes.</returns>
        protected ReadOnlyCollection<string> GetAvailableCurrencies()
        {
            List<IMarket> markets = this.MarketService.GetAllMarkets().ToList();
            List<string> usedCurrencies = new List<string>();

            foreach (IMarket market in markets)
            {
                usedCurrencies.AddRange(market.Currencies.Select(marketCurrency => marketCurrency.CurrencyCode));
            }

            return new ReadOnlyCollection<string>(usedCurrencies.Distinct().ToList());
        }

        /// <summary>
        /// Convert the Unix time stamp to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="unixTimeStamp">The unix time stamp.</param>
        /// <returns>A <see cref="DateTime"/>.</returns>
        protected DateTime UnixTimeStampToDateTime(float unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            try
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, kind: DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(value: unixTimeStamp).ToLocalTime();
                return dtDateTime;
            }
            catch (Exception exception)
            {
                this.Log.Debug("[Exchange Rates : Service] Error getting timestamp", exception);
            }

            return DateTime.Now.ToLocalTime();
        }
    }
}