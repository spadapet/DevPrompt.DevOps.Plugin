using DevPrompt.Api;
using DevPrompt.Api.Utility;

namespace DevOps.Plugin.UI.ViewModels
{
    internal class LoginPageVM : PropertyNotifier
    {
        public IWindow Window => this.tab.Window;
        public bool HasInfoText => !string.IsNullOrEmpty(this.InfoText);

        private readonly PullRequestTab tab;
        private string displayText;
        private string infoText;

        public LoginPageVM()
            : this(null)
        {
        }

        public LoginPageVM(PullRequestTab tab)
        {
            this.tab = tab;
            this.displayText = Resources.LoginPage_InitialText;
            this.infoText = string.Empty;
        }

        public string DisplayText
        {
            get => this.displayText;
            set => this.SetPropertyValue(ref this.displayText, value ?? string.Empty);
        }

        public string InfoText
        {
            get => this.infoText;
            set
            {
                if (this.SetPropertyValue(ref this.infoText, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.HasInfoText));
                }
            }
        }
    }
}
