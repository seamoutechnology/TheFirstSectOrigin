# ==============================================================================
# Dockerfile - Multi-stage build cho Go Server
# Hỗ trợ 3 target: development, sample, production
# ==============================================================================

# ---- Stage 1: Builder ----
FROM golang:alpine AS builder

# Cài các công cụ cần thiết
RUN apk add --no-cache git ca-certificates tzdata

WORKDIR /app

# Copy go.mod và go.sum trước để tận dụng Docker cache layer
COPY go.mod go.sum ./
RUN go mod download

# Copy toàn bộ source code
COPY . .

# Build tất cả các binary
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o bin/login-server  ./cmd/login-server
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o bin/gateway       ./cmd/gateway
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o bin/world-server  ./cmd/world-server
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o bin/combat-server ./cmd/combat-server
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o bin/admin         ./cmd/admin

# ---- Stage 2: Production image (nhỏ gọn nhất) ----
FROM scratch AS production

COPY --from=builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
COPY --from=builder /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=builder /app/bin /bin
COPY --from=builder /app/assets /assets

# CMD được ghi đè bởi docker-compose command
CMD ["/bin/login-server"]

# ---- Stage 3: Sample image (giống production + có shell để debug) ----
FROM alpine:3.19 AS sample

RUN apk add --no-cache ca-certificates tzdata
COPY --from=builder /app/bin /bin
COPY --from=builder /app/assets /assets

CMD ["/bin/login-server"]

# ---- Stage 4: Development image (có thêm debugging tools) ----
FROM alpine:3.19 AS development

RUN apk add --no-cache ca-certificates tzdata curl
COPY --from=builder /app/bin /bin
COPY --from=builder /app/assets /assets

CMD ["/bin/login-server"]
