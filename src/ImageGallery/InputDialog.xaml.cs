using System.Windows;

namespace ImageGallery;

public partial class InputDialog : Window
{
    public string ResponseText { get; private set; } = string.Empty;

    public InputDialog(string prompt, string title, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        InputTextBox.Text = defaultValue;
        
        // Ensure the textbox gets focus when the dialog opens
        Loaded += (s, e) =>
        {
            InputTextBox.SelectAll();
            InputTextBox.Focus();
        };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ResponseText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
