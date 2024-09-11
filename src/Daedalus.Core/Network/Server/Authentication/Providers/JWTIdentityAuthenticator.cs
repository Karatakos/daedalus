namespace Daedalus.Core.Network.Server.Auth;

using Microsoft.Extensions.Logging;

using Daedalus.Core.Network.Components;

using FluentResults;

public class JWTIdentityAuthenticator(string token) : IAsyncIdentityAuthenticator {
    public async Task<Result<NetPlayerIdentity>> AuthenticateAsync() {
        return new NetPlayerIdentity() { Username = "Beefcake", UserId = "some-guid-1" };
    }
}
