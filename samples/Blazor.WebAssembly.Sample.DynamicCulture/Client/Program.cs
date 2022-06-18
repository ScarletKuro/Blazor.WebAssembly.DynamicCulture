using Blazor.WebAssembly.DynamicCulture.Extensions;
using Blazor.WebAssembly.Sample.DynamicCulture.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization(); //requires Microsoft.Extensions.Localization
builder.Services.AddLocalizationDynamic(options =>
{
    //I want to ignore setting the CultureInfo.CurrentCulture and set only CultureInfo.CurrentUICulture, because for example if I change CurrentCulture then NumberDecimalSeparator will change to comma, but I want it to be a dot from en-US
    options.IgnoreCulture = true;
    //Always set the default culture explicitly for WASM because for clients the default might be different than you expect  
    options.SetDefaultCulture("en-US");
    //Personally I specify only UI cultures, but not the AddSupportedCultures since I only want to change the UI looks, but if you also will use
    options.AddSupportedUICultures("ja", "ru");
});

var host = builder.Build();
await host.SetMiddlewareCulturesAsync(); //this will involve Blazor.WebAssembly.DynamicCulture
await host.RunAsync();