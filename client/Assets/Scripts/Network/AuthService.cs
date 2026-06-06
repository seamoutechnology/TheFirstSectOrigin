using System;
using System.Threading.Tasks;
using Grpc.Core;
using UnityEngine;
using GameClient.Network.Pb;

namespace GameClient.Network
{
    public static class AuthClientApi
    {
        public static async Task<RegisterResponse> RegisterAsync(string username, string email, string password)
        {
            var client = NetworkManager.Instance.AuthClient;
            if (client == null) throw new InvalidOperationException("AuthClient is not initialized.");

            var request = new RegisterRequest { Username = username, Email = email, Password = password };

            try
            {
                return await client.RegisterAsync(request, NetworkManager.DefaultCallOptions());
            }
            catch (RpcException ex)
            {
                Debug.LogError($"[Auth] Register RPC failed: {ex.Status}");
                throw;
            }
        }

        public static async Task<LoginResponse> LoginAsync(string username, string password)
        {
            return await GameClient.Managers.AccountManager.Instance.LoginAsync(username, password);
        }
    }
}
