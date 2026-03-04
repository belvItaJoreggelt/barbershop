using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using barberShop;

var builder = WebApplication.CreateBuilder(args);


/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
*/
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

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

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<AppDbContext>();

        //await context.Database.MigrateAsync();

        SeedAdatok.Initialize(context);

        var userManager = services.GetRequiredService<UserManager<Felhasznalo>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        const string adminRoleName = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        }

        const string fodraszRole = "Fodrasz";
        if (!await roleManager.RoleExistsAsync(fodraszRole))
        {
            await roleManager.CreateAsync(new IdentityRole(fodraszRole));
        }

        const string felhasznaloRole = "Mugli";
        if (!await roleManager.RoleExistsAsync(felhasznaloRole))
        {
            await roleManager.CreateAsync(new IdentityRole(felhasznaloRole));
        }
        var adminEmail = "kerberosz@kerberosz.com";
        // Megnézzük, van-e már ilyen e-mailű user (duplikátum elkerülés).
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
            {
                await userManager.AddToRoleAsync(adminUser, adminRoleName);
            }
            else
            {
                // Ha valami hiba volt (pl. gyenge jelszó), logoljuk a hibákat.
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError("Nem sikerült az admin felhasználó létrehozása: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }
        }

        var fodraszEmail = "szaszak@gmail.com";
        var fodraszUser = await userManager.FindByEmailAsync(fodraszEmail);
        if (fodraszUser == null)
        {
            
        }
    }
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Hiba Seed adatok inicializálásánál!");
	}
}
app.Run();
