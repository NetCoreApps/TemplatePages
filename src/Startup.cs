using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Funq;
using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.VirtualPath;
using System.Net;
using System.Web;
using ServiceStack.Templates;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ServiceStack.Web;
using ServiceStack.Text;

namespace TemplatePages
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost());
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("ServiceStack Template Pages", typeof(TemplateServices).GetAssembly()) { }

        public TemplateContext LinqContext;

        public override void Configure(Container container)
        {
            container.Register<ICustomers>(c => new Customers(TemplateQueryData.Customers));

            var customFilters = new CustomTemplateFilters();
            Plugins.Add(new TemplatePagesFeature {
                TemplateFilters = { customFilters },
                Args = {
                    ["products"] = TemplateQueryData.Products
                },
                RenderExpressionExceptions = true
            });

            AfterInitCallbacks.Add(host => {
                var feature = GetPlugin<TemplatePagesFeature>();

                var files = GetVirtualFileSources().First(x => x is FileSystemVirtualFiles);
                foreach (var file in files.GetDirectory("docs").GetAllMatchingFiles("*.html"))
                {
                    var page = feature.GetPage(file.VirtualPath).Init().Result;
                    if (page.Args.TryGetValue("order", out object order) && page.Args.TryGetValue("title", out object title))
                    {
                        customFilters.DocsIndex[int.Parse((string)order)] = new KeyValuePair<string,string>(GetPath(file.VirtualPath), (string)title);
                    }
                }

                foreach (var file in files.GetDirectory("linq").GetAllMatchingFiles("*.html"))
                {
                    var page = feature.GetPage(file.VirtualPath).Init().Result;
                    if (page.Args.TryGetValue("order", out object order) && page.Args.TryGetValue("title", out object title))
                    {
                        customFilters.LinqIndex[int.Parse((string)order)] = new KeyValuePair<string,string>(GetPath(file.VirtualPath), (string)title);
                    }
                }

                foreach (var file in files.GetDirectory("usecases").GetAllMatchingFiles("*.html"))
                {
                    var page = feature.GetPage(file.VirtualPath).Init().Result;
                    if (page.Args.TryGetValue("order", out object order) && page.Args.TryGetValue("title", out object title))
                    {
                        customFilters.UseCasesIndex[int.Parse((string)order)] = new KeyValuePair<string,string>(GetPath(file.VirtualPath), (string)title);
                    }
                }

                LinqContext = new TemplateContext {
                    Args = {
                        [TemplateConstants.DefaultDateFormat] = "yyyy/MM/dd",
                        ["products"] = TemplateQueryData.Products,
                        ["customers"] = TemplateQueryData.Customers,
                        ["comparer"] = new CaseInsensitiveComparer(),
                        ["anagramComparer"] = new AnagramEqualityComparer(),
                    }
                }.Init();
            });
        }

        public string GetPath(string virtualPath)
        {
            var path = "/" + virtualPath.LastLeftPart('.');
            if (path.EndsWith("/index"))
                path = path.Substring(0, path.Length - "index".Length);

            return path;
        }
    }
}
