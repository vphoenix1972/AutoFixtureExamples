using System.Linq;
using System.Threading.Tasks;

namespace Mocks.SUTs
{
    public sealed class SellHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IStockExchangeApiService _stockExchange;

        public SellHandler(IConfiguration configuration, IStockExchangeApiService stockExchange)
        {
            _configuration = configuration;
            _stockExchange = stockExchange;
        }

        public async Task<int> Handle(SellCommand command)
        {
            var url = _configuration.StockExchangeUrl;

            await _stockExchange.ConnectAsync(url);

            var offers = await _stockExchange.GetOffersAsync();

            offers = offers
                .Where(x => x.Type == OfferType.Buy && x.Price >= command.Price)
                .OrderByDescending(x => x.Price)
                .ToList();

            var boughtCount = 0;
            foreach (var offer in offers)
            {
                var countToBuy = command.Count - boughtCount;

                boughtCount += await _stockExchange.SellAsync(offer.Id, countToBuy);

                if (boughtCount >= command.Count)
                    break;
            }

            return boughtCount;
        }
    }
}