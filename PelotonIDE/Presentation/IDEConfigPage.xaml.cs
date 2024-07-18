using Microsoft.Win32;

using LanguageConfigurationStructureSelection =
    System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, string>>;

namespace PelotonIDE.Presentation
{
    public sealed partial class IDEConfigPage : Page
    {
        public IDEConfigPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            NavigationData parameters = (NavigationData)e.Parameter;

            if (parameters.Source == "MainPage")
            {
                protiumInterpreterTextBox.Text = parameters.KVPs["ideOps.Engine.2"].ToString();
                pelotonInterpreterTextBox.Text = parameters.KVPs["ideOps.Engine.3"].ToString();
                sourceTextBox.Text = parameters.KVPs["ideOps.CodeFolder"].ToString();
                dataTextBox.Text = parameters.KVPs["ideOps.DataFolder"].ToString();
                LanguageConfigurationStructureSelection lcs = (LanguageConfigurationStructureSelection)parameters.KVPs["pOps.Language"];
                cmdCancel.Content = lcs["frmMain"]["cmdCancel"];
                cmdSaveMemory.Content = lcs["frmMain"]["cmdSaveMemory"];
                lblSourceDirectory.Text = lcs["frmMain"]["lblSourceDirectory"];
            }
        }
        private async void ProtiumInterpreterLocationBtn_Click(object sender, RoutedEventArgs e)
        { 
            var temp = FileFolderPicking.GetFile("Protium Interpreter?", Path.GetDirectoryName(protiumInterpreterTextBox.Text), "EXE files (*.exe)|*.exe");
            if (temp[0] == "OK")
                protiumInterpreterTextBox.Text = temp[1];
        }
        private async void PelotonInterpreterLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            var temp = FileFolderPicking.GetFile("Peloton Interpreter?", Path.GetDirectoryName(pelotonInterpreterTextBox.Text), "EXE files (*.exe)|*.exe");
            if (temp[0] == "OK")
                pelotonInterpreterTextBox.Text = temp[1];
        }
        private async void SourceDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var temp = FileFolderPicking.GetFolder(sourceTextBox.Text);
            if (temp[0] == "OK")
                sourceTextBox.Text = temp[1];
        }
        private async void DataDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var temp = FileFolderPicking.GetFolder(dataTextBox.Text);
            if (temp[0] == "OK")
                dataTextBox.Text = temp[1];
        }
        private void IDEConfig_Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationData nd = new()
            {
                Source = "IDEConfig",
                KVPs = new()
                {
                    { "ideOps.Engine.2" , protiumInterpreterTextBox.Text },
                    { "ideOps.Engine.3" , pelotonInterpreterTextBox.Text },
                    { "ideOps.CodeFolder" , sourceTextBox.Text},
                    { "ideOps.DataFolder", dataTextBox.Text },
                }
            };
            Frame.Navigate(typeof(MainPage), nd);

        }
        private void IDEConfig_Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null);
        }
    }
}