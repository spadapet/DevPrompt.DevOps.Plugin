using DevPrompt.Api;
using System;
using System.Composition;
using System.IO;
using System.Reflection;

namespace DevOps.Plugin
{
    /// <summary>
    /// Stores global data for this plugin, only one will ever be created at a time.
    /// VSS connections are cached here so that they can be shared among views.
    /// </summary>
    [Export(typeof(IAppListener))]
    internal class Globals : IAppListener, IDisposable
    {
        public static Globals Instance { get; private set; }
        public IHttpClient HttpClient { get; }

        [ImportingConstructor]
        public Globals(IHttpClient httpClient)
        {
            Globals.Instance = this;
            this.HttpClient = httpClient;
        }

        public void Dispose()
        {
            Globals.Instance = null;
        }

        void IAppListener.OnStartup(IApp app)
        {
            AppDomain.CurrentDomain.AssemblyResolve += Globals.OnFailedAssemblyResolve;
        }

        void IAppListener.OnExit(IApp app)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Globals.OnFailedAssemblyResolve;
        }

        void IAppListener.OnOpened(IApp app, IWindow window)
        {
        }

        void IAppListener.OnClosing(IApp app, IWindow window)
        {
        }

        private static Assembly OnFailedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Globals.TryResolveAssembly(args.Name, "Newtonsoft.Json");
        }

        private static Assembly TryResolveAssembly(string fullName, string nameToCheck)
        {
            if (fullName.StartsWith(nameToCheck, StringComparison.OrdinalIgnoreCase))
            {
                AssemblyName name = new AssemblyName(fullName);

                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                path = Path.Combine(path, name.Name);
                if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    path += ".dll";
                }

                if (File.Exists(path))
                {
                    // Maybe another version has already been loaded
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.FullName.StartsWith(nameToCheck, StringComparison.OrdinalIgnoreCase))
                        {
                            AssemblyName otherName = assembly.GetName();
                            if (string.Equals(otherName.Name, name.Name, StringComparison.OrdinalIgnoreCase) &&
                                otherName.Version >= name.Version)
                            {
                                return assembly;
                            }
                        }
                    }

                    try
                    {
                        return Assembly.LoadFrom(path);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }
    }
}
