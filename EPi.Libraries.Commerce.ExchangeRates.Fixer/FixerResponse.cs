// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FixerResponse.cs" company="Jeroen Stemerdink">
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

namespace EPi.Libraries.Commerce.ExchangeRates.Fixer
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Class FixerResponse.
    /// </summary>
    public class FixerResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FixerResponse"/> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        [JsonProperty("timestamp")]
        public float Timestamp { get; set; }

        /// <summary>
        ///     Gets or sets the base currency.
        /// </summary>
        /// <value>The base currency.</value>
        [JsonProperty(PropertyName = "base")]
        public string BaseCurrency { get; set; }

        /// <summary>
        ///     Gets or sets the exchange rates.
        /// </summary>
        /// <value>The exchange rates.</value>
        [JsonProperty(PropertyName = "rates")]
        public Rates ExchangeRates { get; set; }

        /// <summary>
        ///     Gets or sets the import date.
        /// </summary>
        /// <value>The import date.</value>
        [JsonProperty(PropertyName = "date")]
        public string ImportDate { get; set; }
    }
}