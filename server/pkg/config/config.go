package config

import (
	"fmt"
	"os"
	"strings"

	"github.com/spf13/viper"
	_ "github.com/spf13/viper/remote"
	"github.com/fsnotify/fsnotify"
)

type Config struct {
	App      AppConfig      `mapstructure:"app"`
	Postgres PostgresConfig `mapstructure:"postgres"`
	GameDB   GameDBConfig   `mapstructure:"game_db"`
	Redis    RedisConfig    `mapstructure:"redis"`
	JWT      JWTConfig      `mapstructure:"jwt"`
	Server   ServerConfig   `mapstructure:"server"`
	Log      LogConfig      `mapstructure:"log"`
	SMTP     SMTPConfig     `mapstructure:"smtp"`
	Domain   DomainConfig   `mapstructure:"domain"`
}

type DomainConfig struct {
	App string `mapstructure:"app"`
	CDN string `mapstructure:"cdn"`
}

type SMTPConfig struct {
	Host     string `mapstructure:"host"`
	Port     int    `mapstructure:"port"`
	User     string `mapstructure:"user"`
	Password string `mapstructure:"password"`
	From     string `mapstructure:"from"`
}

type LogConfig struct {
	Enabled    bool   `mapstructure:"enabled"`
	Level      string `mapstructure:"level"`
	FilePath   string `mapstructure:"file_path"`
	MaxSize    int    `mapstructure:"max_size"`
	MaxBackups int    `mapstructure:"max_backups"`
	MaxAge     int    `mapstructure:"max_age"`
	Compress   bool   `mapstructure:"compress"`
}

type AppConfig struct {
	Env string `mapstructure:"env"`
}

type PostgresConfig struct {
	Host     string `mapstructure:"host"`
	Port     int    `mapstructure:"port"`
	User     string `mapstructure:"user"`
	Password string `mapstructure:"password"`
	DBName   string `mapstructure:"db_name"`
	DSN      string `mapstructure:"dsn"`
}

type GameDBConfig struct {
	Host     string `mapstructure:"host"`
	Port     int    `mapstructure:"port"`
	User     string `mapstructure:"user"`
	Password string `mapstructure:"password"`
	DBName   string `mapstructure:"db_name"`
	DSN      string `mapstructure:"dsn"`
}

type RedisConfig struct {
	Host     string `mapstructure:"host"`
	Port     int    `mapstructure:"port"`
	Password string `mapstructure:"password"`
	DB       int    `mapstructure:"db"`
	Addr     string `mapstructure:"addr"`
}

type JWTConfig struct {
	Secret      string `mapstructure:"secret"`
	ExpireHours int    `mapstructure:"expire_hours"`
}

type ServerConfig struct {
	Port     int    `mapstructure:"port"`
	Host     string `mapstructure:"host"`
	Addr     string `mapstructure:"addr"`
	ServerID string `mapstructure:"server_id"`
}

var globalConfig *Config

