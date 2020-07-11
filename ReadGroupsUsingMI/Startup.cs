using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Based on https://stackoverflow.com/questions/48013011/msi-permissions-for-graph-api/48014153#48014153 and
//  https://www.rahulpnath.com/blog/how-to-authenticate-with-microsoft-graph-api-using-managed-service-identity/

/* PS script to set up permission:
# Get the MI id from the portal when assigning it to the service
$mi_id = '1820cf46-eb98-49d0-a4bb-a730d37899ac'

Connect-AzureAD -TenantId meraridom.com
$graph = Get-AzureADServicePrincipal -Filter "AppId eq '00000003-0000-0000-c000-000000000000'"
$groupReadPermission = $graph.AppRoles `
    | where Value -Like "Group.Read.All" `
    | Select-Object -First 1
$mi = Get-AzureADServicePrincipal -objectId $mi_id
New-AzureADServiceAppRoleAssignment -ObjectId $mi.objectId -Id $groupReadPermission.Id -PrincipalId $mi.ObjectId -ResourceId $graph.ObjectId

 * */

namespace ReadGroupsUsingMI
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
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
