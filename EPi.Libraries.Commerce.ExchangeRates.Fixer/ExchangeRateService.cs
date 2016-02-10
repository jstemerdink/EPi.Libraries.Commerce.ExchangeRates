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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

using EPiServer.Logging;
using EPiServer.ServiceLocation;

using Newtonsoft.Json;

namespace EPi.Libraries.Commerce.ExchangeRates.Fixer
{
    /// <summary>
    ///     Class ExchangeRateService.
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
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://api.fixer.io/latest?base=USD");
                request.ContentType = "application/json; charset=utf-8";
                request.Method = WebRequestMethods.Http.Get;
                request.Accept = "application/json";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    jsonResponse = streamReader.ReadToEnd();
                }

                FixerResponse fixerResponse = JsonConvert.DeserializeObject<FixerResponse>(jsonResponse);

                currencyConversions.Add(
                    new CurrencyConversion(
                        fixerResponse.BaseCurrency,
                        this.GetCurrencyName(fixerResponse.BaseCurrency),
                        1m,
                        DateTime.ParseExact(fixerResponse.ImportDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)));

                foreach (PropertyInfo propertyInfo in typeof(Rates).GetProperties())
                {
                    string currencyName = this.GetCurrencyName(propertyInfo.Name);
                    float exchangeRate =
                        (float)
                        fixerResponse.ExchangeRates.GetType()
                            .GetProperty(propertyInfo.Name)
                            .GetValue(fixerResponse.ExchangeRates, null);

                    CurrencyConversion currencyConversion = new CurrencyConversion(
                        propertyInfo.Name,
                        currencyName,
                        Convert.ToDecimal(exchangeRate, CultureInfo.CreateSpecificCulture("en-US")),
                        DateTime.ParseExact(fixerResponse.ImportDate, "yyyy-MM-dd", CultureInfo.InvariantCulture));

                    currencyConversions.Add(currencyConversion);
                }
            }
            catch (Exception exception)
            {
                string failMessage = "[Exchange Rates : Fixer] Error retrieving exchange rates from fixer.io";
                messages.Add(failMessage);
                this.Log.Error(failMessage, exception);
                this.Log.Information("[Exchange Rates : CurrencyLayer] JSON response: {0}", jsonResponse);
            }

            return new ReadOnlyCollection<CurrencyConversion>(currencyConversions);
        }

    }
}
