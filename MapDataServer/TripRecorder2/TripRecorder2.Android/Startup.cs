using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using TripRecorder2.Droid.Services;
using TripRecorder2.Services;

namespace TripRecorder2.Droid
{
    public static class StartupExtensions
    {
        private class InMemoryFileProvider : IFileProvider
        {
            private class InMemoryFile : IFileInfo
            {
                private readonly byte[] _data;
                public InMemoryFile(string json) => _data = Encoding.UTF8.GetBytes(json);
                public Stream CreateReadStream() => new MemoryStream(_data);
                public bool Exists { get; } = true;
                public long Length => _data.Length;
                public string PhysicalPath { get; } = string.Empty;
                public string Name { get; } = string.Empty;
                public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;
                public bool IsDirectory { get; } = false;
            }

            private readonly IFileInfo _fileInfo;
            public InMemoryFileProvider(string json) => _fileInfo = new InMemoryFile(json);
            public IFileInfo GetFileInfo(string _) => _fileInfo;
            public IDirectoryContents GetDirectoryContents(string _) => null;
            public IChangeToken Watch(string _) => NullChangeToken.Singleton;
        }

        public static IConfigurationBuilder AddXamarinJsonFile(this IConfigurationBuilder builder, string filename)
        {
            var context = Application.Context;
            string json;

            using (var asset = context.Assets.Open(filename))
            {
                using (var reader = new StreamReader(asset))
                {
                    json = reader.ReadToEnd();
                }
            }

            InMemoryFileProvider fileProvider = new InMemoryFileProvider(json);
            return builder.AddJsonFile(fileProvider, filename, false, false);
        }
    }

    public class Startup : AppStartup
    {
        private MainActivity Activity { get; set; }
        private Context Context { get; set; }

        public Startup(MainActivity activity, Context context)
        {
            Activity = activity;
            Context = context;
        }

        protected override void InitializePlatformServices(ContainerBuilder builder)
        {
            var config = BuildConfiguration();
            builder.RegisterInstance(Activity);
            builder.RegisterInstance(Context);
            builder.RegisterType<LocationProvider>().As<ILocationProvider>();
            builder.RegisterType<LocationTracker>();
            builder.RegisterInstance(config);
        }

        private IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                    .AddXamarinJsonFile("appsettings.json")
                    .AddXamarinJsonFile("secrets.json");
            return builder.Build();
        }
    }
}