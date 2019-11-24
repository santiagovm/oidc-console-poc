using Common.SampleConstants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore()
                    .AddAuthorization()
                    .AddJsonFormatters();

            services.AddAuthentication("Bearer")
                    .AddJwtBearer("Bearer", options =>
                                            {
                                                options.Authority = SampleUrls.SecurityTokenService;
                                                options.RequireHttpsMetadata = false;
                                                options.Audience = SampleScopes.TestApi;
                                            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseAuthentication();

            app.UseMvc();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
