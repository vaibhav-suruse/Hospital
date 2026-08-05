using Microsoft.AspNetCore.Mvc;

namespace WebApplicationSampleTest2.Controllers
{
    // ─────────────────────────────────────────────────────────────────────
    // The old ad-hoc "Doctor/Nurse charge log" Billing Sheet has been
    // replaced by the real-time, item-based IPD Billing engine (the same
    // one OPD Billing uses) — see IPDBillingController.GenerateBill.
    //
    // This controller is kept only as a redirect shim so any old bookmarks
    // or links to /BillingSheet/Index?ipdId=... still land somewhere useful
    // instead of 404-ing. No data or logic lives here anymore; the
    // ipd_billing_sheet table and IBillingSheet/BillingSheetRepository are
    // left untouched in case the two historical test entries are ever
    // needed for reference.
    // ─────────────────────────────────────────────────────────────────────
    public class BillingSheetController : Controller
    {
        [HttpGet]
        public IActionResult Index(int ipdId)
        {
            return RedirectToAction("GenerateBill", "IPDBilling", new { ipdId });
        }
    }
}
