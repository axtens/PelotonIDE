
namespace PelotonIDE
{
    public class App : Application
    {
        public static Window? _window;
        public static IHost? Host { get; private set; }
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            IApplicationBuilder builder = this.CreateBuilder(args)

                // Add navigation support for toolkit controls such as TabBar and NavigationView
                .UseToolkitNavigation()
                .Configure(host => host
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
                    .UseLogging(configure: (context, logBuilder) =>
                    {
                        // Configure log levels for different categories of logging
                        logBuilder.SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Trace);
                    }, enableUnoLogging: true)
                    .UseConfiguration(configure: configBuilder =>
                        configBuilder
                            .EmbeddedSource<App>()
                            .Section<AppConfig>()
                    )
                    // Enable localization (see appsettings.json for supported languages)
                    .UseLocalization()
                    // Register Json serializers (ISerializer and ISerializer)
                    .UseSerialization((context, services) => services
                        .AddContentSerializer(context)
                        .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                    .UseHttp((context, services) => services
                            // Register HttpClient
#if DEBUG
						// DelegatingHandler will be automatically injected into Refit Client
						.AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
                            .AddSingleton<IWeatherCache, WeatherCache>()
                            .AddRefitClient<IApiClient>(context))
                    .ConfigureServices((context, services) =>
                    {
                        // TODO: Register your services
                        //services.AddSingleton<IMyService, MyService>();
                    })
                    .UseNavigation(RegisterRoutes)
                );
            //builder.App.DebugSettings.IsTextPerformanceVisualizationEnabled = true;
            //builder.App.DebugSettings.IsXamlResourceReferenceTracingEnabled = true;
            //builder.App.DebugSettings.EnableFrameRateCounter = true;
            //builder.App.DebugSettings.FailFastOnErrors = true;
            _window = builder.Window;
            _window.Title = "Peloton IDE";
            _window.SizeChanged += Window_SizeChanged;
            Host = await builder.NavigateAsync<Shell>();
        }
        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            Window me = (Window)sender;
            double height = args.Size.Height;
            double width = args.Size.Width;
            Telemetry.Transmit(me.Title, "WindowSizeChangedEventArgs.Size.Height=", height, "WindowSizeChangedEventArgs.Size.Width=", width);
        }
        private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
        {
            views.Register(
                new ViewMap(ViewModel: typeof(ShellViewModel)),
                new ViewMap<MainPage, MainViewModel>(),
                new DataViewMap<IDEConfigPage, IDEConfigViewModel, Entity>(),
                new DataViewMap<TranslatePage, TranslateViewModel, Entity>()
            );

            routes.Register(
                new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                    Nested: new RouteMap[]
                    {
                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
                    new RouteMap("Second", View: views.FindByViewModel<IDEConfigViewModel>()),
                    new RouteMap("Translate", View: views.FindByViewModel<TranslateViewModel>()),
                    }
                )
            );
        }
    }
}