using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;
using GameClient.Managers;

namespace GameClient.UI
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LanguageDropdownSelector : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;
        private bool _isInitializing = false;

        private async void Start()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
            _isInitializing = true;

            // 1. Chờ hệ thống Localization khởi tạo xong
            await LocalizationSettings.InitializationOperation.Task;

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales == null || locales.Count == 0)
            {
                _isInitializing = false;
                return;
            }

            // 2. Clear các lựa chọn cũ và nạp danh sách ngôn ngữ mới
            _dropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentLanguageIndex = 0;
            var currentLocale = LocalizationSettings.SelectedLocale;

            for (int i = 0; i < locales.Count; i++)
            {
                var locale = locales[i];
                
                // Lấy tên hiển thị tự nhiên của ngôn ngữ (ví dụ: Tiếng Việt, English)
                string displayName = locale.name;
                if (locale.Identifier.CultureInfo != null)
                {
                    displayName = locale.Identifier.CultureInfo.NativeName;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
                    }
                }

                options.Add(displayName);

                // Tìm index của ngôn ngữ đang kích hoạt
                if (locale == currentLocale)
                {
                    currentLanguageIndex = i;
                }
            }

            // 3. Cập nhật dữ liệu hiển thị lên Dropdown
            _dropdown.AddOptions(options);
            _dropdown.value = currentLanguageIndex;
            _dropdown.RefreshShownValue();
            
            _isInitializing = false;

            // 4. Lắng nghe sự kiện người chơi chọn ngôn ngữ khác
            _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private async void OnDropdownValueChanged(int index)
        {
            if (_isInitializing) return;

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (index < 0 || index >= locales.Count) return;

            string selectedCode = locales[index].Identifier.Code;

            // Khóa tương tác tạm thời để tránh người chơi đổi liên tục khi đang tải
            _dropdown.interactable = false;

            // Cập nhật ngôn ngữ và lưu file cấu hình tự động
            await LocalizationManager.Instance.SetLanguageByCodeAsync(selectedCode);

            _dropdown.interactable = true;
            Debug.Log($"[LanguageDropdown] Đã chọn ngôn ngữ: {selectedCode} (Index: {index})");
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
            {
                _dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }
    }
}
