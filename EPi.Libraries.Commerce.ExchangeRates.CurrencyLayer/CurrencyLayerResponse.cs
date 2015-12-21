using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace EPi.Libraries.Commerce.ExchangeRates.CurrencyLayer
{
    /// <summary>
    /// Class CurrencyLayerResponse.
    /// </summary>
    public class CurrencyLayerResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CurrencyLayerResponse"/> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }
        /// <summary>
        /// Gets or sets the terms.
        /// </summary>
        /// <value>The terms.</value>
        [JsonProperty(PropertyName = "terms")]
        public string Terms { get; set; }
        /// <summary>
        /// Gets or sets the privacy.
        /// </summary>
        /// <value>The privacy.</value>
        [JsonProperty(PropertyName = "privacy")]
        public string Privacy { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        [JsonProperty(PropertyName = "timestamp")]
        public float Timestamp { get; set; }
        /// <summary>
        /// Gets or sets the base currency.
        /// </summary>
        /// <value>The base currency.</value>
        [JsonProperty(PropertyName = "source")]
        public string BaseCurrency { get; set; }
        /// <summary>
        /// Gets or sets the quotes.
        /// </summary>
        /// <value>The quotes.</value>
        [JsonProperty(PropertyName = "quotes")]
        public Quotes Quotes { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        [JsonProperty(PropertyName = "error")]
        public Error Error { get; set; }
    }
}
