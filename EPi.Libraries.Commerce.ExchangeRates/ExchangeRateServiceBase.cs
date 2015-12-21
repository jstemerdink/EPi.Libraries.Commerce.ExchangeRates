using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EPiServer.Logging;

namespace EPi.Libraries.Commerce.ExchangeRates
{
    /// <summary>
    /// Class ExchangeRateServiceBase.
    /// </summary>
    public abstract class ExchangeRateServiceBase : IExchangeRateService
    {
        /// <summary>
        /// The log
        /// </summary>
        protected ILogger Log = LogManager.GetLogger();

        /// <summary>
        /// The regions infos
        /// </summary>
        protected ReadOnlyCollection<RegionInfo> RegionsInfos;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        protected ExchangeRateServiceBase()
        {
            this.RegionsInfos = this.GetRegions();
        }

        /// <summary>
        /// Gets the exchange rates.
        /// </summary>
        /// <returns>ReadOnlyCollection&lt;CurrencyConversion&gt;.</returns>
        public abstract ReadOnlyCollection<CurrencyConversion> GetExchangeRates();

        /// <summary>
        ///     Gets the name of the currency.
        /// </summary>
        /// <param name="isoCurrencySymbol">The ISO currency symbol.</param>
        /// <returns>System.String.</returns>
        protected string GetCurrencyName(string isoCurrencySymbol)
        {
            RegionInfo currencyRegion =
                this.RegionsInfos.FirstOrDefault(
                    r => r.ISOCurrencySymbol.Equals(isoCurrencySymbol, StringComparison.OrdinalIgnoreCase));

            return currencyRegion == null ? isoCurrencySymbol : currencyRegion.CurrencyEnglishName;
        }

        /// <summary>
        ///     Gets the regions.
        /// </summary>
        /// <returns>List&lt;RegionInfo&gt;.</returns>
        protected ReadOnlyCollection<RegionInfo> GetRegions()
        {
            List<RegionInfo> regions = new List<RegionInfo>();
            CultureInfo[] cultures;

            try
            {
                cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                this.Log.Error("[Exchange Rates : Service] Error getting culture info", argumentOutOfRangeException);
                return new ReadOnlyCollection<RegionInfo>(regions);
            }

            //loop through all the cultures found
            foreach (CultureInfo culture in cultures)
            {
                //pass the current culture's Locale ID (http://msdn.microsoft.com/en-us/library/0h88fahh.aspx)
                //to the RegionInfo constructor to gain access to the information for that culture
                try
                {
                    if (!culture.IsNeutralCulture)
                    {
                        RegionInfo region = new RegionInfo(culture.LCID);
                        regions.Add(region);
                    }
                }
                catch (ArgumentException argumentException)
                {
                    this.Log.Error("[Exchange Rates : Service] Error adding region info for: {0}", culture.EnglishName, argumentException);
                }
            }

            return new ReadOnlyCollection<RegionInfo>(regions);
        }
    }
}
