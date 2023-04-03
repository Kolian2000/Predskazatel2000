using TelegramBotExperiments;
namespace Test;

[TestFixture]
public class HoroscopeTests
{
    [Test]
    public async Task GetHoroscope_ReturnsNonEmptyString()
    {
        // Arrange
        var zodiacSign = "Aries";

        // Act
        var horoscope = await Program.GetHoroscope(zodiacSign);

        // Assert
        Assert.That(horoscope, Is.Not.Empty);
    }
}
