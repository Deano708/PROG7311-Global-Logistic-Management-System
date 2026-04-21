using Microsoft.EntityFrameworkCore;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;

namespace PROG7311GLMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<GlmsContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); //?? throw new InvalidOperationException("Connection string 'GlmsContext' not found.")));

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            // Register application services
            builder.Services.AddScoped<PROG7311GLMS.Service.ILogisticsFacade, PROG7311GLMS.Service.LogisticsFacade>();

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<ILogisticsFacade, LogisticsFacade>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            // Serve static files (wwwroot) so CSS/JS and uploaded files are available
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
