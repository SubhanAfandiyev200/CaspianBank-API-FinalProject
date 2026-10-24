using Xunit;

namespace CaspianBank.Tests.Infrastructure
{
    // Bir test sinfi üçün ayrıca baza (IClassFixture): sinfin testləri ardıcıl işləyir, başqa sinfin məlumatı qarışmır.
    // Bütün bazanın cəmlərinə baxan testlər (xəzinə, dashboard, aylıq iş) üçündür
    public sealed class IsolatedApp : IAsyncLifetime
    {
        public TestApp App { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            App = await TestApp.CreateAsync();
        }

        public async Task DisposeAsync()
        {
            await App.DisposeAsync();
        }
    }
}
