using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyRpc.AspNetCore;
using EasyRpc.AspNetCore.Documentation;
using EasyRpc.TestApp.Services;
using EasyRpc.TestApp.Utilities;
using Grace.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace EasyRpc.TestApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddJsonRpc(c => c.DebugLogging = false);
            services.AddSingleton<IWebAssetProvider, CustomWebAssetProvider>();
        }

        public void ConfigureContainer(IInjectionScope scope)
        {
            var func = new Func<string, LogLevel, bool>((s, level) =>
            {
                return false;
            });
            scope.Configure(c => c.ExportInstance(func));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                //app.UseBrowserLink();
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Error");
            }

            app.UseJsonRpc("/service-api/", api =>
            {
                api.ExposeAssemblyContaining<Startup>().Where(type => type.Namespace.EndsWith(".Services"));
            });

            app.Use((context, next) =>
            {
                var response = context.Response;

                response.StatusCode = 301;
                response.Headers[HeaderNames.Location] = "/service-api/";

                return Task.CompletedTask;
            });
        }
    }
}
