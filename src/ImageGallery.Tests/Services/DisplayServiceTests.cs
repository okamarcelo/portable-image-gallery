using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using ImageGallery.Services;

namespace ImageGallery.Tests.Services
{
    public class DisplayServiceTests
    {
        [Fact]
        public void DisplayService_Exists()
        {
            // This test ensures the DisplayService class exists and can be referenced
            // More comprehensive integration tests would require the full WPF UI context
            // which is complex for unit tests, so we focus on testing that the service was
            // successfully extracted and compiles properly.
            
            Assert.True(typeof(ImageGallery.Services.DisplayService).IsClass);
        }

        [Fact]
        public void DisplayService_HasUpdateMosaicLayoutMethod()
        {
            // Verify the DisplayService has the UpdateMosaicLayout method
            var method = typeof(ImageGallery.Services.DisplayService).GetMethod("UpdateMosaicLayout");
            
            Assert.NotNull(method);
            Assert.Equal(3, method.GetParameters().Length); // mosaicDisplay, windowWidth, windowHeight
        }

        [Fact]
        public void DisplayService_HasFlashSideAsyncMethod()
        {
            // Verify the DisplayService has the FlashSideAsync method
            var method = typeof(ImageGallery.Services.DisplayService).GetMethod("FlashSideAsync");
            
            Assert.NotNull(method);
            Assert.Equal(3, method.GetParameters().Length); // isRight, rightFlash, leftFlash
        }

        [Fact]
        public void DisplayService_HasMouseHandlerMethods()
        {
            // Verify the DisplayService has mouse interaction methods
            var type = typeof(ImageGallery.Services.DisplayService);
            
            var handleMouseWheel = type.GetMethod("HandleMouseWheel");
            var handleMouseLeftButtonDown = type.GetMethod("HandleMouseLeftButtonDown");
            var handleMouseLeftButtonUp = type.GetMethod("HandleMouseLeftButtonUp");
            var handleMouseMove = type.GetMethod("HandleMouseMove");
            
            Assert.NotNull(handleMouseWheel);
            Assert.NotNull(handleMouseLeftButtonDown);
            Assert.NotNull(handleMouseLeftButtonUp);
            Assert.NotNull(handleMouseMove);
        }

        [Fact]
        public void DisplayService_HasFindVisualChildMethod()
        {
            // Verify the DisplayService has the FindVisualChild utility method
            var method = typeof(ImageGallery.Services.DisplayService).GetMethod("FindVisualChild");
            
            Assert.NotNull(method);
            Assert.True(method.IsGenericMethod);
        }

        [Fact]
        public void DisplayService_HasRequiredEvents()
        {
            // Verify the DisplayService has the required events
            var type = typeof(ImageGallery.Services.DisplayService);
            
            var logMessageRequested = type.GetEvent("LogMessageRequested");
            
            Assert.NotNull(logMessageRequested);
        }
    }
}