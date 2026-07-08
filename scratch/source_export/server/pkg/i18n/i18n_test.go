package i18n

import (
	"os"
	"path/filepath"
	"testing"
)

func TestI18n(t *testing.T) {
	tmpDir, _ := os.MkdirTemp("", "locales")
	defer os.RemoveAll(tmpDir)

	viContent := `{"hello": "Xin chào %s", "welcome": "Chào mừng"}`
	enContent := `{"hello": "Hello %s", "welcome": "Welcome"}`

	os.WriteFile(filepath.Join(tmpDir, "vi.json"), []byte(viContent), 0644)
	os.WriteFile(filepath.Join(tmpDir, "en.json"), []byte(enContent), 0644)

	bundle := &Bundle{locales: make(map[string]map[string]string)}
	err := bundle.LoadLocales(tmpDir)
	if err != nil {
		t.Fatalf("LoadLocales failed: %v", err)
	}

	t.Run("TranslateVI", func(t *testing.T) {
		res := bundle.T("vi", "hello", "Nam")
		if res != "Xin chào Nam" {
			t.Errorf("Expected 'Xin chào Nam', got '%s'", res)
		}
	})

	t.Run("TranslateEN", func(t *testing.T) {
		res := bundle.T("en", "hello", "John")
		if res != "Hello John" {
			t.Errorf("Expected 'Hello John', got '%s'", res)
		}
	})

	t.Run("FallbackToEN", func(t *testing.T) {
		res := bundle.T("fr", "welcome")
		if res != "Welcome" {
			t.Errorf("Expected 'Welcome' (fallback), got '%s'", res)
		}
	})

	t.Run("KeyNotFound", func(t *testing.T) {
		res := bundle.T("en", "unknown_key")
		if res != "unknown_key" {
			t.Errorf("Expected 'unknown_key', got '%s'", res)
		}
	})
}
