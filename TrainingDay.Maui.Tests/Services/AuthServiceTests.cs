using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Tests.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        [Test]
        public void IsExpired_ReturnsFalse_WellBeforeExpiry()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var expiresAtUtc = now.AddMinutes(10);

            Assert.IsFalse(AuthService.IsExpired(expiresAtUtc, now));
        }

        [Test]
        public void IsExpired_ReturnsTrue_AfterExpiry()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var expiresAtUtc = now.AddMinutes(-1);

            Assert.IsTrue(AuthService.IsExpired(expiresAtUtc, now));
        }

        [Test]
        public void IsExpired_ReturnsTrue_WithinSafetyBuffer()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var expiresAtUtc = now.AddSeconds(30);

            Assert.IsTrue(AuthService.IsExpired(expiresAtUtc, now));
        }
    }
}
