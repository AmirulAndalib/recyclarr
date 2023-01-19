using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Cache;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.Processors;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Radarr;
using Recyclarr.TrashLib.Services.ReleaseProfile;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.System;
using Recyclarr.TrashLib.Startup;
using Serilog;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

public static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.FullName?.StartsWithIgnoreCase("Recyclarr") ?? false)
            .ToArray();

        RegisterAppPaths(builder);
        RegisterLogger(builder);

        builder.RegisterModule<SonarrAutofacModule>();
        builder.RegisterModule<RadarrAutofacModule>();
        builder.RegisterModule<QualitySizeAutofacModule>();
        builder.RegisterModule<CustomFormatAutofacModule>();
        builder.RegisterModule<ReleaseProfileAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<MigrationAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<SystemServiceAutofacModule>();
        builder.RegisterModule(new ConfigAutofacModule(assemblies));
        builder.RegisterModule<ServiceProcessorsAutofacModule>();
        builder.RegisterModule(new CommonAutofacModule(Assembly.GetExecutingAssembly()));

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterModule<CacheAutofacModule>();
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<ServiceRequestBuilder>().As<IServiceRequestBuilder>();

        CommandRegistrations(builder);

        builder.RegisterAutoMapper(false, assemblies);

        builder.RegisterType<FlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        builder.RegisterType<LogJanitor>().As<ILogJanitor>();
        builder.RegisterType<LoggerFactory>();
        builder.Register(c => c.Resolve<LoggerFactory>().Create()).As<ILogger>().SingleInstance();
    }

    private static void RegisterAppPaths(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<DefaultAppDataSetup>();

        builder.Register(c =>
            {
                var appData = c.Resolve<AppDataPathProvider>();
                var dataSetup = c.Resolve<DefaultAppDataSetup>();
                return dataSetup.CreateAppPaths(appData.AppDataPath);
            })
            .As<IAppPaths>()
            .SingleInstance();
    }

    private static void CommandRegistrations(ContainerBuilder builder)
    {
        builder.RegisterTypes(
                typeof(AppPathSetupTask),
                typeof(JanitorCleanupTask))
            .As<IBaseCommandSetupTask>()
            .OrderByRegistration();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<CommandSettings>();
    }
}
