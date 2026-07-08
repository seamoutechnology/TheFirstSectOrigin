using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.Core.Interfaces;
using GameClient.Managers;
using GameClient.Network;
using System.Threading.Tasks;

namespace GameClient.UI
{
    public class CreateCharPanel : MonoBehaviour, IUIView
    {
        [Header("UI Elements")]
        public TMP_InputField inputName;
        public Button btnConfirm;
        public Button btnRandomName; // Nút tạo tên ngẫu nhiên (nếu có)
        
        [Header("Avatar Selection")]
        public Button btnAvatarMale;
        public Button btnAvatarFemale;
        public Image imgAvatarMale;
        public Image imgAvatarFemale;
        
        public float selectedScale = 1.2f;
        public float unselectedScale = 1.0f;
        
        private int _selectedGender = 0; // 0 = Nam, 1 = Nữ
        
        public bool IsVisible => gameObject.activeSelf;

        public void Setup(object data = null)
        {
            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveAllListeners();
                btnConfirm.onClick.AddListener(OnConfirmClicked);
            }
            
            if (btnRandomName != null)
            {
                btnRandomName.onClick.RemoveAllListeners();
                btnRandomName.onClick.AddListener(GenerateRandomName);
            }
            
            if (btnAvatarMale != null)
            {
                btnAvatarMale.onClick.RemoveAllListeners();
                btnAvatarMale.onClick.AddListener(() => SelectAvatar(0));
            }
            
            if (btnAvatarFemale != null)
            {
                btnAvatarFemale.onClick.RemoveAllListeners();
                btnAvatarFemale.onClick.AddListener(() => SelectAvatar(1));
            }
            
            SelectAvatar(0, instant: true);
        }

        private void SelectAvatar(int gender, bool instant = false)
        {
            _selectedGender = gender;
            float duration = instant ? 0f : 0.25f;

            if (imgAvatarMale != null && imgAvatarFemale != null)
            {
                int maleIdx = imgAvatarMale.transform.GetSiblingIndex();
                int femaleIdx = imgAvatarFemale.transform.GetSiblingIndex();
                
                int topIdx = Mathf.Max(maleIdx, femaleIdx);
                int bottomIdx = Mathf.Min(maleIdx, femaleIdx);

                if (gender == 0) // Nam
                {
                    imgAvatarMale.transform.SetSiblingIndex(topIdx);
                    imgAvatarFemale.transform.SetSiblingIndex(bottomIdx);
                }
                else // Nữ
                {
                    imgAvatarFemale.transform.SetSiblingIndex(topIdx);
                    imgAvatarMale.transform.SetSiblingIndex(bottomIdx);
                }
            }

            if (imgAvatarMale != null)
            {
                bool isMale = gender == 0;
                imgAvatarMale.color = isMale ? Color.white : Color.gray;
                
                if (instant) imgAvatarMale.transform.localScale = Vector3.one * (isMale ? selectedScale : unselectedScale);
                else DG.Tweening.ShortcutExtensions.DOScale(imgAvatarMale.transform, isMale ? selectedScale : unselectedScale, duration);
            }

            if (imgAvatarFemale != null)
            {
                bool isFemale = gender == 1;
                imgAvatarFemale.color = isFemale ? Color.white : Color.gray;
                
                if (instant) imgAvatarFemale.transform.localScale = Vector3.one * (isFemale ? selectedScale : unselectedScale);
                else DG.Tweening.ShortcutExtensions.DOScale(imgAvatarFemale.transform, isFemale ? selectedScale : unselectedScale, duration);
            }
        }

        private void GenerateRandomName()
        {
            if (inputName != null)
            {
                int style = Random.Range(0, 2); // 0: Tiên Hiệp, 1: Slang/Gamer
                bool addNumbers = Random.Range(0, 2) == 0; // 50% thêm số ở đuôi

                string name = GameClient.Utils.NicknameGenerator.Generate(
                    style: style, 
                    noDiacritics: true, 
                    noSpaces: true, // Luôn viết liền không dấu cách
                    addNumbers: addNumbers
                );
                inputName.text = name;
            }
        }

        private async void OnConfirmClicked()
        {
            if (inputName == null || string.IsNullOrWhiteSpace(inputName.text))
            {
                if (ToastManager.Instance != null)
                    ToastManager.Instance.ShowBigToast("Vui lòng nhập tên nhân vật!");
                UIManager.Instance.ShowMessage("Lỗi", "Vui lòng nhập tên nhân vật!");
                return;
            }

            string charName = inputName.text.Trim();
            btnConfirm.interactable = false;
            
            Debug.Log($"[CreateChar] Đang gửi yêu cầu tạo nhân vật: {charName}");
            
            try
            {
                var req = new GameClient.Network.Pb.CreatePlayerRequest { Nickname = charName };
                var res = await NetworkManager.Instance.GatewayClient.CreatePlayerAsync(req, NetworkManager.DefaultCallOptions());
                
                if (res != null && res.Base != null && res.Base.Code == 0)
                {
                    Debug.Log($"[CreateChar] Tạo nhân vật thành công! Giới tính: {(_selectedGender == 0 ? "Nam" : "Nữ")}");
                    GameContext.HasCharacter = true;
                    Hide();
                    
                    if (res.Profile != null)
                    {
                        GameManager.Instance.SetPlayer(res.Profile);
                    }
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.OpenPanel("MainGameHUDPanel");
                    }
                    string nickName = res.Profile != null ? res.Profile.Nickname : charName;
                    if (ToastManager.Instance != null)
                        ToastManager.Instance.ShowBigToast($"Chào mừng {nickName} gia nhập Tu Tiên Giới!");
                    UIManager.Instance.ShowMessage("Hệ thống", $"Chào mừng {nickName} gia nhập Tu Tiên Giới!");
                }
                else
                {
                    string errorMsg = res?.Base?.Message ?? "Lỗi không xác định";
                    if (ToastManager.Instance != null)
                        ToastManager.Instance.ShowBigToast($"Tạo nhân vật thất bại: {errorMsg}");
                    UIManager.Instance.ShowMessage("Lỗi", $"Tạo nhân vật thất bại: {errorMsg}");
                    btnConfirm.interactable = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreateChar] Lỗi gọi API CreatePlayer: {ex.Message}");
                if (ToastManager.Instance != null)
                    ToastManager.Instance.ShowBigToast("Mất kết nối máy chủ hoặc có lỗi xảy ra!");
                UIManager.Instance.ShowMessage("Lỗi", "Mất kết nối máy chủ hoặc có lỗi xảy ra!");
                btnConfirm.interactable = true;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            UIManager.Instance.DestroyPanel("CreateCharPanel"); // Hủy luôn sau khi dùng xong
        }
    }
}
