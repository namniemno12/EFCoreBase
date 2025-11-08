namespace MyProject.Domain.DTOs.Auth.Req
{
    public class AcceptLoginRequestReq
    {
        public  Guid LoginRequestId { get; set; }
        public int Status { get; set; }
    }
}
