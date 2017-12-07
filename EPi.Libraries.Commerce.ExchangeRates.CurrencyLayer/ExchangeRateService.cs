// Copyright © 2017 Jeroen Stemerdink. 
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

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
        /// <summary>
        ///     Gets the exchange rates.
        /// </summary>
        /// <returns>List&lt;Models.CurrencyConversion&gt;.</returns>
        public override ReadOnlyCollection<CurrencyConversion> GetExchangeRates(out List<string> messages)
        {
            messages = new List<string>();
            List<CurrencyConversion> currencyConversions = new List<CurrencyConversion>();
            string jsonResponse = string.Empty;

            try
            {
                string accessKey = ConfigurationManager.AppSettings["exchangerates.currencylayer.accesskey"];

                if (string.IsNullOrWhiteSpace(value: accessKey))
                {
                    string keyMissingMessage = "[Exchange Rates : CurrencyLayer] Access key not configured";
                    messages.Add(item: keyMissingMessage);
                    this.Log.Information(message: keyMissingMessage);
                    return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
                }

                string requestUrl = string.Format(
                    provider: CultureInfo.InvariantCulture,
                    format: "http://www.apilayer.net/api/live?access_key={0}&source=USD",
                    arg0: accessKey);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString: requestUrl);
                request.Method = WebRequestMethods.Http.Get;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    jsonResponse = streamReader.ReadToEnd();
                }

                CurrencyLayerResponse currencyLayerResponse =
                    JsonConvert.DeserializeObject<CurrencyLayerResponse>(value: jsonResponse);

                if (!currencyLayerResponse.Success)
                {
                    string failMessage = string.Format(
                        provider: CultureInfo.InvariantCulture,
                        format:
                        "[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer: '{0}'",
                        arg0: currencyLayerResponse.Error.Info);

                    messages.Add(item: failMessage);
                    this.Log.Information(message: failMessage);
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
                    float exchangeRate = (float)currencyLayerResponse.Quotes.GetType()
                        .GetProperty(name: propertyInfo.Name).GetValue(obj: currencyLayerResponse.Quotes, index: null);

                    CurrencyConversion currencyConversion = new CurrencyConversion(
                        currency: currencyCode,
                        name: currencyName,
                        factor: Convert.ToDecimal(
                            value: exchangeRate,
                            provider: CultureInfo.CreateSpecificCulture("en-US")),
                        updated: exchangeRateDate);

                    currencyConversions.Add(item: currencyConversion);
                }
            }
            catch (Exception exception)
            {
                string failMessage =
                    "[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer";
                messages.Add(item: failMessage);
                this.Log.Error(message: failMessage, exception: exception);
                this.Log.Information("[Exchange Rates : CurrencyLayer] JSON response: {0}", jsonResponse);
            }

            return new ReadOnlyCollection<CurrencyConversion>(list: currencyConversions);
        }

        private static DateTime UnixTimeStampToDateTime(float unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, kind: DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(value: unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}