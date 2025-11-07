using MyProject.Helper.Utils.Interfaces;
using System.Security.Claims;
using MyProject.Helper.ModelHelps;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace MyProject.Helper.Utils
{
    public class TokenUtils: ITokenUtils
    {
        private readonly JwtSettings _jwtSettings;

        public TokenUtils(IOptionsSnapshot<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        //GENERATE TOKEN PART
        //public string GenerateJwt(User user, IList<string> roles)
        //{

        //    var claims = new List<Claim>
        //    {
        //        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        //        new Claim(ClaimTypes.Name, user.UserName),
        //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        //    };
        //    DateTime jwtDate = DateTime.UtcNow;

        //    var roleClaims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        //    claims.AddRange(roleClaims);

        //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        //    var expires = DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings.ExpirationInDays));

        //    var token = new JwtSecurityToken(
        //        issuer: jwtSettings.Issuer,
        //        audience: jwtSettings.Issuer,
        //        claims,
        //        notBefore: jwtDate,
        //        expires: expires,
        //        signingCredentials: creds
        //    );

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}

        public string GenerateToken(long id)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            if (key.Length < 32)
            {
                Array.Resize(ref key, 32);
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", id.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken(long id)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            if (key.Length < 32)
            {
                Array.Resize(ref key, 32);
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", id.ToString()) }),
                Expires = DateTime.UtcNow.AddMonths(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                // Different grant type for refresh token
                // You can add more claims if needed, such as token type ("refresh_token")
                Claims = new Dictionary<string, object> { { "grant_type", "refresh_token" } }
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string? GenerateTokenFromRefreshToken(string refreshToken)
        {
            var adminId = ValidateToken(refreshToken);
            if (adminId == null) return null;

            return GenerateToken((long)adminId);
        }

        public long? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            if (key.Length < 32)
            {
                Array.Resize(ref key, 32);
            }
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = long.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                return userId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool IsAccessTokenExpired(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);

                var expDate = jwtToken.ValidTo;
                var now = DateTime.UtcNow;

                return now >= expDate;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
