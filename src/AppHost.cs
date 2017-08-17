using Funq;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Templates;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ServiceStack.Text;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;

namespace TemplatePages
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("ServiceStack Template Pages", typeof(TemplateServices).GetAssembly()) { }

        public TemplateContext LinqContext;

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig { 
                DebugMode = AppSettings.Get("DebugMode", !Env.IsWindows),
                AdminAuthSecret = Environment.GetEnvironmentVariable("AUTH_SECRET")
            });

            Plugins.Add(new ServiceStack.Api.OpenApi.OpenApiFeature());

            container.Register<ICustomers>(c => new Customers(TemplateQueryData.Customers));
            container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTable<Order>();
                db.CreateTable<Customer>();
                db.CreateTable<Product>();
                TemplateQueryData.Customers.Each(x => db.Save(x, references:true));
                db.InsertAll(TemplateQueryData.Products);
            }

            var customFilters = new CustomTemplateFilters();
            Plugins.Add(new TemplatePagesFeature {
                TemplateFilters = { customFilters },
                Args = {
                    ["products"] = TemplateQueryData.Products
                },
                RenderExpressionExceptions = true,
                EnableDebugTemplate = true,
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
