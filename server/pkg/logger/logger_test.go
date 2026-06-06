package logger

import (
	"os"
	"testing"

	"server/pkg/config"
)

func TestLogger_Init(t *testing.T) {
	cfg := &config.Config{
		App: config.AppConfig{Env: "development"},
		Log: config.LogConfig{
			Enabled:  true,
			Level:    "debug",
			FilePath: "test_logs/test.log",
		},
	}

	defer os.RemoveAll("test_logs")

	l := Init(cfg)
	if l == nil {
		t.Fatal("Expected logger to be initialized, got nil")
	}

	if Get() != l {
		t.Error("Get() should return the same logger as Init()")
	}

	l.Debug("test debug message")
	l.Info("test info message")

	Sync()

	if _, err := os.Stat("test_logs/test.log"); os.IsNotExist(err) {
		t.Error("Log file was not created")
	}
}

func TestLogger_Disabled(t *testing.T) {
	cfg := &config.Config{
		Log: config.LogConfig{
			Enabled: false,
		},
	}

	l := Init(cfg)
	if l == nil {
		t.Fatal("Expected logger to be initialized even if disabled (Nop), got nil")
	}

	l.Info("this should not be logged")
}
