// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyConversionEqualityComparer.cs" company="Jeroen Stemerdink">
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

    /// <summary>
    /// Class CurrencyConversionEqualityComparer.
    /// Implements the <see cref="System.Collections.Generic.IEqualityComparer{CurrencyConversion}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{CurrencyConversion}" />
    public class CurrencyConversionEqualityComparer : IEqualityComparer<CurrencyConversion>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <see cref="CurrencyConversion" /> to compare.</param>
        /// <param name="y">The second object of type <see cref="CurrencyConversion" /> to compare.</param>
        /// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
        public bool Equals(CurrencyConversion x, CurrencyConversion y)
        {
            if ((x == null) | (y == null))
            {
                return false;
            }

            return x.Currency.Equals(value: y.Currency, comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        /// <exception cref="T:System.OverflowException"><paramref name="obj" /> is less than <see cref="F:System.Int32.MinValue" /> or greater than <see cref="F:System.Int32.MaxValue" />.</exception>
        public int GetHashCode(CurrencyConversion obj)
        {
            return obj.Currency.GetHashCode() + decimal.ToInt32(d: obj.Factor);
        }
    }
}