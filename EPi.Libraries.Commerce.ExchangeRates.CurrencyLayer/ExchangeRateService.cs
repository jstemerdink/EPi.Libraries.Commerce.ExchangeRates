// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateService.cs" company="Jeroen Stemerdink">
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

namespace EPi.Libraries.Commerce.ExchangeRates.CurrencyLayer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;

    using EPiServer.Logging;
    using EPiServer.ServiceLocation;

    using Mediachase.Commerce.Markets;

    using Microsoft.Extensions.Configuration;

    using Newtonsoft.Json;

    /// <summary>
    ///     Class FixerExchangeRateService.
    /// </summary>
    [ServiceConfiguration(typeof(IExchangeRateService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ExchangeRateService : ExchangeRateServiceBase
    {
        private const string FailMessage = "[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer";

        private const string ConvertMessage = "[Exchange Rates : CurrencyLayer] Error converting exchange rate from CurrencyLayer";

        private const string KeyMissingMessage = "[Exchange Rates : CurrencyLayer] Access key not configured";

        private const string UrlMissingMessage = "[Exchange Rates : CurrencyLayer] Api Url not configured";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExchangeRateService" /> class.
        /// </summary>
        /// <param name="marketService">The market service.</param>
        /// <param name="configuration">The configuration.</param>
        public ExchangeRateService(IMarketService marketService, IConfiguration configuration)
            : base(marketService: marketService, configuration)
        {
        }

        /// <summary>
        /// Gets the exchange rates.
        /// </summary>
        /// <param name="messages">A <see cref="List{T}"/> of messages.</param>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="CurrencyConversion"/>.</returns>
        public override ReadOnlyCollection<CurrencyConversion> GetExchangeRates(out List<string> messages)
        {
            messages = new List<string>();
            List<CurrencyConversion> currencyConversions = new List<CurrencyConversion>();

            CurrencyLayerResponse currencyLayerResponse = this.GetCurrencyLayerResponse();
            DateTime exchangeRateDate = UnixTimeStampToDateTime(unixTimeStamp: currencyLayerResponse.Timestamp);

            try
            {
                if (!currencyLayerResponse.Success)
                {
                    string failMessage = string.Format(
                        provider: CultureInfo.InvariantCulture,
                        format: "[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer: '{0}'",
                        arg0: currencyLayerResponse.Error.Info);

                    messages.Add(item: failMessage);
                    this.Log.Error(message: failMessage);
                    return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
                }

                if (currencyLayerResponse.Quotes == null)
                {
                    return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
                }

                currencyConversions.Add(
                    new CurrencyConversion(
                        currency: currencyLayerResponse.BaseCurrency,
                        name: this.GetCurrencyName(isoCurrencySymbol: currencyLayerResponse.BaseCurrency),
                        factor: 1m,
                        updated: exchangeRateDate));
            }
            catch (Exception exception)
            {
                messages.Add(item: FailMessage);
                this.Log.Error(message: FailMessage, exception: exception);
            }

            try
            {
                foreach (PropertyInfo propertyInfo in typeof(Quotes).GetProperties())
                {
                    string currencyCode = propertyInfo.Name.Substring(3);
                    string currencyName = this.GetCurrencyName(isoCurrencySymbol: currencyCode);
                    float exchangeRate = (float)currencyLayerResponse.Quotes.GetType().GetProperty(name: propertyInfo.Name).GetValue(obj: currencyLayerResponse.Quotes, index: null);

                    if (exchangeRate.Equals(0))
                    {
                        continue;
                    }

                    CurrencyConversion currencyConversion = new CurrencyConversion(
                        currency: currencyCode,
                        name: currencyName,
                        factor: Convert.ToDecimal(value: exchangeRate, provider: CultureInfo.CreateSpecificCulture("en-US")),
                        updated: exchangeRateDate);

                    currencyConversions.Add(item: currencyConversion);
                }
            }
            catch (Exception exception)
            {
                this.Log.Error(message: ConvertMessage, exception: exception);
            }

            return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions.Distinct(new CurrencyConversionEqualityComparer()).ToList());
        }

        /// <summary>
        /// Convert the Unix time stamp to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="unixTimeStamp">The unix time stamp.</param>
        /// <returns>A <see cref="DateTime"/>.</returns>
        private static DateTime UnixTimeStampToDateTime(float unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, kind: DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(value: unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Gets the currency layer response.
        /// </summary>
        /// <returns>A <see cref="CurrencyLayerResponse"/>.</returns>
        private CurrencyLayerResponse GetCurrencyLayerResponse()
        {
            string jsonResponse = string.Empty;

            string accessKey = this.Configuration.GetValue<string>("ExchangeRates:Services:AccessKey");
            string apiUrl = this.Configuration.GetValue<string>("ExchangeRates:Services:ApiUrl");

            if (string.IsNullOrWhiteSpace(accessKey))
            {
                accessKey = this.Configuration.GetValue<string>("exchangerates.currencylayer.accesskey");
            }

            if (string.IsNullOrWhiteSpace(value: accessKey))
            {
                throw new ConfigurationErrorsException(KeyMissingMessage);
            }

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                apiUrl = this.Configuration.GetValue<string>("exchangerates.currencylayer.apiurl");
            }

            if (string.IsNullOrWhiteSpace(value: apiUrl))
            {
                throw new ConfigurationErrorsException(UrlMissingMessage);
            }

            try
            {
                string requestUrl = $"{apiUrl}live?access_key={accessKey}&currencies={string.Join(",", this.GetAvailableCurrencies())}";

                HttpResponseMessage response = Client.GetAsync(requestUrl).Result;
                response.EnsureSuccessStatusCode();

                string responseBody = response.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    this.Log.Error(message: FailMessage);
                    return JsonConvert.DeserializeObject<CurrencyLayerResponse>(value: jsonResponse);
                }

                jsonResponse = responseBody;
            }
            catch (Exception exception)
            {
                this.Log.Error(message: FailMessage, exception: exception);
                this.Log.Debug("[Exchange Rates : CurrencyLayer] JSON response: {0}", jsonResponse);
            }

            return JsonConvert.DeserializeObject<CurrencyLayerResponse>(value: jsonResponse);
        }
    }
}