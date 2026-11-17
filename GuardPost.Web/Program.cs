using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Web.Hubs;
using GuardPost.Web.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (Dependency Injection container) ----
builder.Services.AddApplicationDbContext(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddApplicationServices();

// AJAX calls send the anti-forgery token in this header (see wwwroot/js/site.js).
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddControllersWithViews(options =>
{
    // Every POST/PUT/DELETE form must carry an anti-forgery token (CSRF protection).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Bulgarian texts for the built-in model-binding errors.
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => string.Format(ValidationMessages.ValueIsInvalid, value, field));
    messages.SetValueIsInvalidAccessor(value => string.Format(ValidationMessages.ValueIsInvalid, value, string.Empty));
    messages.SetValueMustBeANumberAccessor(field => string.Format(ValidationMessages.ValueMustBeANumber, field));
    messages.SetValueMustNotBeNullAccessor(_ => ValidationMessages.ValueRequired);
    messages.SetMissingKeyOrValueAccessor(() => ValidationMessages.ValueRequired);
});

builder.Services.AddSignalR();

var app = builder.Build();

// ---- HTTP request pipeline ----
// Bulgarian culture – month and day names, number formatting.
var culture = new CultureInfo("bg-BG");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture },
});

// Custom 500 page. The detailed developer page is shown only when explicitly enabled.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("ErrorHandling:ShowDetailedErrors", false))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
}

// Custom 404 / 403 / 400 pages.
app.UseStatusCodePagesWithReExecute("/Home/HttpError", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationsHub>(NotificationsHub.Path);

await app.PrepareDatabaseAsync();

await app.RunAsync();
