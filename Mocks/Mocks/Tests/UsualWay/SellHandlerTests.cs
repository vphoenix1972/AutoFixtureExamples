using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mocks.SUTs;
using Moq;
using Xunit;

namespace Mocks.Tests.UsualWay
{
    public sealed class SellHandlerTests
    {
        [Fact]
        public async Task ShouldSellOffers_PriceEqualOrMore()
        {
            // arrange
            var configuration = Mock.Of<IConfiguration>();
            Mock.Get(configuration).SetupGet(x => x.StockExchangeUrl).Returns("https://moex.ru/api");

            var stockExchange = Mock.Of<IStockExchangeApiService>();
            Mock.Get(stockExchange).Setup(x => x.ConnectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var offers = new List<Offer>
            {
                new() {Id = 1, Type = OfferType.Sell, Count = 20, Price = 120},
                new() {Id = 2, Type = OfferType.Sell, Count = 10, Price = 110},
                new() {Id = 3, Type = OfferType.Sell, Count = 5, Price = 100},

                new() {Id = 4, Type = OfferType.Buy, Count = 5, Price = 90},
                new() {Id = 5, Type = OfferType.Buy, Count = 10, Price = 80},
                new() {Id = 6, Type = OfferType.Buy, Count = 20, Price = 70}
            };

            Mock.Get(stockExchange).Setup(x => x.GetOffersAsync())
                .ReturnsAsync(offers);

            Mock.Get(stockExchange).Setup(x => x.SellAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int offerId, int countToBuy) =>
                {
                    var offer = offers.Single(x => x.Id == offerId);

                    return offer.Count > countToBuy ? countToBuy : offer.Count;
                });

            var handler = new SellHandler(configuration, stockExchange);

            // act
            var boughtCount = await handler.Handle(new SellCommand {Price = 75, Count = 20});

            // assert
            Assert.Equal(15, boughtCount);

            Mock.Get(stockExchange).Verify(x => x.ConnectAsync(configuration.StockExchangeUrl), Times.Once);
            Mock.Get(stockExchange).Verify(x => x.SellAsync(4, 20), Times.Once);
            Mock.Get(stockExchange).Verify(x => x.SellAsync(5, 15), Times.Once);
            Mock.Get(stockExchange).Verify(x => x.SellAsync(6, It.IsAny<int>()), Times.Never);
        }
    }
}