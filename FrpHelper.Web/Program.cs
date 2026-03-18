using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using FrpHelper.Web.Configuration;
using FrpHelper.Web;
using FrpHelper.Web.Services.Auth;
using FrpHelper.Web.Services.Archive;
using FrpHelper.Web.Services.ClientStorage;
using FrpHelper.Web.Services.Export;
using FrpHelper.Web.Services.Parsing;
using FrpHelper.Web.Services.Permissions;
using FrpHelper.Web.Services.Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var supabaseOptions = new SupabaseOptions();
builder.Configuration.GetSection(SupabaseOptions.SectionName).Bind(supabaseOptions);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IOptions<SupabaseOptions>>(Options.Create(supabaseOptions));
builder.Services.AddScoped<IClientStorageService, ClientStorageService>();
builder.Services.AddScoped<IAuthService, SupabaseAuthService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<IArchiveService, ArchiveService>();
builder.Services.AddScoped<IReportParserService, ReportParserService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<ISupabaseReportService, SupabaseReportService>();

await builder.Build().RunAsync();
