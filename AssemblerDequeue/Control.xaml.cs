using System.Windows;
using System.Windows.Controls;
using VRage.Game.Entity;

namespace AssemblerDequeue
{
    public partial class Control : UserControl
    {
        private DequeuePlugin Plugin { get; }
        public Control()
        {
            InitializeComponent();
        }

        public Control(DequeuePlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }
        
        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }

    }
}