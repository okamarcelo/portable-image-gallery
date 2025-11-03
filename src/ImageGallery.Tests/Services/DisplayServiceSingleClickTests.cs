using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Input;
using Xunit;
using Moq;
using ImageGallery.Services;

namespace ImageGallery.Tests.Services
{
    /// <summary>
    /// Tests for single-click navigation functionality in DisplayService
    /// </summary>
    public class DisplayServiceSingleClickTests
    {
        private readonly Mock<ILogger<DisplayService>> _mockLogger;
        private readonly Mock<MosaicManager> _mockMosaicManager;
        private readonly Mock<ZoomController> _mockZoomController;
        private readonly Mock<NavigationService> _mockNavigationService;
        private readonly DisplayService _displayService;

        public DisplayServiceSingleClickTests()
        {
            _mockLogger = new Mock<ILogger<DisplayService>>();
            _mockMosaicManager = new Mock<MosaicManager>();
            _mockZoomController = new Mock<ZoomController>();
            _mockNavigationService = new Mock<NavigationService>();

            _displayService = new DisplayService(
                _mockLogger.Object,
                _mockMosaicManager.Object,
                _mockZoomController.Object,
                _mockNavigationService.Object);
        }

        [Fact]
        public void HandleMouseLeftButtonUp_SingleClick_NavigatesToNextImage()
        {
            // Arrange
            _mockMosaicManager.Setup(m => m.IsMosaicMode).Returns(false);
            _mockZoomController.Setup(z => z.IsZoomed).Returns(false);

            var mouseDownEvent = new Mock<MouseButtonEventArgs>(MouseButton.Left, 1);
            mouseDownEvent.Setup(e => e.GetPosition(It.IsAny<IInputElement>())).Returns(new Point(100, 100));

            var mockElement = new Mock<IInputElement>();
            
            // Simulate mouse down (start click)
            _displayService.HandleMouseLeftButtonDown(mouseDownEvent.Object, new Window(), mockElement.Object);

            // Act - Simulate mouse up at same position shortly after (single click)
            Mouse.SetPosition(mockElement.Object, new Point(100, 100)); // Same position
            System.Threading.Thread.Sleep(50); // Short delay (well under 500ms threshold)
            _displayService.HandleMouseLeftButtonUp(mockElement.Object);

            // Assert
            _mockNavigationService.Verify(n => n.NavigateNext(), Times.Once);
        }

        [Fact]
        public void HandleMouseLeftButtonUp_MosaicMode_DoesNotNavigate()
        {
            // Arrange
            _mockMosaicManager.Setup(m => m.IsMosaicMode).Returns(true); // In mosaic mode
            _mockZoomController.Setup(z => z.IsZoomed).Returns(false);

            var mouseDownEvent = new Mock<MouseButtonEventArgs>(MouseButton.Left, 1);
            mouseDownEvent.Setup(e => e.GetPosition(It.IsAny<IInputElement>())).Returns(new Point(100, 100));

            var mockElement = new Mock<IInputElement>();
            
            // Simulate single click
            _displayService.HandleMouseLeftButtonDown(mouseDownEvent.Object, new Window(), mockElement.Object);
            Mouse.SetPosition(mockElement.Object, new Point(100, 100));
            System.Threading.Thread.Sleep(50);
            _displayService.HandleMouseLeftButtonUp(mockElement.Object);

            // Assert - Should not navigate in mosaic mode
            _mockNavigationService.Verify(n => n.NavigateNext(), Times.Never);
        }

        [Fact]
        public void HandleMouseLeftButtonUp_ZoomedIn_DoesNotNavigate()
        {
            // Arrange
            _mockMosaicManager.Setup(m => m.IsMosaicMode).Returns(false);
            _mockZoomController.Setup(z => z.IsZoomed).Returns(true); // Zoomed in

            var mouseDownEvent = new Mock<MouseButtonEventArgs>(MouseButton.Left, 1);
            mouseDownEvent.Setup(e => e.GetPosition(It.IsAny<IInputElement>())).Returns(new Point(100, 100));

            var mockElement = new Mock<IInputElement>();
            
            // Simulate single click
            _displayService.HandleMouseLeftButtonDown(mouseDownEvent.Object, new Window(), mockElement.Object);
            Mouse.SetPosition(mockElement.Object, new Point(100, 100));
            System.Threading.Thread.Sleep(50);
            _displayService.HandleMouseLeftButtonUp(mockElement.Object);

            // Assert - Should not navigate when zoomed
            _mockNavigationService.Verify(n => n.NavigateNext(), Times.Never);
        }

        [Fact]
        public void HandleMouseLeftButtonUp_DragMovement_DoesNotNavigate()
        {
            // Arrange
            _mockMosaicManager.Setup(m => m.IsMosaicMode).Returns(false);
            _mockZoomController.Setup(z => z.IsZoomed).Returns(false);

            var mouseDownEvent = new Mock<MouseButtonEventArgs>(MouseButton.Left, 1);
            mouseDownEvent.Setup(e => e.GetPosition(It.IsAny<IInputElement>())).Returns(new Point(100, 100));

            var mockElement = new Mock<IInputElement>();
            
            // Simulate mouse down
            _displayService.HandleMouseLeftButtonDown(mouseDownEvent.Object, new Window(), mockElement.Object);

            // Act - Simulate mouse up at different position (drag movement > threshold)
            Mouse.SetPosition(mockElement.Object, new Point(110, 110)); // 10+ pixels away
            System.Threading.Thread.Sleep(50);
            _displayService.HandleMouseLeftButtonUp(mockElement.Object);

            // Assert - Should not navigate on drag
            _mockNavigationService.Verify(n => n.NavigateNext(), Times.Never);
        }

        [Fact]
        public void HandleMouseLeftButtonUp_LongPress_DoesNotNavigate()
        {
            // Arrange
            _mockMosaicManager.Setup(m => m.IsMosaicMode).Returns(false);
            _mockZoomController.Setup(z => z.IsZoomed).Returns(false);

            var mouseDownEvent = new Mock<MouseButtonEventArgs>(MouseButton.Left, 1);
            mouseDownEvent.Setup(e => e.GetPosition(It.IsAny<IInputElement>())).Returns(new Point(100, 100));

            var mockElement = new Mock<IInputElement>();
            
            // Simulate mouse down
            _displayService.HandleMouseLeftButtonDown(mouseDownEvent.Object, new Window(), mockElement.Object);

            // Act - Simulate mouse up after long delay (> 500ms threshold)
            Mouse.SetPosition(mockElement.Object, new Point(100, 100));
            System.Threading.Thread.Sleep(600); // Longer than threshold
            _displayService.HandleMouseLeftButtonUp(mockElement.Object);

            // Assert - Should not navigate on long press
            _mockNavigationService.Verify(n => n.NavigateNext(), Times.Never);
        }
    }
}