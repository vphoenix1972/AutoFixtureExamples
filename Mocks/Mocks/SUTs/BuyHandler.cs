using System.Linq;
using System.Threading.Tasks;

namespace Mocks.SUTs
{
    public sealed class BuyHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IStockExchangeApiService _stockExchange;

        public BuyHandler(IConfiguration configuration, IStockExchangeApiService stockExchange)
        {
            _configuration = configuration;
            _stockExchange = stockExchange;
        }

        public async Task<int> Handle(BuyCommand command)
        {
            var url = _configuration.StockExchangeUrl;

            await _stockExchange.ConnectAsync(url);

            var offers = await _stockExchange.GetOffersAsync();

            offers = offers
                .Where(x => x.Type == OfferType.Sell && x.Price <= command.Price)
                .OrderBy(x => x.Price)
                .ToList();

            var boughtCount = 0;
            foreach (var offer in offers)
            {
                var countToBuy = command.Count - boughtCount;

                boughtCount += await _stockExchange.BuyAsync(offer.Id, countToBuy);

                if (boughtCount >= command.Count)
                    break;
            }

            return boughtCount;
        }
    }
}