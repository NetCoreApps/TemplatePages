using System.Collections.Generic;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace TemplatePages
{
    [Route("/db/query")]
    public class QueryDb
    {
        public string Page { get; set; }
    }

    public class QueryDbTemplateFilters : TemplateFilter
    {
        System.Data.IDbConnection db;
        public static QueryDbTemplateFilters Create(System.Data.IDbConnection db) => 
            new QueryDbTemplateFilters { db = db };

        public object dbSelect(string sql) => db.Select<Dictionary<string,object>>(sql);
        public object dbSingle(string sql) => db.Single<Dictionary<string,object>>(sql);
        public object dbScalar(string sql) => db.Scalar<object>(sql);
    }

    //[Authenticate] // friends don't let friends deploy to production without this
    public class QueryDbServices : Service
    {
        public object Any(QueryDb request)
        {
            var context = new TemplateContext {
                TemplateFilters = { QueryDbTemplateFilters.Create(Db) },
                RenderExpressionExceptions = true
            }.Init();

            context.VirtualFiles.WriteFile("page.html", request.Page);

            return new PageResult(context.GetPage("page"));
        }
    }
}