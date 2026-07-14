using Bogus;

namespace Payment.Infrastructure.PaymentGateway;

// Generates fake credit card data for development and testing.
// Card numbers are random (not Luhn-valid) and should only be used with the FakePaymentGateway.
public static class FakeCardGenerator
{
    // Returns a tuple with card number, CVV, expiry month, expiry year, and holder name
    public static (string Number, string Cvv, int Month, int Year, string Holder) Generate()
    {
        var faker = new Faker("pt_BR");
        var brand = faker.PickRandom(new[] { "Visa", "Mastercard", "Amex" });

        // Generate card number starting with the brand's IIN prefix
        var number = brand switch
        {
            "Visa" => "4" + faker.Random.String2(15, "0123456789"),
            "Mastercard" => "5" + faker.Random.String2(15, "0123456789"),
            "Amex" => "3" + faker.Random.String2(14, "0123456789"),
            _ => faker.Finance.CreditCardNumber()
        };

        // Amex uses 4-digit CVV; other brands use 3-digit
        var cvv = brand == "Amex"
            ? faker.Random.String2(4, "0123456789")
            : faker.Random.String2(3, "0123456789");

        var month = faker.Random.Int(1, 12);
        var year = faker.Random.Int(DateTime.UtcNow.Year + 1, DateTime.UtcNow.Year + 5);
        var holder = faker.Name.FullName();

        return (number, cvv, month, year, holder);
    }
}
