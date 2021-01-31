using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Mocks.SUTs;
using Moq;
using Xunit;

namespace Mocks.Tests.AutoFixtureWay
{
    public sealed class SellHandlerTests
    {
        [Fact]
        public async Task ShouldSellOffers_PriceEqualOrMore()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new ConfigurationCustomization())
                .Customize(new StockExchangeApiServiceCustomization());

            var handler = fixture.Create<SellHandler>();

            // act
            var boughtCount = await handler.Handle(new SellCommand {Price = 75, Count = 20});

            // assert
            Assert.Equal(15, boughtCount);

            var stockExchangeMock = fixture.Create<Mock<IStockExchangeApiService>>();

            stockExchangeMock.Verify(x => x.ConnectAsync(ConfigurationCustomization.StockExchangeUrl), Times.Once);

            stockExchangeMock.Verify(x => x.SellAsync(4, 20), Times.Once);
            stockExchangeMock.Verify(x => x.SellAsync(5, 15), Times.Once);
            stockExchangeMock.Verify(x => x.SellAsync(6, It.IsAny<int>()), Times.Never);
        }
    }
}