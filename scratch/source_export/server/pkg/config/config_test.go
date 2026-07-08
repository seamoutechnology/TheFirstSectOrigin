package config

import (
	"os"
	"testing"
)

func TestConfig_Load(t *testing.T) {
	os.Setenv("APP_ENV", "test")
	os.Setenv("POSTGRES_HOST", "test_host")
	os.Setenv("REDIS_PORT", "1234")
	os.Setenv("LOG_ENABLED", "false")

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Failed to load config: %v", err)
	}

	if cfg.App.Env != "test" {
		t.Errorf("Expected APP_ENV test, got %s", cfg.App.Env)
	}

	if cfg.Postgres.Host != "test_host" {
		t.Errorf("Expected POSTGRES_HOST test_host, got %s", cfg.Postgres.Host)
	}

	if cfg.Redis.Port != 1234 {
		t.Errorf("Expected REDIS_PORT 1234, got %d", cfg.Redis.Port)
	}

	if cfg.Log.Enabled != false {
		t.Errorf("Expected LOG_ENABLED false, got %v", cfg.Log.Enabled)
	}

	os.Unsetenv("APP_ENV")
	cfg, _ = Load()
	if cfg.App.Env != "development" {
		t.Errorf("Expected default APP_ENV development, got %s", cfg.App.Env)
	}
}

func TestConfig_Helpers(t *testing.T) {
	cfg := &Config{App: AppConfig{Env: "development"}}
	if !cfg.IsDev() {
		t.Error("Expected IsDev() to be true")
	}
	if cfg.IsProd() {
		t.Error("Expected IsProd() to be false")
	}

	cfg.App.Env = "production"
	if cfg.IsDev() {
		t.Error("Expected IsDev() to be false")
	}
	if !cfg.IsProd() {
		t.Error("Expected IsProd() to be true")
	}
}
