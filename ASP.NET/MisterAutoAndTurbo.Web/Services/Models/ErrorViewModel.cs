namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// Data passed to the shared Error view; ShowRequestId is true when a request id exists.
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
