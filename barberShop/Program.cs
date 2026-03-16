using barberShop;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
*/
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Felhasznalo, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(7);
    options.SlidingExpiration = true;   //cookie expire reset minden bejelentkezésnél
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //https nél van csak cooki
});

builder.Services.Configure<EmailBeallitasok>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddScoped<IEmailKuldo, SmtpEmailKuldo>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapRazorPages();


using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        SeedAdatok.Initialize(context);
        var userManager = services.GetRequiredService<UserManager<Felhasznalo>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        const string adminRoleName = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRoleName))
            await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        const string fodraszRole = "Fodrasz";
        if (!await roleManager.RoleExistsAsync(fodraszRole))
            await roleManager.CreateAsync(new IdentityRole(fodraszRole));
        const string felhasznaloRole = "Mugli";
        if (!await roleManager.RoleExistsAsync(felhasznaloRole))
            await roleManager.CreateAsync(new IdentityRole(felhasznaloRole));
        var adminEmail = "kerberosz@kerberosz.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new Felhasznalo
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(adminUser, "%20kerberosz02%");
            if (createResult.Succeeded)
                await userManager.AddToRoleAsync(adminUser, adminRoleName);
            else
                services.GetRequiredService<ILogger<Program>>()
                    .LogError("Nem sikerült az admin létrehozása: {Errors}",
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }
        await SeedAdatok.SeedFodraszBejelentkezoekAsync(context, userManager);
    }
    catch (Exception ex)
    {
        app.Services.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Hiba Seed adatok inicializálásánál!");
    }
}
app.Run();
