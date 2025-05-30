namespace Simapd.Models
{
  class ErrorResponse
  {
    public int StatusCode { get; set; }
    public string Message { get; set; }

    public ErrorResponse(int statusCode, string message) {
      this.StatusCode = statusCode;
      this.Message = message;
    }
  }
}
