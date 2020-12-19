using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UtiliSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Main.InitialiseAsync().GetAwaiter().GetResult();

            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(30);
                options.IncludeSubDomains = false;
            });

            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/dashboard", "dashboard/{guild?}");
                options.Conventions.AddPageRoute("/dashboard/autopurge", "dashboard/{guild?}/autopurge");
                options.Conventions.AddPageRoute("/dashboard/core", "dashboard/{guild?}/core");
                options.Conventions.AddPageRoute("/dashboard/channelmirroring", "dashboard/{guild?}/channelmirroring");
                options.Conventions.AddPageRoute("/dashboard/inactiverole", "dashboard/{guild?}/inactiverole");
                options.Conventions.AddPageRoute("/dashboard/joinmessage", "dashboard/{guild?}/joinmessage");
                options.Conventions.AddPageRoute("/dashboard/messagefilter", "dashboard/{guild?}/messagefilter");
                options.Conventions.AddPageRoute("/dashboard/messagelogs", "dashboard/{guild?}/messagelogs");
                options.Conventions.AddPageRoute("/dashboard/notices", "dashboard/{guild?}/notices");
                options.Conventions.AddPageRoute("/dashboard/reputation", "dashboard/{guild?}/reputation");
                options.Conventions.AddPageRoute("/dashboard/roles", "dashboard/{guild?}/roles");
                options.Conventions.AddPageRoute("/dashboard/voicelink", "dashboard/{guild?}/voicelink");
                options.Conventions.AddPageRoute("/dashboard/voiceroles", "dashboard/{guild?}/voiceroles");
                options.Conventions.AddPageRoute("/dashboard/votechannels", "dashboard/{guild?}/votechannels");
            });

            services.AddAuthentication().AddCookie();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddDiscord(options =>
                {
                    options.ClientId = Main._config.DiscordClientId;
                    options.ClientSecret = Main._config.DiscordClientSecret;
                    options.AccessDeniedPath = "/";
                    options.Scope.Add("email");
                    options.Scope.Add("guilds");
                    options.SaveTokens = true;
                    options.ClaimActions.MapAll();
                });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            else
            {
                app.UseHsts(); // TODO: Figure out why this isn't working properly
                app.UseExceptionHandler("/Error");
            }
            app.UseMiddleware<ErrorLoggingMiddleware>();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next();
            });
            
            app.UseResponseCompression();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                HttpsCompression = Microsoft.AspNetCore.Http.Features.HttpsCompressionMode.Compress,
                OnPrepareResponse = ctx =>
                {
                    // Cache static files for 30 days
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
                    ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddDays(30).ToString("R", CultureInfo.InvariantCulture));
                }
            });
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
