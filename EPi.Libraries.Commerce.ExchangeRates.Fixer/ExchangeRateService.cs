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

namespace EPi.Libraries.Commerce.ExchangeRates.Fixer
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
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;

    using EPiServer.Logging;
    using EPiServer.ServiceLocation;

    using Mediachase.Commerce.Markets;

    using Microsoft.Extensions.Configuration;

    using Newtonsoft.Json;
    using static System.Net.Mime.MediaTypeNames;

    using ConfigurationManager = System.Configuration.ConfigurationManager;

    /// <summary>
    ///     Class ExchangeRateService.
    /// </summary>
    [ServiceConfiguration(typeof(IExchangeRateService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ExchangeRateService : ExchangeRateServiceBase
    {
        private const string FailMessage = "[Exchange Rates : Fixer] Error retrieving exchange rates from fixer.io";

        private const string ConvertMessage = "[Exchange Rates : Fixer] Error converting exchange rate from fixer.io";

        private const string KeyMissingMessage = "[Exchange Rates : Fixer] Access key not configured";

        private const string UrlMissingMessage = "[Exchange Rates : Fixer] Api Url not configured";

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
        ///     Gets the exchange rates.
        /// </summary>
        /// /// <param name="messages">A <see cref="List{T}"/> of messages.</param>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="CurrencyConversion"/>.</returns>
        public override ReadOnlyCollection<CurrencyConversion> GetExchangeRates(out List<string> messages)
        {
            messages = new List<string>();

            List<CurrencyConversion> currencyConversions = new List<CurrencyConversion>();

            FixerResponse fixerResponse = this.GetFixerResponse();
            DateTime exchangeRateDate = UnixTimeStampToDateTime(unixTimeStamp: fixerResponse.Timestamp);

            try
            {
                currencyConversions.Add(
                    new CurrencyConversion(
                        currency: fixerResponse.BaseCurrency, 
                        name: fixerResponse.BaseCurrency, 
                        factor: 1m, 
                        updated: exchangeRateDate));
            }
            catch (Exception exception)
            {
                messages.Add(item: FailMessage);
                this.Log.Error(message: FailMessage, exception: exception);
                return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
            }

            Type ratesType = fixerResponse.ExchangeRates.GetType();

            foreach (PropertyInfo propertyInfo in typeof(Rates).GetProperties())
            {
                try
                {
                    string currencyCode = propertyInfo.Name;
                    string currencyName = propertyInfo.Name;
                    float exchangeRate = 0;

                    if (string.IsNullOrWhiteSpace(currencyCode))
                    {
                        continue;
                    }

                    PropertyInfo property = ratesType?.GetProperty(name: currencyCode);
                    object propertyValue = property?.GetValue(obj: fixerResponse.ExchangeRates, index: null);

                    if (propertyValue == null)
                    {
                        continue;
                    }

                    exchangeRate = (float)propertyValue;
                    

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
                catch (Exception exception)
                {
                    this.Log.Error(message: ConvertMessage, exception: exception);
                }
            }

            return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions.Distinct(new CurrencyConversionEqualityComparer()).ToList());
        }

        /// <summary>
        /// Gets the response from fixer.io.
        /// </summary>
        /// <returns>A <see cref="FixerResponse"/>.</returns>
        private FixerResponse GetFixerResponse()
        {
            string jsonResponse = string.Empty;

            string accessKey = this.Configuration.GetValue<string>("ExchangeRates:Services:AccessKey");
            string apiUrl = this.Configuration.GetValue<string>("ExchangeRates:Services:ApiUrl");

            if (string.IsNullOrWhiteSpace(accessKey))
            {
                accessKey = this.Configuration.GetValue<string>("exchangerates.fixer.accesskey");
            }

            if (string.IsNullOrWhiteSpace(value: accessKey))
            {
                throw new ConfigurationErrorsException(KeyMissingMessage);
            }

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                apiUrl = this.Configuration.GetValue<string>("exchangerates.fixer.apiurl");
            }

            if (string.IsNullOrWhiteSpace(value: apiUrl))
            {
                throw new ConfigurationErrorsException(UrlMissingMessage);
            }

            try
            {
                string requestUrl = $"{apiUrl}latest?access_key={accessKey}&symbols={string.Join(",", this.GetAvailableCurrencies())}";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                HttpResponseMessage response = Client.SendAsync(request).Result;

                response.EnsureSuccessStatusCode();

                string responseBody = response.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    this.Log.Error(message: FailMessage);
                    return JsonConvert.DeserializeObject<FixerResponse>(value: jsonResponse);
                }

                jsonResponse = responseBody;
            }
            catch (Exception exception)
            {
                this.Log.Error(message: FailMessage, exception: exception);
                this.Log.Debug("[Exchange Rates : Fixer] JSON response: {0}", jsonResponse);
            }

            return JsonConvert.DeserializeObject<FixerResponse>(value: jsonResponse);
        }
    }
}