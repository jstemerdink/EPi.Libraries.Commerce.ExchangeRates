using EPi.Libraries.Commerce.ExchangeRates.Fixer;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace EPi.Libraries.Commerce.ExchangeRates.Tests
{
    using System.Collections.ObjectModel;

    using FluentAssertions;

    [TestFixture]
    public class ExchangeRateServiceTests
    {
        private MockRepository mockRepository;



        [SetUp]
        public void SetUp()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockRepository.VerifyAll();
        }

        private IExchangeRateService CreateFixerService()
        {
            return new EPi.Libraries.Commerce.ExchangeRates.Fixer.ExchangeRateService();
        }

        private IExchangeRateService CreateCurrencyLayerService()
        {
            return new EPi.Libraries.Commerce.ExchangeRates.CurrencyLayer.ExchangeRateService();
        }

        [Test]
        public void GetCurrencyLayerExchangeRates_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var unitUnderTest = this.CreateCurrencyLayerService();
            List<string> messages = new List<string>();

            // Act
            ReadOnlyCollection<CurrencyConversion> result = unitUnderTest.GetExchangeRates(out messages);

            messages.Count.Should().Be(0, "there should be no errors");

            result.Count.Should().BeGreaterThan(0, "the service should return exchange rates");
        }

        [Test]
        public void GetFixerExchangeRates_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var unitUnderTest = this.CreateFixerService();
            List<string> messages = new List<string>();

            // Act
            ReadOnlyCollection<CurrencyConversion> result = unitUnderTest.GetExchangeRates(out messages);

            messages.Count.Should().Be(0, "there should be no errors");

            result.Count.Should().BeGreaterThan(0, "the service should return exchange rates");
        }
    }
}
