using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DerasaX.Application.Dto.AccountDto
{
    public class AuthModel
    {
        public string Id { get; set; }
        public string? Message { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public List<string> Errors { get; set; }
        public string UserName { get; set; }
        public string? FullName { get; set; }
        /// <summary>Trusted role from the access token; the frontend uses it only to render navigation, never as authorization.</summary>
        public string? Role { get; set; }
        public string? Token { get; set; }
        /// <summary>Access-token expiry (UTC) so the SPA can schedule a silent refresh.</summary>
        public DateTime ExpiresOn { get; set; }
        [JsonIgnore]
        public string? RefreshToken { get; set; }
        [JsonIgnore]
        public DateTime RefreshTokenExpiration { get; set; }
    }
}
