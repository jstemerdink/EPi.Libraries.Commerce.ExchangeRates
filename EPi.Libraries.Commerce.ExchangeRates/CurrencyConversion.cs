﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyConversion.cs" company="Jeroen Stemerdink">
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

    /// <summary>
    ///     Class CurrencyConversion.
    /// </summary>
    public class CurrencyConversion
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CurrencyConversion" /> class.
        /// </summary>
        /// <param name="currency">The currency.</param>
        /// <param name="name">The name.</param>
        /// <param name="factor">The factor.</param>
        public CurrencyConversion(string currency, string name, decimal factor)
        {
            this.Currency = currency;
            this.Name = name;
            this.Factor = factor;
            this.CurrencyRateDate = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencyConversion"/> class.
        /// </summary>
        /// <param name="currency">The currency.</param>
        /// <param name="name">The name.</param>
        /// <param name="factor">The factor.</param>
        /// <param name="updated">The updated.</param>
        public CurrencyConversion(string currency, string name, decimal factor, DateTime updated)
        {
            this.Currency = currency;
            this.Name = name;
            this.Factor = factor;
            this.CurrencyRateDate = updated;
        }

        /// <summary>
        ///     Gets the currency
        /// </summary>
        public string Currency { get; }

        /// <summary>
        ///     Gets the factor
        /// </summary>
        public decimal Factor { get; }

        /// <summary>
        ///     Gets the name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the currency rate date.
        /// </summary>
        /// <value>The currency rate date.</value>
        public DateTime CurrencyRateDate { get; }
    }
}
