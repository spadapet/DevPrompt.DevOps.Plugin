using DevOps.Plugin.UI;
using DevPrompt.Api;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevOps.Plugin
{
    [Guid("527dd4ee-5150-4b3b-84d0-c81e548b7830")]
    internal sealed class PullRequestTab : PropertyNotifier, ITab, IDisposable
    {
        public IWindow Window { get; }
        private UIElement viewElement;

        public PullRequestTab(IWindow window)
        {
            this.Window = window;
        }

        public void Dispose()
        {
            this.ViewElement = null;
        }

        Guid ITab.Id => this.GetType().GUID;
        string ITab.Name => Resources.PullRequestsTabName;
        string ITab.Title => Resources.PullRequestsTabName;
        string ITab.Tooltip => string.Empty;
        ITabSnapshot ITab.Snapshot => null;
        IEnumerable<FrameworkElement> ITab.ContextMenuItems => null;

        public UIElement ViewElement
        {
            get
            {
                if (this.viewElement == null)
                {
                    this.viewElement = new LoginPage(this);
                }

                return this.viewElement;
            }

            set
            {
                if (this.viewElement != value)
                {
                    if (this.viewElement is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    this.viewElement = value;
                    this.OnPropertyChanged(nameof(this.ViewElement));
                }
            }
        }

        void ITab.Focus()
        {
            if (this.viewElement != null)
            {
                Action action = () => this.viewElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                this.viewElement.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
        }

        void ITab.OnShowing()
        {
        }

        void ITab.OnHiding()
        {
        }

        bool ITab.OnClosing()
        {
            return true;
        }

        void ITab.OnSetTabName()
        {
        }

        void ITab.OnClone()
        {
        }

        void ITab.OnDetach()
        {
        }
    }
}
