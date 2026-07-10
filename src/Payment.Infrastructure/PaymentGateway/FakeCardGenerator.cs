using Bogus;

namespace Payment.Infrastructure.PaymentGateway;

public static class FakeCardGenerator
{
    public static (string Number, string Cvv, int Month, int Year, string Holder) Generate()
    {
        var faker = new Faker("pt_BR");
        var brand = faker.PickRandom(new[] { "Visa", "Mastercard", "Amex" });

        var number = brand switch
        {
            "Visa" => "4" + faker.Random.String2(15, "0123456789"),
            "Mastercard" => "5" + faker.Random.String2(15, "0123456789"),
            "Amex" => "3" + faker.Random.String2(14, "0123456789"),
            _ => faker.Finance.CreditCardNumber()
        };

        var cvv = brand == "Amex"
            ? faker.Random.String2(4, "0123456789")
            : faker.Random.String2(3, "0123456789");

        var month = faker.Random.Int(1, 12);
        var year = faker.Random.Int(DateTime.UtcNow.Year + 1, DateTime.UtcNow.Year + 5);
        var holder = faker.Name.FullName();

        return (number, cvv, month, year, holder);
    }
}
