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
    using System.Net;
    using System.Reflection;

    using EPiServer.Logging;
    using EPiServer.ServiceLocation;

    using Newtonsoft.Json;

    /// <summary>
    ///     Class FixerExchangeRateService.
    /// </summary>
    [ServiceConfiguration(typeof(IExchangeRateService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ExchangeRateService : ExchangeRateServiceBase
    {
        private const string FailMessage = "[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer";

        private const string KeyMissingMessage = "[Exchange Rates : CurrencyLayer] Access key not configured";

        /// <summary>
        /// Gets the exchange rates.
        /// </summary>
        /// <param name="messages">A <see cref="List{T}"/> of messages.</param>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="CurrencyConversion"/>.</returns>
        public override ReadOnlyCollection<CurrencyConversion> GetExchangeRates(out List<string> messages)
        {
            messages = new List<string>();
            List<CurrencyConversion> currencyConversions = new List<CurrencyConversion>();

            try
            {
                CurrencyLayerResponse currencyLayerResponse = this.GetCurrencyLayerResponse();

                if (!currencyLayerResponse.Success)
                {
                    string failMessage = string.Format(
                        provider: CultureInfo.InvariantCulture,
                        format: "[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer: '{0}'",
                        arg0: currencyLayerResponse.Error.Info);

                    messages.Add(item: failMessage);
                    this.log.Error(message: failMessage);
                    return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
                }

                DateTime exchangeRateDate = UnixTimeStampToDateTime(unixTimeStamp: currencyLayerResponse.Timestamp);

                currencyConversions.Add(
                    new CurrencyConversion(
                        currency: currencyLayerResponse.BaseCurrency,
                        name: this.GetCurrencyName(isoCurrencySymbol: currencyLayerResponse.BaseCurrency),
                        factor: 1m,
                        updated: exchangeRateDate));

                foreach (PropertyInfo propertyInfo in typeof(Quotes).GetProperties())
                {
                    string currencyCode = propertyInfo.Name.Substring(3);
                    string currencyName = this.GetCurrencyName(isoCurrencySymbol: currencyCode);
                    float exchangeRate = (float)currencyLayerResponse.Quotes.GetType().GetProperty(name: propertyInfo.Name).GetValue(obj: currencyLayerResponse.Quotes, index: null);

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
                messages.Add(item: FailMessage);
                this.log.Error(message: FailMessage, exception: exception);
            }

            return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
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

            string accessKey = ConfigurationManager.AppSettings["exchangerates.currencylayer.accesskey"];

            if (string.IsNullOrWhiteSpace(value: accessKey))
            {
                this.log.Error(message: KeyMissingMessage);
            }

            try
            {
                string requestUrl = string.Format(
                    provider: CultureInfo.InvariantCulture,
                    format: "http://www.apilayer.net/api/live?access_key={0}&source=USD",
                    arg0: accessKey);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString: requestUrl);
                request.Method = WebRequestMethods.Http.Get;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                if (responseStream == null)
                {
                    this.log.Error(message: FailMessage);
                    return JsonConvert.DeserializeObject<CurrencyLayerResponse>(value: jsonResponse);
                }

                using (StreamReader streamReader = new StreamReader(stream: responseStream))
                {
                    jsonResponse = streamReader.ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                this.log.Error(message: FailMessage, exception: exception);
                this.log.Debug("[Exchange Rates : CurrencyLayer] JSON response: {0}", jsonResponse);
            }

            return JsonConvert.DeserializeObject<CurrencyLayerResponse>(value: jsonResponse);
        }
    }
}