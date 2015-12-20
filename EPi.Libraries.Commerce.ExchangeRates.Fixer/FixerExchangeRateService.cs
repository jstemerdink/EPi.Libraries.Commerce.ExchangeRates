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
    ///     Class FixerExchangeRateService.
    /// </summary>
    [ServiceConfiguration(typeof(IExchangeRateService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class FixerExchangeRateService : IExchangeRateService
    {
        private readonly ILogger log = LogManager.GetLogger();

        private readonly List<RegionInfo> regionsInfos;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public FixerExchangeRateService()
        {
            this.regionsInfos = this.GetRegions();
        }

        /// <summary>
        ///     Gets the exchange rates.
        /// </summary>
        /// <returns>List&lt;Models.CurrencyConversion&gt;.</returns>
        public ReadOnlyCollection<CurrencyConversion> GetExchangeRates()
        {
            List<CurrencyConversion> currencyConversions = new List<CurrencyConversion>();

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://api.fixer.io/latest?base=USD");
                request.ContentType = "application/json; charset=utf-8";
                request.Method = WebRequestMethods.Http.Get;
                request.Accept = "application/json";

                string text;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    text = streamReader.ReadToEnd();
                }

                FixerResponse fixerResponse = JsonConvert.DeserializeObject<FixerResponse>(text);

                currencyConversions.Add(
                    new CurrencyConversion(
                        fixerResponse.BaseCurrency,
                        this.GetCurrencyName(fixerResponse.BaseCurrency),
                        1m));

                foreach (PropertyInfo propertyInfo in typeof(Rates).GetProperties())
                {
                    CurrencyConversion currencyConversion = new CurrencyConversion(
                        propertyInfo.Name,
                        this.GetCurrencyName(propertyInfo.Name),
                        Convert.ToDecimal(
                            fixerResponse.ExchangeRates.GetType()
                                .GetProperty(propertyInfo.Name)
                                .GetValue(fixerResponse.ExchangeRates, null), CultureInfo.CreateSpecificCulture("en-US")));

                    currencyConversions.Add(currencyConversion);
                }
            }
            catch (Exception exception)
            {
                this.log.Error("[Exchange Rates : Fixer] Error retrieving exchange rates from fixer.io", exception);
            }

            return new ReadOnlyCollection<CurrencyConversion>(currencyConversions);
        }

        /// <summary>
        ///     Gets the name of the currency.
        /// </summary>
        /// <param name="isoCurrencySymbol">The ISO currency symbol.</param>
        /// <returns>System.String.</returns>
        private string GetCurrencyName(string isoCurrencySymbol)
        {
            RegionInfo currencyRegion =
                this.regionsInfos.FirstOrDefault(
                    r => r.ISOCurrencySymbol.Equals(isoCurrencySymbol, StringComparison.OrdinalIgnoreCase));

            return currencyRegion == null ? isoCurrencySymbol : currencyRegion.CurrencyNativeName;
        }

        /// <summary>
        ///     Gets the regions.
        /// </summary>
        /// <returns>List&lt;RegionInfo&gt;.</returns>
        private List<RegionInfo> GetRegions()
        {
            List<RegionInfo> regions = new List<RegionInfo>();
            CultureInfo[] cultures;

            try
            {
                cultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                this.log.Error("[Exchange Rates : Fixer] Error getting culture info", argumentOutOfRangeException);
                return regions;
            }

            //loop through all the cultures found
            foreach (CultureInfo culture in cultures)
            {
                //pass the current culture's Locale ID (http://msdn.microsoft.com/en-us/library/0h88fahh.aspx)
                //to the RegionInfo constructor to gain access to the information for that culture
                try
                {
                    RegionInfo region = new RegionInfo(culture.LCID);
                    regions.Add(region);
                }
                catch (ArgumentException argumentException)
                {
                    this.log.Error("[Exchange Rates : Fixer] Error add region info", argumentException);
                }
            }

            return regions;
        }
    }
}
