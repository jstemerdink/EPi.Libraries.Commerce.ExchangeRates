// Copyright © 2015 Jeroen Stemerdink. 
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

using Newtonsoft.Json;

namespace EPi.Libraries.Commerce.ExchangeRates.CurrencyLayer
{
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
        public override ReadOnlyCollection<CurrencyConversion> GetExchangeRates()
        {
            List<CurrencyConversion> currencyConversions = new List<CurrencyConversion>();
            string jsonResponse = string.Empty;

            try
            {
                string accessKey = ConfigurationManager.AppSettings["exchangerates.currencylayer.accesskey"];

                if (string.IsNullOrWhiteSpace(accessKey))
                {
                    this.Log.Information("[Exchange Rates : CurrencyLayer] Access key not configured");
                    return new ReadOnlyCollection<CurrencyConversion>(currencyConversions);
                }

                string requestUrl = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://www.apilayer.net/api/live?access_key={0}&source=USD",
                    accessKey);


                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
                //request.ContentType = "application/json; charset=utf-8";
                request.Method = WebRequestMethods.Http.Get;
                //request.Accept = "application/json";

                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    jsonResponse = streamReader.ReadToEnd();
                }

                CurrencyLayerResponse currencyLayerResponse = JsonConvert.DeserializeObject<CurrencyLayerResponse>(jsonResponse);

                if (!currencyLayerResponse.Success)
                {
                    this.Log.Information("[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer on url: {0}: '{1}'", requestUrl, currencyLayerResponse.Error.Info);
                }
                
                DateTime exchangeRateDate = UnixTimeStampToDateTime(currencyLayerResponse.Timestamp);

                currencyConversions.Add(
                    new CurrencyConversion(
                        currencyLayerResponse.BaseCurrency, this.GetCurrencyName(currencyLayerResponse.BaseCurrency),
                        1m, exchangeRateDate));
                
                foreach (PropertyInfo propertyInfo in typeof(Quotes).GetProperties())
                {
                    string currencyCode = propertyInfo.Name.Substring(3);
                    string currencyName = this.GetCurrencyName(currencyCode);
                    float exchangeRate =
                        (float)currencyLayerResponse.Quotes.GetType()
                            .GetProperty(propertyInfo.Name)
                            .GetValue(currencyLayerResponse.Quotes, null);

                    CurrencyConversion currencyConversion = new CurrencyConversion(
                        currencyCode,
                        currencyName,
                        Convert.ToDecimal(exchangeRate, CultureInfo.CreateSpecificCulture("en-US")), exchangeRateDate);

                    currencyConversions.Add(currencyConversion);
                }
            }
            catch (Exception exception)
            {
                this.Log.Error("[Exchange Rates : CurrencyLayer] Error retrieving exchange rates from CurrencyLayer", exception);
                this.Log.Information("[Exchange Rates : CurrencyLayer] JSON response: {0}", jsonResponse);
            }

            return new ReadOnlyCollection<CurrencyConversion>(currencyConversions);
        }

        private static DateTime UnixTimeStampToDateTime(float unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
