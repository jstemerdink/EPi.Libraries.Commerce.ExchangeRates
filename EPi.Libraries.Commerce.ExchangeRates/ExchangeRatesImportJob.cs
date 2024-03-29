﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatesImportJob.cs" company="Jeroen Stemerdink">
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
    using EPiServer.PlugIn;
    using EPiServer.Scheduler;
    using EPiServer.ServiceLocation;

    using Mediachase.Commerce.Catalog.Dto;
    using Mediachase.Commerce.Catalog.Managers;
    
    /// <summary>
    ///     Class ExchangeRatesImportJob.
    /// </summary>
    [ScheduledPlugIn(DisplayName = "Exchange Rates Import", DefaultEnabled = true, Restartable = false)]
    [ServiceConfiguration]
    public class ExchangeRatesImportJob : ScheduledJobBase
    {
        /// <summary>
        /// The log
        /// </summary>
        private readonly ILogger log = LogManager.GetLogger(typeof(ExchangeRatesImportJob));

        /// <summary>
        ///     Gets or sets the exchange rate service.
        /// </summary>
        /// <value>The exchange rate service.</value>
        private readonly IExchangeRateService exchangeRateService;

        /// <summary>
        ///     The conversion rates
        /// </summary>
        private ReadOnlyCollection<CurrencyConversion> currencyConversions;

        /// <summary>
        ///     The stop signaled
        /// </summary>
        private bool stopSignaled;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExchangeRatesImportJob" /> class.
        /// </summary>
        /// <param name="exchangeRateService">The exchange rate service.</param>
        public ExchangeRatesImportJob(IExchangeRateService exchangeRateService)
        {
            this.exchangeRateService = exchangeRateService;
            this.IsStoppable = true;
        }

        /// <summary>
        ///     Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        public override string Execute()
        {
            this.OnStatusChanged(
                string.Format(
                    provider: CultureInfo.InvariantCulture,
                    format: "Starting execution of {0}",
                    arg0: this.GetType()));

            List<string> messages;

            this.currencyConversions = this.exchangeRateService.GetExchangeRates(messages: out messages);

            if (!messages.Any())
            {
                this.OnStatusChanged($"Processing {this.currencyConversions.Count} exchange rates");

                messages = this.CreateConversions();
            }

            string returnMessage = messages.Any() ? string.Join("<br/>", values: messages) : "Exchange rates updated";
            return this.stopSignaled ? "Stop of job was called" : returnMessage;
        }

        /// <summary>
        ///     Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            this.stopSignaled = true;
        }

        /// <summary>
        ///     Gets the currency.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="currencyCode">The currency code.</param>
        /// <returns>The CurrencyDto.CurrencyRow.</returns>
        private static CurrencyDto.CurrencyRow GetCurrency(CurrencyDto dto, string currencyCode)
        {
            return (CurrencyDto.CurrencyRow)dto.Currency.Select("CurrencyCode = '" + currencyCode + "'")
                .SingleOrDefault();
        }

        /// <summary>
        /// Adds the rates.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="from">From currency.</param>
        /// <param name="toCurrencies">To currencies.</param>
        /// <returns>A list of rates.</returns>
        private List<string> AddRates(
            CurrencyDto dto,
            CurrencyConversion from,
            IEnumerable<CurrencyConversion> toCurrencies)
        {
            List<string> messages = new List<string>();

            if (this.stopSignaled)
            {
                return messages;
            }

            CurrencyDto.CurrencyRateDataTable rates = dto.CurrencyRate;

            foreach (CurrencyConversion to in toCurrencies)
            {
                if (this.stopSignaled)
                {
                    messages.Add("Stop of job was called");
                    break;
                }

                try
                {
                    double rate = (double)(to.Factor / from.Factor);
                    CurrencyDto.CurrencyRow fromRow = GetCurrency(dto: dto, currencyCode: from.Currency);
                    CurrencyDto.CurrencyRow toRow = GetCurrency(dto: dto, currencyCode: to.Currency);

                    if (fromRow.CurrencyId == toRow.CurrencyId)
                    {
                        continue;
                    }

                    CurrencyDto.CurrencyRateRow existingRow = rates.Rows.Cast<CurrencyDto.CurrencyRateRow>()
                        .LastOrDefault(
                            row => row.FromCurrencyId == fromRow.CurrencyId && row.ToCurrencyId == toRow.CurrencyId);
                    
                    if (existingRow != null)
                    {
                        rates.RemoveCurrencyRateRow(existingRow);
                        
                        this.log.Information($"[Exchange Rates : Job] Old exchange rate removed for {to.Name} : {to.Factor}");
                    }

                    rates.AddCurrencyRateRow(
                        AverageRate: rate,
                        EndOfDayRate: rate,
                        ModifiedDate: DateTime.Now,
                        parentCurrencyRowByFK_CurrencyRate_Currency: fromRow,
                        parentCurrencyRowByFK_CurrencyRate_Currency1: toRow,
                        CurrencyRateDate: to.CurrencyRateDate);

                    this.log.Information($"[Exchange Rates : Job] Exchange rate added for {to.Name} : {to.Factor}");
                }
                catch (Exception exception)
                {
                    messages.Add(
                        string.Format(
                            provider: CultureInfo.InvariantCulture,
                            format: "Error setting exchange rates row: {0}",
                            arg0: to.Name));

                    this.log.Error($"[Exchange Rates : Job] Error setting exchange rates row for {to.Name}", exception);
                }
            }

            return messages;
        }

        /// <summary>
        /// Creates the conversions.
        /// </summary>
        /// <returns>A list of conversions.</returns>
        private List<string> CreateConversions()
        {
            List<string> messages = new List<string>();

            if (this.stopSignaled)
            {
                return messages;
            }

            if (!this.currencyConversions.Any())
            {
                messages.Add("Error retrieving exchange rates from service");
                return messages;
            }

            this.EnsureCurrencies();

            CurrencyDto dto = CurrencyManager.GetCurrencyDto();

            foreach (CurrencyConversion conversion in this.currencyConversions)
            {
                if (this.stopSignaled)
                {
                    messages.Add("Stop of job was called");
                    break;
                }

                List<CurrencyConversion> toCurrencies = this.currencyConversions.Where(c => c != conversion).ToList();
                messages = this.AddRates(dto: dto, from: conversion, toCurrencies: toCurrencies);
            }

            CurrencyManager.SaveCurrency(dataset: dto);

            return messages;
        }

        /// <summary>
        ///     Ensures the currencies.
        /// </summary>
        private void EnsureCurrencies()
        {
            bool isDirty = false;
            CurrencyDto dto = CurrencyManager.GetCurrencyDto();

            foreach (CurrencyConversion conversion in this.currencyConversions)
            {
                CurrencyDto.CurrencyRow currencyRow = GetCurrency(dto: dto, currencyCode: conversion.Currency);

                if (currencyRow != null)
                {
                    continue;
                }

                dto.Currency.AddCurrencyRow(
                    CurrencyCode: conversion.Currency,
                    Name: conversion.Name,
                    ModifiedDate: DateTime.Now);

                isDirty = true;
            }

            if (isDirty)
            {
                CurrencyManager.SaveCurrency(dataset: dto);
            }
        }
    }
}