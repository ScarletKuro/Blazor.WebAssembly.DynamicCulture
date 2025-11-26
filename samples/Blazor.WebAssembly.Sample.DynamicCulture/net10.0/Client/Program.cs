using Blazor.WebAssembly.DynamicCulture.Extensions;
using Blazor.WebAssembly.Sample.DynamicCulture.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization(); //requires Microsoft.Extensions.Localization package
builder.Services.AddLocalizationDynamic(options =>
{
    //Always set the default culture explicitly for WASM because for clients the default might be different than you expect
    options.SetDefaultCulture("en-US");
    //Personally, I specify only UI cultures, but not the AddSupportedCultures since I only want to change the UI looks.
    //For this reason I also set the IgnoreCulture = true(read below)
    options.AddSupportedUICultures("ja", "ru");
    //I want to ignore setting the CultureInfo.CurrentCulture and set only CultureInfo.CurrentUICulture and CurrentCulture to be default(en-US).
    //Reason: for example, if I change CurrentCulture to Estonian then NumberDecimalSeparator will change to comma, but I want it to be a dot from en-US
    //If you want to be CurrentCulture to be the same as CurrentUICulture then do no set this, and also don't forget to specify AddSupportedCultures
    options.IgnoreCulture = true;
});

var host = builder.Build();
await host.SetMiddlewareCulturesAsync(); //this will involve Blazor.WebAssembly.DynamicCulture
await host.RunAsync();

//or await builder.Build().RunWithCultureMiddlewareAsync();