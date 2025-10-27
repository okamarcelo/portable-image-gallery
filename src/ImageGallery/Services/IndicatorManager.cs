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
        private Border? speedIndicator;
        private TextBlock? speedText;
        private Border? zoomIndicator;
        private TextBlock? zoomText;

        public void Initialize(Border speedBorder, TextBlock speedTxt, Border zoomBorder, TextBlock zoomTxt)
        {
            speedIndicator = speedBorder;
            speedText = speedTxt;
            zoomIndicator = zoomBorder;
            zoomText = zoomTxt;
        }

        public async void ShowSpeed(double intervalSeconds)
        {
            if (speedIndicator == null || speedText == null) return;

            speedText.Text = $"{intervalSeconds:0.0}s";
            speedIndicator.Visibility = Visibility.Visible;

            // Blink animation - 3 times
            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                var opacity = speedIndicator.Opacity == 1.0 ? 0.5 : 1.0;
                speedIndicator.Opacity = opacity;
            }

            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                var opacity = speedIndicator.Opacity == 1.0 ? 0.5 : 1.0;
                speedIndicator.Opacity = opacity;
            }

            speedIndicator.Visibility = Visibility.Collapsed;
            speedIndicator.Opacity = 1.0; // Reset for next time
        }

        public async void ShowZoom(int zoomPercent)
        {
            if (zoomIndicator == null || zoomText == null) return;

            zoomText.Text = $"{zoomPercent}%";
            zoomIndicator.Visibility = Visibility.Visible;

            // Blink animation (fainter than speed indicator)
            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                var opacity = zoomIndicator.Opacity == 0.4 ? 0.2 : 0.4;
            zoomIndicator.Opacity = opacity;
        }

        await Task.Delay(500);
        zoomIndicator.Visibility = Visibility.Collapsed;
        zoomIndicator.Opacity = 0.4;
    }
}