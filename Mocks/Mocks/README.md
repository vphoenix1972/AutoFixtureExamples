# Использвание Autofixture для конфигурирования mock-ов для сервисов

Возможности Autofixture позволяют не только упростить создание данных для юнит тестов, но и упростить конфигурирование mock-ов для сервисов, используемых тестируемыми классами.

Для примера рассмотрим код, который получает доступные заявки на бирже и покупает бумаги по цене равной или ниже заданной:
```
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
        // Получаем ссылку для подключения к фондовой бирже из конфигурации
        var url = _configuration.StockExchangeUrl;

        // Подключаемся
        await _stockExchange.ConnectAsync(url);

        // Считываем заявки на покупку и продажу, доступные в данный момент
        var offers = await _stockExchange.GetOffersAsync();

        // Отфильтровываем заявки на продажу с ценой ниже или равной заданной
        offers = offers
            .Where(x => x.Type == OfferType.Sell && x.Price <= command.Price)
            .OrderBy(x => x.Price)
            .ToList();

        var boughtCount = 0;
        foreach (var offer in offers)
        {
            // Рассчитываем, сколько бумаг осталось купить
            var countToBuy = command.Count - boughtCount;

            // Покупаем по цене предложения столько, сколько доступно в данный момент
            boughtCount += await _stockExchange.BuyAsync(offer.Id, countToBuy);

            // Если купили требуемое количество, то заканчиваем процесс покупки
            if (boughtCount >= command.Count)
                break;
        }

        // Возвращаем купленное число ценных бумаг
        return boughtCount;
    }
}
```
У класса есть две зависимости - IConfiguration:
```
public interface IConfiguration
{
    string StockExchangeUrl { get; }
}
```
и IStockExchangeApiService:
```
public interface IStockExchangeApiService
{
    Task ConnectAsync(string url);

    Task<List<Offer>> GetOffersAsync();

    Task<int> BuyAsync(int offerId, int count);
}
```

Напишем тест на данный класс:
```
public sealed class BuyHandlerTests
{a
    [Fact]
    public async Task ShouldBuyOffers_PriceEqualOrLess()
    {
        // arrange

        // Конфигурируем mock сервиса IConfiguration
        var configuration = Mock.Of<IConfiguration>();
        Mock.Get(configuration).SetupGet(x => x.StockExchangeUrl).Returns("https://moex.ru/api");

        // Конфигурируем mock сервиса IStockExchangeApiService
        var stockExchange = Mock.Of<IStockExchangeApiService>();

        // Подключение будет всегда успешно
        Mock.Get(stockExchange).Setup(x =>           x.ConnectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Создаем список заявок для использования в тесте
        var offers = new List<Offer>
        {
            new() {Id = 1, Type = OfferType.Sell, Count = 20, Price = 120},
            new() {Id = 2, Type = OfferType.Sell, Count = 10, Price = 110},
            new() {Id = 3, Type = OfferType.Sell, Count = 5, Price = 100},

            new() {Id = 4, Type = OfferType.Buy, Count = 5, Price = 90},
            new() {Id = 5, Type = OfferType.Buy, Count = 10, Price = 80},
            new() {Id = 6, Type = OfferType.Buy, Count = 20, Price = 70}
        };

        // Возвращаем тестовый список заявок
        Mock.Get(stockExchange).Setup(x => x.GetOffersAsync())
            .ReturnsAsync(offers);

        // Прописываем логику работы mockа, чтобы возвращать число заявок, которые были куплены при вызове
        Mock.Get(stockExchange).Setup(x => x.BuyAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((int offerId, int countToBuy) =>
            {
                var offer = offers.Single(x => x.Id == offerId);

                return offer.Count > countToBuy ? countToBuy : offer.Count;
            });

        // Создаем handler
        var handler = new BuyHandler(configuration, stockExchange);

        // act
        var boughtCount = await handler.Handle(new BuyCommand {Price = 115, Count = 20});

        // assert

        // Должно быть куплено 15 бумаг
        Assert.Equal(15, boughtCount);

        // handler должен подключиться к бирже
        Mock.Get(stockExchange).Verify(x => x.ConnectAsync(configuration.StockExchangeUrl), Times.Once);

        // handler должен попробовать купить 20 бумаг по цене 100 (в итоге будет куплено 5)
        Mock.Get(stockExchange).Verify(x => x.BuyAsync(3, 20), Times.Once);

        // handler должен попробовать купить оставшиеся 15 бумаг по цене 110 (будет куплено 10)
        Mock.Get(stockExchange).Verify(x => x.BuyAsync(2, 15), Times.Once);

        // handle НЕ должен покупать бумаги по 120, тк максимальная цена указана равной 115
        Mock.Get(stockExchange).Verify(x => x.BuyAsync(1, It.IsAny<int>()), Times.Never);
    }
```
Как мы видим, достаточно много Arrange кода. Мы могли уменьшить его, например частично перенеся в конструктор или вынеся его отдельными методами, или использовать Autofixture.

