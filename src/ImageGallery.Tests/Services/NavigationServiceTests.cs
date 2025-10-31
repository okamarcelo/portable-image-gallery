using Xunit;

namespace ImageGallery.Tests.Services
{
    public class NavigationServiceTests
    {
        [Fact]
        public void NavigationService_Exists()
        {
            // This test ensures the NavigationService class exists and can be referenced
            // More comprehensive integration tests would require the full DI container setup
            // which is complex for unit tests, so we focus on testing that the service was
            // successfully extracted and compiles properly.
            
            Assert.True(typeof(ImageGallery.Services.NavigationService).IsClass);
        }

        [Fact]
        public void NavigationService_HasCurrentIndexProperty()
        {
            // Verify the NavigationService has the CurrentIndex property
            var property = typeof(ImageGallery.Services.NavigationService).GetProperty("CurrentIndex");
            
            Assert.NotNull(property);
            Assert.Equal(typeof(int), property.PropertyType);
            Assert.True(property.CanRead);
            Assert.True(property.CanWrite);
        }

        [Fact]
        public void NavigationService_HasNavigationMethods()
        {
            // Verify the NavigationService has the main navigation methods
            var type = typeof(ImageGallery.Services.NavigationService);
            
            var navigateNext = type.GetMethod("NavigateNext");
            var navigatePrevious = type.GetMethod("NavigatePrevious");
            var showImage = type.GetMethod("ShowImage");
            var onSlideshowTick = type.GetMethod("OnSlideshowTick");
            
            Assert.NotNull(navigateNext);
            Assert.NotNull(navigatePrevious);
            Assert.NotNull(showImage);
            Assert.NotNull(onSlideshowTick);
        }

        [Fact]
        public void NavigationService_HasRequiredEvents()
        {
            // Verify the NavigationService has the required events for UI updates
            var type = typeof(ImageGallery.Services.NavigationService);
            
            var imagesDisplayRequested = type.GetEvent("ImagesDisplayRequested");
            var mosaicLayoutUpdateRequested = type.GetEvent("MosaicLayoutUpdateRequested");
            var flashSideRequested = type.GetEvent("FlashSideRequested");
            
            Assert.NotNull(imagesDisplayRequested);
            Assert.NotNull(mosaicLayoutUpdateRequested);
            Assert.NotNull(flashSideRequested);
        }
    }
}