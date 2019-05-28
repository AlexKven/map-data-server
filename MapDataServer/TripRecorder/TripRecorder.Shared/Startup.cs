using Autofac;
using System;
using System.Collections.Generic;
using System.Text;
using TripRecorder.ViewModels;

namespace TripRecorder
{
    public partial class Startup
    {
        public static IContainer Container
        {
            get;
            private set;
        }

        partial void InitializePlatformServices(ContainerBuilder builder);

        public void InitializeServices()
        {
            var builder = new ContainerBuilder();
            InitializePlatformServices(builder);

            builder.Register(cc => Container).As<IContainer>();
            builder.RegisterType<MainPageViewModel>();

            Container = builder.Build();
        }
    }
}
