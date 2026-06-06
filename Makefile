# ==============================================================================
# Makefile - TheFirstSectOrigin | Cross-Platform (Windows / Linux / macOS)
# Environments: development (default) | sample | production
# Usage: make docker-up ENV=sample
# ==============================================================================

# --- Phát hiện hệ điều hành ---
ifeq ($(OS),Windows_NT)
    DETECTED_OS  := Windows
    SHELL        := powershell.exe
    .SHELLFLAGS  := -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command
    EXE          := .exe
    NULL         := $$null
    # Chuyển path separator cho protoc trên Windows
    PROTOS       := $(subst /,\,$(wildcard share/proto/*.proto))

    # --- Lệnh tạo thư mục ---
    define MKDIR
        if (-not (Test-Path "$(1)")) { New-Item -ItemType Directory -Path "$(1)" -Force | Out-Null }
    endef

    # --- Lệnh in màu ---
    define OK
        Write-Host "[OK] $(1)" -ForegroundColor Green
    endef
    define INFO
        Write-Host ">>> $(1)" -ForegroundColor Cyan
    endef
    define WARN
        Write-Host "[WARN] $(1)" -ForegroundColor Yellow
    endef
    define ERR
        Write-Host "[MISS] $(1) -> $(2)" -ForegroundColor Red
    endef

    # --- Kiểm tra tool ---
    define CHECK
        if (Get-Command "$(1)" -ErrorAction SilentlyContinue) { Write-Host "  [OK]   $(1)" -ForegroundColor Green } else { Write-Host "  [MISS] $(1)  -- $(2)" -ForegroundColor Red }
    endef

else
    # Linux hoặc macOS
    UNAME := $(shell uname -s)
    ifeq ($(UNAME),Darwin)
        DETECTED_OS := macOS
    else
        DETECTED_OS := Linux
    endif

    EXE    :=
    NULL   := /dev/null
    PROTOS := $(wildcard share/proto/*.proto)

    define MKDIR
        mkdir -p $(1)
    endef

    define OK
        printf '\033[0;32m[OK] $(1)\033[0m\n'
    endef
    define INFO
        printf '\033[0;36m>>> $(1)\033[0m\n'
    endef
    define WARN
        printf '\033[0;33m[WARN] $(1)\033[0m\n'
    endef
    define ERR
        printf '\033[0;31m[MISS] $(1) -> $(2)\033[0m\n'
    endef

    define CHECK
        @if command -v $(1) > /dev/null 2>&1; then printf '  \033[0;32m[OK]   $(1)\033[0m\n'; else printf '  \033[0;31m[MISS] $(1)  -- $(2)\033[0m\n'; fi
    endef
endif

# --- Biến Cấu hình ---
PROTO_DIR      := share/proto
GO_OUT_DIR     := server/pkg/pb
CSHARP_OUT_DIR := client/Assets/Scripts/Network/Pb
SERVER_DIR     := server
BINARY_NAME    := server

# --- Môi trường (mặc định: development) ---
ENV            ?= development
ENV_FILE       := .env.$(ENV)

# ==============================================================================
.PHONY: all help proto proto-go proto-csharp \
        infra-up env-info docker-restart \
        server-run server-build server-tidy server-lint \
        docker-up docker-down docker-logs docker-clean \
        setup check-deps

all: help

# ==============================================================================
# HELP
# ==============================================================================
help:
	@echo ""
	@echo "========================================"
	@echo "  TheFirstSectOrigin | OS: $(DETECTED_OS)"
	@echo "========================================"
	@echo ""
	@echo "  [Proto]"
	@echo "    make proto           Compile all .proto -> Go + C#"
	@echo "    make proto-go        Compile .proto -> Go only"
	@echo "    make proto-csharp    Compile .proto -> C# only"
	@echo ""
	@echo "  [Server]"
	@echo "    make server-run      Run Go server (dev mode)"
	@echo "    make server-build    Build server binary"
	@echo "    make server-tidy     go mod tidy"
	@echo "    make server-lint     Run golangci-lint"
	@echo ""
	@echo "  [Docker]"
	@echo "    make docker-up       Start all services (DB, Redis)"
	@echo "    make docker-down     Stop all services"
	@echo "    make docker-logs     Tail service logs"
	@echo "    make docker-clean    Stop + delete all volumes (!)"
	@echo ""
	@echo "  [Setup]"
	@echo "    make check-deps      Check required tools"
	@echo "    make setup           Install Go proto plugins"
	@echo ""

# ==============================================================================
# PROTO COMPILATION
# ==============================================================================
proto: proto-go proto-csharp
	@$(call OK,Protobuf compilation complete!)

proto-go:
	@$(call INFO,Compiling .proto for Go...)
	@$(call MKDIR,$(GO_OUT_DIR))
	protoc --proto_path=$(PROTO_DIR) --go_out=$(GO_OUT_DIR) --go_opt=paths=source_relative --go-grpc_out=$(GO_OUT_DIR) --go-grpc_opt=paths=source_relative $(PROTOS)
	@$(call OK,Go pb generated -> $(GO_OUT_DIR))

# Đường dẫn plugin gRPC C# (Đã cập nhật theo máy bạn)
GRPC_CSHARP_PLUGIN ?= tools/Grpc.Tools.2.62.0/tools/windows_x86/grpc_csharp_plugin.exe

proto-csharp:
	@$(call INFO,Compiling .proto for C# Unity (including gRPC)...)
	@$(call MKDIR,$(CSHARP_OUT_DIR))
	protoc --proto_path=$(PROTO_DIR) --csharp_out=$(CSHARP_OUT_DIR) --grpc_out=$(CSHARP_OUT_DIR) --plugin=protoc-gen-grpc=$(GRPC_CSHARP_PLUGIN) $(PROTOS)
	@$(call OK,C# pb & gRPC generated -> $(CSHARP_OUT_DIR))

# ==============================================================================
# SERVER (GOLANG)
# ==============================================================================
server-run:
	@$(call INFO,Starting Go server in dev mode...)
	Set-Location $(SERVER_DIR); go run ./cmd/login-server/main.go

server-build:
	@$(call INFO,Building server binary...)
	Set-Location $(SERVER_DIR); go build -o bin/$(BINARY_NAME)$(EXE) ./cmd/login-server/main.go
	@$(call OK,Binary -> $(SERVER_DIR)/bin/$(BINARY_NAME)$(EXE))

server-tidy:
	@$(call INFO,Running go mod tidy...)
	Set-Location $(SERVER_DIR); go mod tidy
	@$(call OK,Done.)

server-lint:
	@$(call INFO,Running linter...)
	Set-Location $(SERVER_DIR); golangci-lint run ./...
	@$(call OK,Lint passed.)

# ==============================================================================
# TESTING
# ==============================================================================

## Chạy toàn bộ test trên Server (Go)
test-server:
	@$(call INFO,Running Server Tests...)
	Set-Location $(SERVER_DIR); go test -v ./...
	@$(call OK,Server tests passed.)

## Chạy toàn bộ test trên Client (Unity) - Yêu cầu Unity trong PATH
## Lưu ý: Cần điều chỉnh đường dẫn -projectPath nếu cần
test-client:
	@$(call INFO,Running Client Tests (Unity Test Runner)...)
	unity -batchmode -runTests -projectPath client -testResults client/Tests/results.xml -testPlatform PlayMode
	@$(call OK,Client tests executed. Results -> client/Tests/results.xml)

## Chạy toàn bộ hệ thống test
test-all: test-server test-client
	@$(call OK,All tests passed!)

# ==============================================================================
# DOCKER & INFRASTRUCTURE
# ==============================================================================

## Chỉ khởi động các services hạ tầng (DB + Redis), không build server
infra-up:
	@$(call INFO,[$(ENV)] Starting infra services (DB + Redis)...)
	docker-compose --env-file $(ENV_FILE) up -d postgres_global postgres_game redis
	@$(call OK,Infra running.)

## Khởi động toàn bộ services bao gồm các Go servers
docker-up:
	@$(call INFO,[$(ENV)] Starting ALL services...)
	docker-compose --env-file $(ENV_FILE) --profile server up -d --build

docker-build:
	@$(call INFO,[$(ENV)] Building service $(SERVICE)...)
	docker-compose --env-file $(ENV_FILE) build $(SERVICE)

docker-rebuild:
	@$(call INFO,[$(ENV)] Rebuilding and restarting service $(SERVICE)...)
	docker-compose --env-file $(ENV_FILE) build $(SERVICE)
	docker-compose --env-file $(ENV_FILE) up -d $(SERVICE)
	@$(call OK,All services running. Use: make docker-logs ENV=$(ENV))

## Dừng tất cả services
docker-down:
	@$(call WARN,[$(ENV)] Stopping all services...)
	docker-compose --env-file $(ENV_FILE) --profile server down
	@$(call OK,Services stopped.)

## Xem logs của services
docker-logs:
	docker-compose --env-file $(ENV_FILE) --profile server logs -f

## Restart một service cụ thể: make docker-restart SERVICE=gateway
docker-restart:
	@$(call INFO,[$(ENV)] Restarting $(SERVICE)...)
	docker-compose --env-file $(ENV_FILE) restart $(SERVICE)

## Xóa toàn bộ data (volume) - CẢNH BÁO!
docker-clean:
	@$(call WARN,[$(ENV)] Deleting ALL data volumes!)
	docker-compose --env-file $(ENV_FILE) --profile server down -v
	@$(call OK,All services and volumes removed.)

## In ra env file đang được dùng
env-info:
	@$(call INFO,Current ENV: $(ENV) | File: $(ENV_FILE))

# ==============================================================================
# SETUP & UTILITIES
# ==============================================================================
check-deps:
	@echo ""
	@echo "Checking dependencies on $(DETECTED_OS)..."
	@echo ""
	@$(call CHECK,go,https://go.dev/dl/)
	@$(call CHECK,protoc,https://github.com/protocolbuffers/protobuf/releases)
	@$(call CHECK,protoc-gen-go,run: make setup)
	@$(call CHECK,protoc-gen-go-grpc,run: make setup)
	@$(call CHECK,docker,https://www.docker.com)
	@$(call CHECK,golangci-lint,go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest)
	@echo ""

setup:
	@$(call INFO,Installing Go protobuf plugins...)
	go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
	go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest
	@$(call OK,Setup done! Ensure GOPATH/bin is in your PATH)
