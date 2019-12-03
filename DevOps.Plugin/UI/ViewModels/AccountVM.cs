using DevPrompt.Api.Utility;
using Microsoft.VisualStudio.Services.Account;

namespace DevOps.Plugin.UI.ViewModels
{
    internal class AccountVM : PropertyNotifier
    {
        public Account Account { get; }

        public AccountVM(Account account)
        {
            this.Account = account;
        }

        public string Name => this.Account?.AccountName ?? "(no account selected)";
    }
}
