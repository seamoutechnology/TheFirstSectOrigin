package i18n

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sync"
)

type Bundle struct {
	locales map[string]map[string]string
	mu      sync.RWMutex
}

var globalBundle *Bundle
var once sync.Once

func GetBundle() *Bundle {
	once.Do(func() {
		globalBundle = &Bundle{
			locales: make(map[string]map[string]string),
		}
	})
	return globalBundle
}

func (b *Bundle) LoadLocales(dir string) error {
	b.mu.Lock()
	defer b.mu.Unlock()

	files, err := os.ReadDir(dir)
	if err != nil {
		return err
	}

	for _, f := range files {
		if filepath.Ext(f.Name()) == ".json" {
			lang := f.Name()[:len(f.Name())-5] // e.g. "vi", "en"
			data, err := os.ReadFile(filepath.Join(dir, f.Name()))
			if err != nil {
				continue
			}

			var translations map[string]string
			if err := json.Unmarshal(data, &translations); err != nil {
				continue
			}
			b.locales[lang] = translations
		}
	}
	return nil
}

func (b *Bundle) T(lang, key string, args ...interface{}) string {
	b.mu.RLock()
	defer b.mu.RUnlock()

	translations, ok := b.locales[lang]
	if !ok {
		translations = b.locales["en"]
	}

	val, ok := translations[key]
	if !ok {
		return key
	}

	return fmt.Sprintf(val, args...)
}
