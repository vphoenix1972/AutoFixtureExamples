using AutoFixture;
using Mocks.SUTs;
using Moq;

namespace Mocks.Tests.AutoFixtureWay
{
    public sealed class ConfigurationCustomization : ICustomization
    {
        public const string StockExchangeUrl = "https://moex.ru/api";

        public void Customize(IFixture fixture)
        {
            var mock = fixture.Freeze<Mock<IConfiguration>>();
            mock.SetupGet(x => x.StockExchangeUrl).Returns(StockExchangeUrl);

            fixture.Inject(mock.Object);
        }
    }
}