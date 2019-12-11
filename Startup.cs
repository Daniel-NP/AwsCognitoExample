using AwsCognitoExample.Services;
using AwsCognitoExample.ViewModel;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AwsCognitoExample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<TestViewModel>();
            services.AddScoped<AuthenticationServiceOnBlazor>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