У Autofixture есть интерфейс ICustomization, позволяющий настроить создание тестовых данных. Мы можем создавать наш handler при помощи Autofixture, предварительно настроив кастомизации для зависимостей handler.

Создадим кастомизацию для IConfiguration:
```
public sealed class ConfigurationCustomization : ICustomization
{
    public const string StockExchangeUrl = "https://moex.ru/api";

    public void Customize(IFixture fixture)
    {
        // Создадим mock при помощи fixture.Freeze, тем самым указав Autofixture возвращать один и тот же инстанс при создании
        var mock = fixture.Freeze<Mock<IConfiguration>>();

        // Настраиваем mock на возврат "https://moex.ru/api" в качестве StockExchangeUrl
        mock.SetupGet(x => x.StockExchangeUrl).Returns(StockExchangeUrl);

        // Указываем Autofixture возвращать mock при необходимости создания типа IConfiguration
        fixture.Inject(mock.Object);
    }
}
```
Выглядит несложно, теперь создадим кастомизацию для IStockExchangeApiService:
```
public sealed class StockExchangeApiServiceCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Создадим mock при помощи fixture.Freeze, тем самым указав Autofixture возвращать один и тот же инстанс при создании
        var mock = fixture.Freeze<Mock<IStockExchangeApiService>>();

        // Создаем список заявок, которые будет возвращать mock IStockExchangeApiService
        var offers = new List<Offer>
        {
            new() {Id = 1, Type = OfferType.Sell, Count = 20, Price = 120},
            new() {Id = 2, Type = OfferType.Sell, Count = 10, Price = 110},
            new() {Id = 3, Type = OfferType.Sell, Count = 5, Price = 100},

            new() {Id = 4, Type = OfferType.Buy, Count = 5, Price = 90},
            new() {Id = 5, Type = OfferType.Buy, Count = 10, Price = 80},
            new() {Id = 6, Type = OfferType.Buy, Count = 20, Price = 70}
        };

        // Подключение будет всегда успешно
        mock.Setup(x => x.ConnectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Возвращаем тестовый список заявок
        mock.Setup(x => x.GetOffersAsync())
            .ReturnsAsync(offers);

        // Прописываем логику работы mockа, чтобы возвращать число заявок, которые были куплены при вызове
        mock.Setup(x => x.BuyAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((int offerId, int countToBuy) =>
            {
                var offer = offers.Single(x => x.Id == offerId);

                return offer.Count > countToBuy ? countToBuy : offer.Count;
            });

        // Указываем Autofixture возвращать mock при необходимости создания типа IStockExchangeApiService
        fixture.Inject(mock.Object);
    }
}
```

Перепишем наш тест с использованием Autofixture
```
public sealed class BuyHandlerTests
{
    [Fact]
    public async Task ShouldBuyOffers_PriceEqualOrLess()
    {
        // arrange
        var fixture = new Fixture()
            // Применяем ConfigurationCustomization, чтобы Autofixture знал, как создавать тип IConfiguration
            .Customize(new ConfigurationCustomization())
            // Аналогично для IStockExchangeApiService
            .Customize(new StockExchangeApiServiceCustomization());

        // Создаем handler при помощи Autofixture. Autofixture автоматически будет использовать кастомизации для создания зависимостей IConfiguration и IStockExchangeApiService
        var handler = fixture.Create<BuyHandler>();

        // act
        var boughtCount = await handler.Handle(new BuyCommand {Price = 115, Count = 20});

        // assert

        // Должно быть куплено 15 бумаг
        Assert.Equal(15, boughtCount);

        // Получаем Mock<IStockExchangeApiService>
        var stockExchangeMock = fixture.Create<Mock<IStockExchangeApiService>>();

        // handler должен подключиться к бирже
        stockExchangeMock.Verify(x => x.ConnectAsync(ConfigurationCustomization.StockExchangeUrl), Times.Once);

        // handler должен попробовать купить 20 бумаг по цене 100 (в итоге будет куплено 5)
        stockExchangeMock.Verify(x => x.BuyAsync(3, 20), Times.Once);

        // handler должен попробовать купить оставшиеся 15 бумаг по цене 110 (будет куплено 10)
        stockExchangeMock.Verify(x => x.BuyAsync(2, 15), Times.Once);

        // handle НЕ должен покупать бумаги по 120, тк максимальная цена указана равной 115
        stockExchangeMock.Verify(x => x.BuyAsync(1, It.IsAny<int>()), Times.Never);
    }
}
```
Как мы видим, нам удалось значитель уменьшить arrange фазу теста и вынести конфигурирующий код так, чтобы его можно было переиспользовать в других тестах.

Enjoy!













