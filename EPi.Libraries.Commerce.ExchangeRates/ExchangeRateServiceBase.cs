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

    using EPiServer.Logging;

    /// <summary>
    ///     Class ExchangeRateServiceBase.
    /// </summary>
    public abstract class ExchangeRateServiceBase : IExchangeRateService
    {
        /// <summary>
        ///     The log
        /// </summary>
        protected ILogger log = LogManager.GetLogger();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExchangeRateServiceBase" /> class.
        /// </summary>
        protected ExchangeRateServiceBase()
        {
            this.RegionsInfo = this.GetRegions();
        }

        /// <summary>
        ///     Gets the regions infos
        /// </summary>
        private ReadOnlyCollection<RegionInfo> RegionsInfo { get; }

        /// <summary>
        ///     Gets the exchange rates.
        /// </summary>
        /// /// <param name="messages">A <see cref="List{T}"/> of messages.</param>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="CurrencyConversion"/>.</returns>
        public abstract ReadOnlyCollection<CurrencyConversion> GetExchangeRates(out List<string> messages);

        /// <summary>
        ///     Gets the name of the currency.
        /// </summary>
        /// <param name="isoCurrencySymbol">The ISO currency symbol.</param>
        /// <returns>The currency name.</returns>
        protected string GetCurrencyName(string isoCurrencySymbol)
        {
            RegionInfo currencyRegion = this.RegionsInfo.FirstOrDefault(
                r => r.ISOCurrencySymbol.Equals(
                    value: isoCurrencySymbol,
                    comparisonType: StringComparison.OrdinalIgnoreCase));

            return currencyRegion == null ? isoCurrencySymbol : currencyRegion.CurrencyEnglishName;
        }

        /// <summary>
        ///     Gets the regions.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> of <see cref="RegionInfo"/>.</returns>
        private ReadOnlyCollection<RegionInfo> GetRegions()
        {
            List<RegionInfo> regions = new List<RegionInfo>();
            CultureInfo[] cultures;

            try
            {
                cultures = CultureInfo.GetCultures(types: CultureTypes.AllCultures);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                this.log.Error(
                    "[Exchange Rates : Service] Error getting culture info",
                    exception: argumentOutOfRangeException);
                return new ReadOnlyCollection<RegionInfo>(list: regions);
            }

            // loop through all the cultures found
            foreach (CultureInfo culture in cultures)
            {
                // pass the current culture's Locale ID (http://msdn.microsoft.com/en-us/library/0h88fahh.aspx)
                // to the RegionInfo constructor to gain access to the information for that culture
                try
                {
                    if (culture.IsNeutralCulture)
                    {
                        continue;
                    }

                    RegionInfo region = new RegionInfo(culture: culture.LCID);
                    regions.Add(item: region);
                }
                catch (ArgumentException argumentException)
                {
                    this.log.Error(
                        "[Exchange Rates : Service] Error adding region info for: {0}",
                        culture.EnglishName,
                        argumentException);
                }
            }

            return new ReadOnlyCollection<RegionInfo>(list: regions);
        }
    }
}