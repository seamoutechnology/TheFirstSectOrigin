using System.Threading.Tasks;

namespace GameClient.Core.Interfaces
{
    public interface IUIView
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
        void Setup(object data = null); // Truyền dữ liệu vào panel
    }

    public interface IResourceManager
    {
        Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;
        Task<UnityEngine.GameObject> InstantiateAsync(string address, UnityEngine.Transform parent = null);
        void ReleaseAsset(object asset);
        void ReleaseInstance(UnityEngine.GameObject instance);
    }
}
