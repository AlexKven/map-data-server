using Autofac;
using System;
using System.Collections.Generic;
using System.Text;
using TripRecorder2.Services;
using TripRecorder2.ViewModels;

namespace TripRecorder2
{
    public abstract class AppStartup
    {
        public static IContainer Container
        {
            get;
            private set;
        }

        protected abstract void InitializePlatformServices(ContainerBuilder builder);

        public void InitializeServices()
        {
            var builder = new ContainerBuilder();
            InitializePlatformServices(builder);

            builder.Register(cc => Container).As<IContainer>();
            builder.RegisterType<TripPagePointsListService>().SingleInstance();
            builder.RegisterType<TripPageViewModel>().SingleInstance();
            builder.RegisterType<TripSettingsProvider>().SingleInstance();

            try
            {
                Container = builder.Build();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
