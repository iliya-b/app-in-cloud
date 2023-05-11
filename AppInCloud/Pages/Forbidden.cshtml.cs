using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppInCloud.Pages
{
    public class ForbiddenModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "Forbidden page.";
        }
    }
}
