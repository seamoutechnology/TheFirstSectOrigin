package logger

import (
	"os"
	"path/filepath"

	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
	"server/pkg/config"
)

var global *zap.Logger

func Init(cfg *config.Config) *zap.Logger {
	if !cfg.Log.Enabled {
		global = zap.NewNop()
		return global
	}

	var level zapcore.Level
	if err := level.UnmarshalText([]byte(cfg.Log.Level)); err != nil {
		level = zapcore.InfoLevel
	}

	var encoder zapcore.Encoder
	if cfg.App.Env == "production" || cfg.App.Env == "sample" {
		encoder = zapcore.NewJSONEncoder(zap.NewProductionEncoderConfig())
	} else {
		encCfg := zap.NewDevelopmentEncoderConfig()
		encCfg.EncodeLevel = zapcore.CapitalColorLevelEncoder
		encoder = zapcore.NewConsoleEncoder(encCfg)
	}

	syncers := []zapcore.WriteSyncer{zapcore.AddSync(os.Stdout)}

	if cfg.Log.FilePath != "" {
		dir := filepath.Dir(cfg.Log.FilePath)
		_ = os.MkdirAll(dir, 0755)

		file, err := os.OpenFile(cfg.Log.FilePath, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0644)
		if err == nil {
			syncers = append(syncers, zapcore.AddSync(file))
		}
	}

	core := zapcore.NewCore(encoder, zapcore.NewMultiWriteSyncer(syncers...), level)
	logger := zap.New(core, zap.AddCaller())

	global = logger
	zap.ReplaceGlobals(logger)
	return logger
}

func Get() *zap.Logger {
	if global == nil {
		return zap.NewNop()
	}
	return global
}

func Sync() {
	if global != nil {
		_ = global.Sync()
	}
}
