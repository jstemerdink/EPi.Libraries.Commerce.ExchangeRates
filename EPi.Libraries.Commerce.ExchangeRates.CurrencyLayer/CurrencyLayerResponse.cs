// Copyright © 2017 Jeroen Stemerdink. 
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

namespace EPi.Libraries.Commerce.ExchangeRates.CurrencyLayer
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Class CurrencyLayerResponse.
    /// </summary>
    public class CurrencyLayerResponse
    {
        /// <summary>
        ///     Gets or sets the base currency.
        /// </summary>
        /// <value>The base currency.</value>
        [JsonProperty(PropertyName = "source")]
        public string BaseCurrency { get; set; }

        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        [JsonProperty(PropertyName = "error")]
        public Error Error { get; set; }

        /// <summary>
        ///     Gets or sets the privacy.
        /// </summary>
        /// <value>The privacy.</value>
        [JsonProperty(PropertyName = "privacy")]
        public string Privacy { get; set; }

        /// <summary>
        ///     Gets or sets the quotes.
        /// </summary>
        /// <value>The quotes.</value>
        [JsonProperty(PropertyName = "quotes")]
        public Quotes Quotes { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="CurrencyLayerResponse" /> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets the terms.
        /// </summary>
        /// <value>The terms.</value>
        [JsonProperty(PropertyName = "terms")]
        public string Terms { get; set; }

        /// <summary>
        ///     Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        [JsonProperty(PropertyName = "timestamp")]
        public float Timestamp { get; set; }
    }
}