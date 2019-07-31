﻿using DevOps.Avatars;
using DevOps.Plugin.Utility;
using DevPrompt.Api;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevOps.Plugin.UI.ViewModels
{
    internal class PullRequestPageVM : PropertyNotifier, IAvatarProvider, IDisposable
    {
        public PullRequestTab Tab { get; }

        private ObservableCollection<AccountVM> accounts;
        private ObservableCollection<ProjectReferenceVM> projects;
        private ObservableCollection<PullRequestVM> pullRequests;
        private AzureDevOpsClient accountClient;
        private AccountVM currentAccount;
        private ProjectReferenceVM currentProject;
        private AzureDevOpsUserContext devopsUserContext;

        // Avatars
        private Dictionary<Uri, ImageSource> avatars;
        private Dictionary<Uri, List<IAvatarSite>> pendingAvatars;
        private HttpClient avatarHttpClient;

        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;

        public PullRequestPageVM(PullRequestTab tab, AzureDevOpsUserContext userContext)
        {
            this.devopsUserContext = userContext;
            this.Tab = tab;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.accounts = new ObservableCollection<AccountVM>(userContext.Accounts.OrderBy(a => a.AccountName).Select(a => new AccountVM(a)));
            this.projects = new ObservableCollection<ProjectReferenceVM>();
            this.pullRequests = new ObservableCollection<PullRequestVM>();
            this.currentAccount = new AccountVM(null);
            this.currentProject = new ProjectReferenceVM(null);

            this.avatars = new Dictionary<Uri, ImageSource>();
            this.pendingAvatars = new Dictionary<Uri, List<IAvatarSite>>();

            // Cookie container is used so we can get caching on avatar images
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            this.avatarHttpClient = new HttpClient(handler);
            this.avatarHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userContext.AuthenticationResult.AccessToken);
            this.avatarHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
        }

        public void Dispose()
        {
            this.disposed = true;

            this.Cancel();
            this.avatarHttpClient.Dispose();
        }

        public IList<AccountVM> Accounts => this.accounts;
        public IList<ProjectReferenceVM> Projects => this.projects;
        public bool HasProjects => this.CurrentAccount?.Account != null && this.Projects.Count > 0;
        public IList<PullRequestVM> PullRequests => this.pullRequests;
        public IWindow Window => this.Tab.Window;

        public void OnLoaded()
        {
            this.Refresh();
        }

        public void OnUnloaded()
        {
            if (!this.disposed)
            {
                this.Cancel();
            }
        }

        private void Cancel()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();

            this.accountClient?.Dispose();
            this.accountClient = null;

            this.avatarHttpClient.CancelPendingRequests();

            if (!this.disposed)
            {
                this.cancellationTokenSource = new CancellationTokenSource();
            }
        }

        public AccountVM CurrentAccount
        {
            get => this.currentAccount;
            set
            {
                if (this.SetPropertyValue(ref this.currentAccount, value))
                {
                    this.OnPropertyChanged(nameof(this.HasProjects));
                    this.UpdateProjects(this.currentAccount);
                }
            }
        }

        public ProjectReferenceVM CurrentProject
        {
            get => this.currentProject;
            set
            {
                if (this.SetPropertyValue(ref this.currentProject, value))
                {
                    this.UpdatePullRequests(this.currentProject?.Project);
                }
            }
        }

        private void Refresh()
        {
            if (this.currentProject != null)
            {
                this.UpdatePullRequests(this.currentProject.Project);
            }
        }

        private async void UpdateProjects(AccountVM account)
        {
            this.CurrentProject = null;

            this.accountClient = new AzureDevOpsClient(account.Account.AccountUri, this.devopsUserContext);

            using (this.Window.ProgressBar.Begin(this.cancellationTokenSource.Cancel, "Getting projects"))
            {
                IEnumerable<TeamProjectReference> projects = Array.Empty<TeamProjectReference>();
                try
                {
                    projects = await this.accountClient.GetProjectsAsync(this.cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    this.Window.InfoBar.SetError(ex);
                }

                this.projects.Clear();

                foreach (TeamProjectReference project in projects.OrderBy(p => p.Name))
                {
                    this.projects.Add(new ProjectReferenceVM(project));
                }

                this.OnPropertyChanged(nameof(this.HasProjects));
            }
        }

        private async void UpdatePullRequests(TeamProjectReference project)
        {
            if (project == null)
            {
                this.pullRequests.Clear();
            }
            else if (this.accountClient != null)
            {
                using (this.Window.ProgressBar.Begin(this.cancellationTokenSource.Cancel, "Getting pull requests"))
                {
                    GitPullRequestSearchCriteria search = new GitPullRequestSearchCriteria()
                    {
                        Status = PullRequestStatus.Active,
                    };

                    try
                    {
                        Tuple<Uri, List<GitPullRequest>> pullRequests = await this.accountClient.GetPullRequests(project.Name, search, this.cancellationTokenSource.Token);
                        this.UpdatePullRequests(project, pullRequests.Item1, pullRequests.Item2);
                    }
                    catch (Exception ex)
                    {
                        this.Window.InfoBar.SetError(ex);
                    }
                }
            }
        }

        private void ClearFailedAvatarDownloads()
        {
            foreach (KeyValuePair<Uri, ImageSource> pair in this.avatars.ToArray())
            {
                if (pair.Value == null)
                {
                    this.avatars.Remove(pair.Key);
                }
            }
        }

        private void UpdatePullRequests(TeamProjectReference project, Uri baseAddress, IReadOnlyList<GitPullRequest> newPullRequests)
        {
            this.ClearFailedAvatarDownloads();

            for (int i = 0; i < newPullRequests.Count; i++)
            {
                if (this.pullRequests.Count > i)
                {
                    this.pullRequests[i].BaseAddress = baseAddress;
                    this.pullRequests[i].GitPullRequest = newPullRequests[i];
                }
                else
                {
                    this.pullRequests.Add(new PullRequestVM(baseAddress, newPullRequests[i], this, this.Window));
                }
            }

            while (this.pullRequests.Count > newPullRequests.Count)
            {
                this.pullRequests.RemoveAt(this.pullRequests.Count - 1);
            }
        }

        async void IAvatarProvider.ProvideAvatar(Uri uri, IAvatarSite site)
        {
            if (this.avatars.TryGetValue(uri, out ImageSource image))
            {
                site.AvatarImageSource = image;
                return;
            }

            if (!this.pendingAvatars.TryGetValue(uri, out List<IAvatarSite> pendingSites))
            {
                pendingSites = new List<IAvatarSite>();
                this.pendingAvatars[uri] = pendingSites;
            }

            if (!pendingSites.Contains(site))
            {
                pendingSites.Add(site);

                try
                {
                    HttpResponseMessage response = await this.avatarHttpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, this.cancellationTokenSource.Token);
                    response = response.EnsureSuccessStatusCode();

                    Stream stream = await response.Content.ReadAsStreamAsync();
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    image = bitmap;
                    this.avatars[uri] = image;
                }
                catch
                {
                    // oh well, the image won't show
                    this.avatars[uri] = null;
                }
                finally
                {
                    site.AvatarImageSource = image;

                    if (pendingSites.Remove(site) && pendingSites.Count == 0)
                    {
                        this.pendingAvatars.Remove(uri);
                    }
                }
            }
        }
    }
}
