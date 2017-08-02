using ServiceStack;
using ServiceStack.Templates;

namespace TemplatePages
{
    [Route("/customers/{Id}")]
    public class ViewCustomer
    {
        public string Id { get; set; }
    }

    public class CustomerServices : Service
    {
        public ITemplatePages Pages { get; set; }

        public object Any(ViewCustomer request) =>
            new PageResult(Pages.GetPage("examples/customer")) {
                Model = TemplateQueryData.GetCustomer(request.Id)
            };
    }
}