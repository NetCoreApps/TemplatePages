using System.Linq;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Templates;

namespace TemplatePages
{
    [Page("products")]
    [PageArg("title", "Products")]
    public class ProductsPage : TemplateCodePage
    {
        string render(Product[] products) => $@"
        <table class='table table-striped'>
            <thead>
                <tr>
                    <th></th>
                    <th>Name</th>
                    <th>Price</th>
                </tr>
            </thead>
            {products.OrderBy(x => x.Category).ThenBy(x => x.ProductName).Map(x => $@"
                <tr>
                    <th>{x.Category}</th>
                    <td>{x.ProductName.HtmlEncode()}</td>
                    <td>{x.UnitPrice:C}</td>
                </tr>").Join("")}
        </table>";
    }
}