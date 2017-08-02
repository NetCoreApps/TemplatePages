using System;
using System.Threading.Tasks;
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
using System.IO;

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
            var customFilters = new CustomTemplateFilters();
            Plugins.Add(new TemplatePagesFeature {
                TemplateFilters = { customFilters }
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

    public class CustomTemplateFilters : TemplateFilter
    {
        public Dictionary<int, KeyValuePair<string, string>> DocsIndex { get; } = new Dictionary<int, KeyValuePair<string, string>>();
        public Dictionary<int, KeyValuePair<string, string>> LinqIndex { get; } = new Dictionary<int, KeyValuePair<string, string>>();

        public object prevDocLink(int order)
        {
            if (DocsIndex.TryGetValue(order - 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object nextDocLink(int order)
        {
            if (DocsIndex.TryGetValue(order + 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object prevLinqLink(int order)
        {
            if (LinqIndex.TryGetValue(order - 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object nextLinqLink(int order)
        {
            if (LinqIndex.TryGetValue(order + 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        List<KeyValuePair<string,string>> sortedDocLinks;
        public object docLinks() => sortedDocLinks ?? (sortedDocLinks = sortLinks(DocsIndex));

        List<KeyValuePair<string,string>> sortedLinqLinks;
        public object linqLinks() => sortedLinqLinks ?? (sortedLinqLinks = sortLinks(LinqIndex));

        public List<KeyValuePair<string,string>> sortLinks(Dictionary<int, KeyValuePair<string,string>> links)
        {
            var sortedKeys = links.Keys.ToList();
            sortedKeys.Sort();

            var to = new List<KeyValuePair<string,string>>();

            foreach (var key in sortedKeys)
            {
                var entry = links[key];
                to.Add(entry);
            }

            return to;
        }

        public async Task includeContentFile(TemplateScopeContext scope, string virtualPath)
        {
            var file = HostContext.VirtualFiles.GetFile(virtualPath);
            if (file == null)
                throw new FileNotFoundException($"includeContentFile '{virtualPath}' was not found");

            using (var reader = file.OpenRead())
            {
                await reader.CopyToAsync(scope.OutputStream);
            }
        }

        public List<Customer> customers() => TemplateQueryData.Customers;
    }
}