func Load(envFile ...string) (*Config, error) {
	v := viper.New()

	v.AutomaticEnv()
	v.SetEnvKeyReplacer(strings.NewReplacer(".", "_"))

	consulURL := os.Getenv("CONSUL_URL")
	if consulURL != "" {
		fmt.Printf("[config] Connecting to Consul at %s\n", consulURL)
		v.AddRemoteProvider("consul", consulURL, "thefirstsectorigin/config.json")
		v.SetConfigType("json")
		err := v.ReadRemoteConfig()
		if err == nil {
			go func() {
				for {
					err := v.WatchRemoteConfig()
					if err != nil {
						fmt.Printf("[config] WatchRemoteConfig error: %v\n", err)
						continue
					}
					var newCfg Config
					if err := v.Unmarshal(&newCfg); err == nil {
						computeDerivedFields(&newCfg)
						globalConfig = &newCfg
						fmt.Println("[config] Remote config updated successfully!")
					}
				}
			}()
		} else {
			fmt.Printf("[config] Remote config error: %v. Falling back to local/env.\n", err)
		}
	} else {
		v.SetConfigName("config")
		v.SetConfigType("yaml")
		v.AddConfigPath(".")
		v.AddConfigPath("./config")
		
		if err := v.ReadInConfig(); err != nil {
			fmt.Printf("[config] No local config.yaml found, using ENV vars: %v\n", err)
		} else {
			v.WatchConfig()
			v.OnConfigChange(func(e fsnotify.Event) {
				fmt.Println("[config] Local config file changed:", e.Name)
				var newCfg Config
				if err := v.Unmarshal(&newCfg); err == nil {
					computeDerivedFields(&newCfg)
					globalConfig = &newCfg
				}
			})
		}
	}

	cfg := &Config{}
	
	v.SetDefault("app.env", "development")
	v.BindEnv("app.env", "APP_ENV")

	v.SetDefault("log.enabled", true)
	v.SetDefault("log.level", "info")

	v.SetDefault("postgres.host", "localhost")
	v.SetDefault("postgres.port", 5432)
	v.SetDefault("postgres.user", "postgres")
	v.SetDefault("postgres.password", "postgres")
	v.SetDefault("postgres.db_name", "tfso_global")
	v.BindEnv("postgres.host", "POSTGRES_HOST")
	v.BindEnv("postgres.port", "POSTGRES_PORT")
	v.BindEnv("postgres.user", "POSTGRES_USER")
	v.BindEnv("postgres.password", "POSTGRES_PASSWORD")
	v.BindEnv("postgres.db_name", "POSTGRES_DB")

	v.SetDefault("game_db.host", "localhost")
	v.SetDefault("game_db.port", 5432)
	v.SetDefault("game_db.user", "postgres")
	v.SetDefault("game_db.password", "postgres")
	v.SetDefault("game_db.db_name", "tfso_game")
	v.BindEnv("game_db.host", "GAME_DB_HOST")
	v.BindEnv("game_db.port", "GAME_DB_PORT")
	v.BindEnv("game_db.user", "GAME_DB_USER")
	v.BindEnv("game_db.password", "GAME_DB_PASSWORD")
	v.BindEnv("game_db.db_name", "GAME_DB_NAME")

	v.SetDefault("redis.host", "redis")
	v.SetDefault("redis.port", 6379)
	v.SetDefault("redis.db", 0)
	v.BindEnv("redis.host", "REDIS_HOST")
	v.BindEnv("redis.port", "REDIS_PORT")
	v.BindEnv("redis.password", "REDIS_PASSWORD")
	v.BindEnv("redis.db", "REDIS_DB")

	v.BindEnv("jwt.secret", "JWT_SECRET")
	v.BindEnv("jwt.expire_hours", "JWT_EXPIRE_HOURS")

	v.SetDefault("server.port", 50051)
	v.SetDefault("server.server_id", "zone1")
	v.BindEnv("server.port", "SERVER_PORT")
	v.BindEnv("server.host", "SERVER_HOST")
	v.BindEnv("server.server_id", "SERVER_ID")
	
	if err := v.Unmarshal(cfg); err != nil {
		return nil, fmt.Errorf("unable to decode config into struct: %v", err)
	}

	computeDerivedFields(cfg)
	
	globalConfig = cfg
	return cfg, nil
}

func computeDerivedFields(cfg *Config) {
	cfg.Postgres.DSN = fmt.Sprintf(
		"host=%s port=%d user=%s password=%s dbname=%s sslmode=disable TimeZone=UTC",
		cfg.Postgres.Host, cfg.Postgres.Port, cfg.Postgres.User,
		cfg.Postgres.Password, cfg.Postgres.DBName,
	)
	
	cfg.GameDB.DSN = fmt.Sprintf(
		"host=%s port=%d user=%s password=%s dbname=%s sslmode=disable TimeZone=UTC",
		cfg.GameDB.Host, cfg.GameDB.Port, cfg.GameDB.User,
		cfg.GameDB.Password, cfg.GameDB.DBName,
	)

	cfg.Redis.Addr = fmt.Sprintf("%s:%d", cfg.Redis.Host, cfg.Redis.Port)
	cfg.Server.Addr = fmt.Sprintf("%s:%d", cfg.Server.Host, cfg.Server.Port)
}

func (c *Config) IsDev() bool {
	return c.App.Env == "development"
}

func (c *Config) IsProd() bool {
	return c.App.Env == "production"
}

func GetConfig() *Config {
	return globalConfig
}
