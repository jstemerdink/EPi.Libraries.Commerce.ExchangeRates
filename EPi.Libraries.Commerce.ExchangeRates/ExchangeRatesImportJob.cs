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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Configuration;

using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Core;

namespace EPi.Libraries.Commerce.ExchangeRates
{
    /// <summary>
    ///     Class ExchangeRatesImportJob.
    /// </summary>
    [ScheduledPlugIn(DisplayName = "Exchange Rates Import")]
    public class ExchangeRatesImportJob : ScheduledJobBase
    {
        /// <summary>
        ///     The conversion rates to USD
        /// </summary>
        private ReadOnlyCollection<CurrencyConversion> conversionRatesToUsd;

        private readonly ILogger log = LogManager.GetLogger();

        /// <summary>
        ///     The stop signaled
        /// </summary>
        private bool stopSignaled;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExchangeRatesImportJob" /> class.
        /// </summary>
        public ExchangeRatesImportJob()
        {
            this.IsStoppable = true;
        }

        /// <summary>
        ///     Gets or sets the exchange rate service.
        /// </summary>
        /// <value>The exchange rate service.</value>
        private Injected<IExchangeRateService> ExchangeRateService { get; set; }

        /// <summary>
        ///     Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            this.stopSignaled = true;
        }

        /// <summary>
        ///     Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        public override string Execute()
        {
            //Call OnStatusChanged to periodically notify progress of job for manually started jobs
            this.OnStatusChanged(string.Format(CultureInfo.InvariantCulture, "Starting execution of {0}", this.GetType()));

            List<string> messages;

            this.conversionRatesToUsd = this.ExchangeRateService.Service.GetExchangeRates(out messages);

            if (!messages.Any())
            {
                messages = this.CreateConversions();
            }

            //For long running jobs periodically check if stop is signaled and if so stop execution
            string returnMessage = messages.Any() ? string.Join("<br/>", messages) : "Exchange rates updated";
            return this.stopSignaled ? "Stop of job was called" : returnMessage;
        }

        /// <summary>
        ///     Creates the conversions.
        /// </summary>
        private List<string> CreateConversions()
        {
            List<string> messages = new List<string>();

            if (!this.conversionRatesToUsd.Any())
            {
                messages.Add("Error retrieving exchange rates from service");
            }

            this.EnsureCurrencies();

            CurrencyDto dto = CurrencyManager.GetCurrencyDto();

            foreach (CurrencyConversion conversion in this.conversionRatesToUsd)
            {
                List<CurrencyConversion> toCurrencies = this.conversionRatesToUsd.Where(c => c != conversion).ToList();
                messages = this.AddRates(dto, conversion, toCurrencies);
            }

            CurrencyManager.SaveCurrency(dto);

            return messages;
        }

        /// <summary>
        ///     Ensures the currencies.
        /// </summary>
        private void EnsureCurrencies()
        {
            bool isDirty = false;
            CurrencyDto dto = CurrencyManager.GetCurrencyDto();

            foreach (CurrencyConversion conversion in this.conversionRatesToUsd)
            {
                CurrencyDto.CurrencyRow currencyRow = GetCurrency(dto, conversion.Currency);

                if (currencyRow != null)
                {
                    continue;
                }

                dto.Currency.AddCurrencyRow(
                    conversion.Currency,
                    conversion.Name,
                    DateTime.Now,
                    AppContext.Current.ApplicationId);

                isDirty = true;
            }

            if (isDirty)
            {
                CurrencyManager.SaveCurrency(dto);
            }
        }

        /// <summary>
        ///     Adds the rates.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="from">From.</param>
        /// <param name="toCurrencies">To currencies.</param>
        private List<string> AddRates(CurrencyDto dto, CurrencyConversion from, IEnumerable<CurrencyConversion> toCurrencies)
        {
            List<string> messages = new List<string>();
            CurrencyDto.CurrencyRateDataTable rates = dto.CurrencyRate;

            foreach (CurrencyConversion to in toCurrencies)
            {
                double rate = (double)(@from.Factor / to.Factor);
                CurrencyDto.CurrencyRow fromRow = GetCurrency(dto, @from.Currency);
                CurrencyDto.CurrencyRow toRow = GetCurrency(dto, to.Currency);

                try
                {
                    CurrencyDto.CurrencyRateRow existingRow = rates.Rows.Cast<CurrencyDto.CurrencyRateRow>().FirstOrDefault(row => row.FromCurrencyId == fromRow.CurrencyId && row.ToCurrencyId == toRow.CurrencyId);

                    if (existingRow != null)
                    {
                        existingRow.AverageRate = rate;
                        existingRow.EndOfDayRate = rate;
                        existingRow.CurrencyRateDate = to.CurrencyRateDate;
                        existingRow.ModifiedDate = DateTime.Now;
                    }
                    else
                    {
                        if (fromRow.CurrencyId == toRow.CurrencyId)
                        {
                            continue;
                        }

                        rates.AddCurrencyRateRow(rate, rate, DateTime.Now, fromRow, toRow, to.CurrencyRateDate);
                    }

                    this.log.Information("[Exchange Rates : Job] Exchange rate updated for {0} : {1} ", to.Name, to.Factor);
                }
                catch (Exception exception)
                {
                    messages.Add(string.Format(CultureInfo.InvariantCulture, "Error setting exchange rates row: {0}", to.Name));
                    this.log.Error("[Exchange Rates : Job] Error setting exchange rates row for: {0}", to.Name, exception);
                }
            }

            return messages;
        }

        /// <summary>
        ///     Gets the currency.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="currencyCode">The currency code.</param>
        /// <returns>CurrencyDto.CurrencyRow.</returns>
        private static CurrencyDto.CurrencyRow GetCurrency(CurrencyDto dto, string currencyCode)
        {
            return
                (CurrencyDto.CurrencyRow)dto.Currency.Select("CurrencyCode = '" + currencyCode + "'").SingleOrDefault();
        }
    }
}
