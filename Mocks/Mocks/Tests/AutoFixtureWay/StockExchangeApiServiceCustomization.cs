using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Mocks.SUTs;
using Moq;

namespace Mocks.Tests.AutoFixtureWay
{
    public sealed class StockExchangeApiServiceCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var mock = fixture.Freeze<Mock<IStockExchangeApiService>>();

            var offers = new List<Offer>
            {
                new() {Id = 1, Type = OfferType.Sell, Count = 20, Price = 120},
                new() {Id = 2, Type = OfferType.Sell, Count = 10, Price = 110},
                new() {Id = 3, Type = OfferType.Sell, Count = 5, Price = 100},

                new() {Id = 4, Type = OfferType.Buy, Count = 5, Price = 90},
                new() {Id = 5, Type = OfferType.Buy, Count = 10, Price = 80},
                new() {Id = 6, Type = OfferType.Buy, Count = 20, Price = 70}
            };

            mock.Setup(x => x.ConnectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            mock.Setup(x => x.GetOffersAsync())
                .ReturnsAsync(offers);

            mock.Setup(x => x.BuyAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int offerId, int countToBuy) =>
                {
                    var offer = offers.Single(x => x.Id == offerId);

                    return offer.Count > countToBuy ? countToBuy : offer.Count;
                });

            mock.Setup(x => x.SellAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int offerId, int countToBuy) =>
                {
                    var offer = offers.Single(x => x.Id == offerId);

                    return offer.Count > countToBuy ? countToBuy : offer.Count;
                });

            fixture.Inject(mock.Object);
        }
    }
}