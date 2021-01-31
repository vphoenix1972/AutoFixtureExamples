using System.Threading.Tasks;
using AutoFixture;
using Mocks.SUTs;
using Moq;
using Xunit;

namespace Mocks.Tests.AutoFixtureWay
{
    public sealed class BuyHandlerTests
    {
        [Fact]
        public async Task ShouldBuyOffers_PriceEqualOrLess()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new ConfigurationCustomization())
                .Customize(new StockExchangeApiServiceCustomization());

            var handler = fixture.Create<BuyHandler>();

            // act
            var boughtCount = await handler.Handle(new BuyCommand {Price = 115, Count = 20});

            // assert
            Assert.Equal(15, boughtCount);

            var stockExchangeMock = fixture.Create<Mock<IStockExchangeApiService>>();

            stockExchangeMock.Verify(x => x.ConnectAsync(ConfigurationCustomization.StockExchangeUrl), Times.Once);

            stockExchangeMock.Verify(x => x.BuyAsync(3, 20), Times.Once);
            stockExchangeMock.Verify(x => x.BuyAsync(2, 15), Times.Once);
            stockExchangeMock.Verify(x => x.BuyAsync(1, It.IsAny<int>()), Times.Never);
        }
    }
}