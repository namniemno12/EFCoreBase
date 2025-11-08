namespace MyProject.Helper.Utils.Interfaces
{
    public interface ITokenUtils
    {
        //string GenerateJwt(User user, IList<string> roles);

        string GenerateToken(Guid id);
        string GenerateRefreshToken(Guid id);
        string? GenerateTokenFromRefreshToken(string refreshToken);
        Guid? ValidateToken(string token);
        bool IsAccessTokenExpired(string accessToken);
    }
}
