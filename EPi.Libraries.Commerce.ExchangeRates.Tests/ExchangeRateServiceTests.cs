// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateServiceTests.cs" company="Jeroen Stemerdink">
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

namespace EPi.Libraries.Commerce.ExchangeRates.Tests
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using FluentAssertions;

    using Mediachase.Commerce;
    using Mediachase.Commerce.Markets;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ExchangeRateServiceTests
    {
        private MockRepository mockRepository;

        private Mock<IMarketService> marketServiceMock;
        private Mock<IMarket> marketMock;

        [Test]
        public void GetCurrencyLayerExchangeRates_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            IExchangeRateService unitUnderTest = this.CreateCurrencyLayerService();
            List<string> messages;

            // Act
            ReadOnlyCollection<CurrencyConversion> result = unitUnderTest.GetExchangeRates(messages: out messages);

            messages.Count.Should().Be(0, "there should be no errors");

            result.Count.Should().Be(3, "the market has three currencies");
        }

        [Test]
        public void GetFixerExchangeRates_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var unitUnderTest = this.CreateFixerService();
            List<string> messages;

            // Act
            ReadOnlyCollection<CurrencyConversion> result = unitUnderTest.GetExchangeRates(messages: out messages);

            messages.Count.Should().Be(0, "there should be no errors");

            result.Count.Should().Be(3, "the market has three currencies");
        }

        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.marketServiceMock = this.mockRepository.Create<IMarketService>();
            this.marketMock = this.mockRepository.Create<IMarket>();
            this.marketMock.Setup(x => x.Currencies).Returns(() => new[] { new Currency("USD"), new Currency("SEK"), new Currency("EUR") });

            this.marketServiceMock.Setup(x => x.GetAllMarkets()).Returns(new List<IMarket>() { this.marketMock.Object });
        }

        [TearDown]
        public void TearDown()
        {
            this.mockRepository.VerifyAll();
        }

        private IExchangeRateService CreateCurrencyLayerService()
        {
            return new CurrencyLayer.ExchangeRateService(this.marketServiceMock.Object);
        }

        private IExchangeRateService CreateFixerService()
        {
            return new Fixer.ExchangeRateService(this.marketServiceMock.Object);
        }
    }
}