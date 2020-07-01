using System.Windows.Controls;


namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityButtonHeader.xaml
    /// </summary>
    public partial class GameActivityButtonHeader : Button
    {
        public GameActivityButtonHeader(string Content)
        {
            InitializeComponent();

            btHeaderName.Text = Content;
        }
    }
}
