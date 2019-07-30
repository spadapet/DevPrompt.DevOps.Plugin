using DevOps.Plugin.UI.ViewModels;
using DevOps.Plugin.Utility;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace DevOps.Plugin.UI
{
    internal partial class LoginPage : UserControl
    {
        public PullRequestTab Tab { get; }
        public LoginPageVM ViewModel { get; }
        private CancellationTokenSource cancellationTokenSource;

        public LoginPage(PullRequestTab tab)
        {
            this.Tab = tab;
            this.ViewModel = new LoginPageVM(tab);

            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            try
            {
                AzureDevOpsUserContext devopsUserContext;

                using (this.ViewModel.Window.ProgressBar.Begin(this.cancellationTokenSource.Cancel, DevOps.Plugin.Resources.LoginPage_ProgressText))
                {
                    devopsUserContext = await AzureDevOpsClient.GetUserContext();
                }

                PullRequestPageVM vm = new PullRequestPageVM(this.Tab, devopsUserContext);
                this.Tab.ViewElement = new PullRequestPage(vm);
            }
            catch (Exception ex)
            {
                this.ViewModel.DisplayText = DevOps.Plugin.Resources.LoginPage_ErrorText;
                this.ViewModel.InfoText = ex.ToString();
            }
            finally
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            this.cancellationTokenSource?.Cancel();
        }
    }
}
