using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ImageGallery.Services;

/// <summary>
/// Manages visual indicators for speed and zoom changes.
/// Single Responsibility: Handle indicator display and animations.
/// </summary>
public class IndicatorManager
    {
        private Border? _speedIndicator;
        private TextBlock? _speedText;
        private Border? _zoomIndicator;
        private TextBlock? _zoomText;

        public void Initialize(Border speedBorder, TextBlock speedTxt, Border zoomBorder, TextBlock zoomTxt)
        {
            _speedIndicator = speedBorder;
            _speedText = speedTxt;
            _zoomIndicator = zoomBorder;
            _zoomText = zoomTxt;
        }

        public async void ShowSpeed(double intervalSeconds)
        {
            if (_speedIndicator == null || _speedText == null) return;

            _speedText.Text = $"{intervalSeconds:0.0}s";
            _speedIndicator.Visibility = Visibility.Visible;

            // Blink animation - 3 times
            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                var opacity = _speedIndicator.Opacity == 1.0 ? 0.5 : 1.0;
                _speedIndicator.Opacity = opacity;
            }

            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                var opacity = _speedIndicator.Opacity == 1.0 ? 0.5 : 1.0;
                _speedIndicator.Opacity = opacity;
            }

            _speedIndicator.Visibility = Visibility.Collapsed;
            _speedIndicator.Opacity = 1.0; // Reset for next time
        }

        public async void ShowZoom(int zoomPercent)
        {
            if (_zoomIndicator == null || _zoomText == null) return;

            _zoomText.Text = $"{zoomPercent}%";
            _zoomIndicator.Visibility = Visibility.Visible;

            // Blink animation (fainter than speed indicator)
            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                var opacity = _zoomIndicator.Opacity == 0.4 ? 0.2 : 0.4;
            _zoomIndicator.Opacity = opacity;
        }

        await Task.Delay(500);
        _zoomIndicator.Visibility = Visibility.Collapsed;
        _zoomIndicator.Opacity = 0.4;
    }
}