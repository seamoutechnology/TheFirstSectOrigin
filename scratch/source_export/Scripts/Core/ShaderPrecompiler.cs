using System.Collections;
using UnityEngine;

namespace GameClient.Core
{
    public class ShaderPrecompiler : MonoBehaviour
    {
        [SerializeField] private ShaderVariantCollection globalShaderCollection;

        public IEnumerator WarmUpShaders(System.Action<float> onProgress, System.Action onComplete)
        {
            if (globalShaderCollection == null)
            {
                Debug.LogWarning("[ShaderCompiler] Không tìm thấy ShaderVariantCollection. Đã bỏ qua bước dịch Shader.");
                onComplete?.Invoke();
                yield break;
            }

            if (!globalShaderCollection.isWarmedUp)
            {
                Debug.Log("[ShaderCompiler] Bắt đầu dịch (compile) Shaders...");
                
                onProgress?.Invoke(0.1f);
                yield return null; 

                globalShaderCollection.WarmUp();

                onProgress?.Invoke(1.0f);
                Debug.Log("[ShaderCompiler] Đã dịch xong toàn bộ Shaders.");
            }
            
            onComplete?.Invoke();
        }
    }
}
