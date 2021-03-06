﻿// --------------------------------------------------------------------------------------------------------------------
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
    using System.Reflection;

    using EPiServer.Logging;
    using EPiServer.ServiceLocation;

    using Mediachase.Commerce.Markets;

    using Newtonsoft.Json;

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
        /// Initializes a new instance of the <see cref="ExchangeRateService"/> class.
        /// </summary>
        /// <param name="marketService">The market service.</param>
        public ExchangeRateService(IMarketService marketService)
            : base(marketService: marketService)
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

            try
            {
                currencyConversions.Add(new CurrencyConversion(currency: fixerResponse.BaseCurrency, name: this.GetCurrencyName(isoCurrencySymbol: fixerResponse.BaseCurrency), factor: 1m, updated: DateTime.ParseExact(s: fixerResponse.ImportDate, format: "yyyy-MM-dd", provider: CultureInfo.InvariantCulture)));
            }
            catch (Exception exception)
            {
                messages.Add(item: FailMessage);
                this.log.Error(message: FailMessage, exception: exception);
                return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
            }

            foreach (PropertyInfo propertyInfo in typeof(Rates).GetProperties())
            {
                try
                {
                    string currencyCode = propertyInfo.Name;
                    string currencyName = this.GetCurrencyName(isoCurrencySymbol: propertyInfo.Name);
                    float exchangeRate = (float)fixerResponse.ExchangeRates.GetType().GetProperty(name: propertyInfo.Name).GetValue(obj: fixerResponse.ExchangeRates, index: null);

                    if (exchangeRate.Equals(0))
                    {
                        continue;
                    }

                    CurrencyConversion currencyConversion = new CurrencyConversion(
                        currency: currencyCode,
                        name: currencyName,
                        factor: Convert.ToDecimal(value: exchangeRate, provider: CultureInfo.CreateSpecificCulture("en-US")),
                        updated: DateTime.ParseExact(s: fixerResponse.ImportDate, format: "yyyy-MM-dd", provider: CultureInfo.InvariantCulture));

                    currencyConversions.Add(item: currencyConversion);
                }
                catch (Exception exception)
                {
                    this.log.Error(message: ConvertMessage, exception: exception);
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

            string accessKey = ConfigurationManager.AppSettings["exchangerates.fixer.accesskey"];
            string apiUrl = ConfigurationManager.AppSettings["exchangerates.fixer.apiurl"];

            if (string.IsNullOrWhiteSpace(value: accessKey))
            {
                this.log.Error(message: KeyMissingMessage);
            }

            if (string.IsNullOrWhiteSpace(value: apiUrl))
            {
                this.log.Error(message: UrlMissingMessage);
            }

            try
            {
                string requestUrl = $"{apiUrl}latest?access_key={accessKey}&symbols={string.Join(",", this.GetAvailableCurrencies())}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = WebRequestMethods.Http.Get;
                request.Accept = "application/json";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                if (responseStream == null)
                {
                    this.log.Error(message: FailMessage);
                    return JsonConvert.DeserializeObject<FixerResponse>(value: jsonResponse);
                }

                using (StreamReader streamReader = new StreamReader(stream: responseStream))
                {
                    jsonResponse = streamReader.ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                this.log.Error(message: FailMessage, exception: exception);
                this.log.Debug("[Exchange Rates : Fixer] JSON response: {0}", jsonResponse);
            }

            return JsonConvert.DeserializeObject<FixerResponse>(value: jsonResponse);
        }
    }
}