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
        public System.Data.IDbConnection Db { get; set; }
        public object dbSelect(string sql) => Db.Select<Dictionary<string,object>>(sql);
        public object dbSingle(string sql) => Db.Single<Dictionary<string,object>>(sql);
        public object dbScalar(string sql) => Db.Scalar<object>(sql);
    }

    //[Authenticate] // friends don't let friends deploy to production without this
    public class QueryDbServices : Service
    {
        public object Any(QueryDb request)
        {
            var context = new TemplateContext {
                TemplateFilters = { new QueryDbTemplateFilters { Db = Db } },
                RenderExpressionExceptions = true
            }.Init();

            context.VirtualFiles.WriteFile("page.html", request.Page);

            return new PageResult(context.GetPage("page"));
        }
    }
}