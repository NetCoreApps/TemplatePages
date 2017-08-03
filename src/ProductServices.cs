using ServiceStack;
using ServiceStack.Templates;

namespace TemplatePages
{
    [Route("/products/view")]
    public class ViewProducts
    {
        public string Id { get; set; }
    }

    public class ProductsServices : Service
    {
        public ITemplatePages Pages { get; set; }

        public object Any(ViewProducts request) =>
            new PageResult(Pages.GetCodePage("products")) {
                Args = {
                    ["products"] = TemplateQueryData.Products
                }
            };
    }
}