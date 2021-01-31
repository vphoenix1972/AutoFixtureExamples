using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mocks.SUTs
{
    public interface IStockExchangeApiService
    {
        Task ConnectAsync(string url);
        Task<List<Offer>> GetOffersAsync();
        Task<int> BuyAsync(int offerId, int count);

        Task<int> SellAsync(int offerId, int count);
    }
}