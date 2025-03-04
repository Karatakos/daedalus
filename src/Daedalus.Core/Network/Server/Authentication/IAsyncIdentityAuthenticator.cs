namespace Daedalus.Core.Network.Server.Auth;

using Daedalus.Core.Network.Components;

using FluentResults;

public interface IAsyncIdentityAuthenticator {
    public Task<Result<NetPlayerIdentity>> AuthenticateAsync();
}
