using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace VSL.UI.Views.Pages;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private void RepositoryLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(psi);
            e.Handled = true;
        }
        catch
        {
            // Ignore launcher failures.
        }
    }
}
