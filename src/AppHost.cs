using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using Funq;

namespace TemplatePages
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Template Pages", typeof(TemplateServices).Assembly) { }

        public TemplateContext LinqContext;

        public override void Configure(Container container)
        {
            ServiceStack.Memory.NetCoreMemory.Configure();

            SetConfig(new HostConfig { 
                DebugMode = AppSettings.Get("DebugMode", Env.IsWindows),
            });

            Plugins.Add(new ServiceStack.Api.OpenApi.OpenApiFeature());

            var path = MapProjectPath("~/wwwroot/assets/js/customers.json");
            var json = File.ReadAllText(path);
            TemplateQueryData.Customers = json.FromJson<List<Customer>>();

            container.Register<ICustomers>(c => new Customers(TemplateQueryData.Customers));
            container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTable<Order>();
                db.CreateTable<Customer>();
                db.CreateTable<Product>();
                TemplateQueryData.Customers.Each(x => db.Save(x, references:true));
                db.InsertAll(TemplateQueryData.Products);

                db.CreateTable<Quote>();
                db.Insert(new Quote { Id = 1, Url = "https://gist.githubusercontent.com/gistlyn/0ab7494dfbff78466ef622d501662027/raw/b3dd1e9f8e82c169a32829071aa7f761c6494843/quote.md" });
            }

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

            Plugins.Add(new AutoQueryDataFeature { MaxLimit = 100 }
                .AddDataSource(ctx => ctx.ServiceSource<GithubRepo>(ctx.Dto.ConvertTo<GetGithubRepos>(), 
                    HostContext.Cache, TimeSpan.FromMinutes(10)))
            );

            var customFilters = new CustomTemplateFilters();
            Plugins.Add(new TemplatePagesFeature {
                TemplateFilters = { 
                    customFilters,
                    new TemplateDbFiltersAsync()
                },
                Args = {
                    ["products"] = TemplateQueryData.Products
                },
                RenderExpressionExceptions = true,
                MetadataDebugAdminRole = RoleNames.AllowAnon,
                ExcludeFiltersNamed = { "dbExec" }
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

    public class Quote
    {
        public int Id { get; set; }
        public string Url { get; set; }
    }
}
