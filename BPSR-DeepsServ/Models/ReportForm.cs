using Microsoft.AspNetCore.Mvc;

namespace BPSR_DeepsServ.Models
{
    public class ReportForm
    {
        [FromForm]
        public EncounterReport Report { get; set; }
        [FromForm]
        public IFormFile ReportImg { get; set; }
    }
}
