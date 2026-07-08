using UnityEngine;
using GameClient.Managers;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.Gameplay.Items
{
    public class ItemInstance : MonoBehaviour
    {
        public ExportedItem Data { get; private set; }

        public void Setup(ExportedItem itemData)
        {
            Data = itemData;
        }

        private void OnMouseDown()
        {
            Debug.Log($"[Item] Nhặt rương {Data.id}, số lượng {Data.quantity}");
            
            Destroy(gameObject);
        }
    }
}
