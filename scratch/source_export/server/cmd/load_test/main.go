package main

import (
	"context"
	"flag"
	"fmt"
	"math/rand"
	"os"
	"os/exec"
	"regexp"
	"strconv"
	"strings"
	"sync"
	"sync/atomic"
	"time"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/metadata"

	"server/pkg/pb"
)

type ContainerStats struct {
	PeakCPU float64
	PeakMem float64 // in MB
	RawMem  string
}

type DockerMonitor struct {
	mu    sync.Mutex
	stats map[string]*ContainerStats
	stop  chan struct{}
}

func NewDockerMonitor() *DockerMonitor {
	return &DockerMonitor{
		stats: make(map[string]*ContainerStats),
		stop:  make(chan struct{}),
	}
}

func (m *DockerMonitor) Start() {
	go func() {
		ticker := time.NewTicker(1 * time.Second)
		defer ticker.Stop()
		re := regexp.MustCompile(`^([^,]+),([0-9.]+)%,([0-9.]+)(GiB|MiB|KiB|B)`)
		
		for {
			select {
			case <-m.stop:
				return
			case <-ticker.C:
				cmd := exec.Command("docker", "stats", "--no-stream", "--format", "{{.Name}},{{.CPUPerc}},{{.MemUsage}}")
				output, err := cmd.Output()
				if err != nil {
					continue
				}
				
				lines := strings.Split(string(output), "\n")
				m.mu.Lock()
				for _, line := range lines {
					line = strings.TrimSpace(line)
					if line == "" {
						continue
					}
					
					matches := re.FindStringSubmatch(line)
					if len(matches) >= 5 {
						name := strings.TrimSpace(matches[1])
						cpuStr := matches[2]
						memStr := matches[3]
						unit := matches[4]
						
						cpu, _ := strconv.ParseFloat(cpuStr, 64)
						memVal, _ := strconv.ParseFloat(memStr, 64)
						
						var memMB float64
						switch unit {
						case "GiB":
							memMB = memVal * 1024
						case "MiB":
							memMB = memVal
						case "KiB":
							memMB = memVal / 1024
						default:
							memMB = memVal / (1024 * 1024)
						}
						
						rawMem := fmt.Sprintf("%s %s", memStr, unit)
						
						s, exists := m.stats[name]
						if !exists {
							m.stats[name] = &ContainerStats{
								PeakCPU: cpu,
								PeakMem: memMB,
								RawMem:  rawMem,
							}
						} else {
							if cpu > s.PeakCPU {
								s.PeakCPU = cpu
							}
							if memMB > s.PeakMem {
								s.PeakMem = memMB
								s.RawMem = rawMem
							}
						}
					}
				}
				m.mu.Unlock()
			}
		}
	}()
}

func (m *DockerMonitor) Stop() {
	close(m.stop)
}

func (m *DockerMonitor) PrintResults() {
	m.mu.Lock()
	defer m.mu.Unlock()
	
	if len(m.stats) == 0 {
		return
	}
	
	fmt.Println("\n=============================================================")
	fmt.Println("                 HIỆU NĂNG ĐỈNH SERVER (PEAK STATS)          ")
	fmt.Println("=============================================================")
	fmt.Printf("%-32s | %-14s | %-15s\n", "Tên Container", "Peak CPU (%)", "Peak Memory")
	fmt.Println("-------------------------------------------------------------")
	for name, s := range m.stats {
		fmt.Printf("%-32s | %12.2f%% | %-15s\n", name, s.PeakCPU, s.RawMem)
	}
	fmt.Println("=============================================================")
}

// Statistics tracking
type Stats struct {
	TotalRequests   uint64
	SuccessRequests uint64
	FailedRequests  uint64
	TotalLatencyNs  int64
	MinLatencyMs    int64
	MaxLatencyMs    int64
	mu              sync.Mutex
}

func (s *Stats) Add(latency time.Duration, success bool) {
	atomic.AddUint64(&s.TotalRequests, 1)
	if success {
		atomic.AddUint64(&s.SuccessRequests, 1)
	} else {
		atomic.AddUint64(&s.FailedRequests, 1)
	}

	ms := latency.Milliseconds()
	atomic.AddInt64(&s.TotalLatencyNs, int64(latency))

	s.mu.Lock()
	if s.MinLatencyMs == 0 || ms < s.MinLatencyMs {
		s.MinLatencyMs = ms
	}
	if ms > s.MaxLatencyMs {
		s.MaxLatencyMs = ms
	}
	s.mu.Unlock()
}

func main() {
	// Parse command line flags
	numBots := flag.Int("bots", 100, "Number of concurrent bots to simulate")
	durationStr := flag.String("duration", "1m", "Duration of the test (e.g. 30s, 2m, 5m)")
	authAddr := flag.String("auth", "localhost:50051", "Target gRPC Login/Auth Server address")
	gatewayAddr := flag.String("gateway", "localhost:50052", "Target gRPC Gateway Server address")
	rampupStr := flag.String("rampup", "10s", "Ramp-up period to spawn all bots")
	registerOnly := flag.Bool("register", false, "Only pre-register the specified number of accounts and exit")
	flag.Parse()

	duration, err := time.ParseDuration(*durationStr)
	if err != nil {
		fmt.Printf("Invalid duration parameter '%s': %v\n", *durationStr, err)
		os.Exit(1)
	}

	rampup, err := time.ParseDuration(*rampupStr)
	if err != nil {
		fmt.Printf("Invalid rampup parameter '%s': %v\n", *rampupStr, err)
		os.Exit(1)
	}

	// Connect to Auth Server once just for registration if needed
	authConn, err := grpc.Dial(*authAddr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		fmt.Printf("❌ Không thể kết nối tới Auth Server: %v\n", err)
		os.Exit(1)
	}
	defer authConn.Close()

	authCli := pb.NewAuthServiceClient(authConn)

	// If we are only registering accounts
	if *registerOnly {
		fmt.Println("=============================================================")
		fmt.Println("             BẮT ĐẦU ĐĂNG KÝ TỰ ĐỘNG TÀI KHOẢN BOTS          ")
		fmt.Println("=============================================================")
		fmt.Printf("[Cấu hình] Số lượng tài khoản: %d\n", *numBots)
		fmt.Printf("[Cấu hình] Auth Server       : %s\n", *authAddr)
		fmt.Println("-------------------------------------------------------------")
		
		ctx, cancel := context.WithTimeout(context.Background(), 30*time.Minute)
		defer cancel()
		
		runRegistrationOnly(ctx, *numBots, authCli)
		return
	}

	fmt.Println("=============================================================")
	fmt.Println("             BẮT ĐẦU KIỂM THỬ TẢI HỆ THỐNG GIAO DIỆN gRPC      ")
	fmt.Println("=============================================================")
	fmt.Printf("[Thiết lập] Số lượng Bots    : %d CCU\n", *numBots)
	fmt.Printf("[Thiết lập] Thời gian test   : %v\n", duration)
	fmt.Printf("[Thiết lập] Ramp-up period   : %v\n", rampup)
	fmt.Printf("[Thiết lập] Auth Server      : %s\n", *authAddr)
	fmt.Printf("[Thiết lập] Gateway Server   : %s\n", *gatewayAddr)
	fmt.Println("-------------------------------------------------------------")

	// Khởi chạy bộ giám sát Docker
	monitor := NewDockerMonitor()
	monitor.Start()
	defer monitor.Stop()

	// Stats
	stats := &Stats{}

	// Global Context
	ctx, cancel := context.WithTimeout(context.Background(), duration)
	defer cancel()

	var wg sync.WaitGroup
	rampUpDelay := rampup / time.Duration(*numBots)

	startTime := time.Now()

	// Spawn bots (each bot gets the connection details and dials its own connection)
	for i := 1; i <= *numBots; i++ {
		wg.Add(1)
		go func(botId int) {
			defer wg.Done()
			runBot(ctx, botId, *authAddr, *gatewayAddr, stats)
		}(i)
		
		time.Sleep(rampUpDelay)
		if botId := i; botId%500 == 0 || botId == *numBots {
			fmt.Printf("-> Đang khởi chạy... %d/%d bots đã kết nối\n", botId, *numBots)
		}
	}

	fmt.Println("-> Tất cả bots đã kích hoạt. Đang tiến hành đo đạc...")
	
	// Wait for duration to finish
	<-ctx.Done()
	
	fmt.Println("-> Đang thu thập và tổng hợp số liệu từ các bots...")
	wg.Wait()

	totalTime := time.Since(startTime)
	totalReq := atomic.LoadUint64(&stats.TotalRequests)
	successReq := atomic.LoadUint64(&stats.SuccessRequests)
	failReq := atomic.LoadUint64(&stats.FailedRequests)
	totalLat := atomic.LoadInt64(&stats.TotalLatencyNs)

	avgLatencyMs := int64(0)
	if totalReq > 0 {
		avgLatencyMs = (totalLat / int64(totalReq)) / int64(time.Millisecond)
	}

	tps := float64(totalReq) / totalTime.Seconds()
	successRate := 0.0
	if totalReq > 0 {
		successRate = float64(successReq) / float64(totalReq) * 100
	}

	fmt.Println("\n=============================================================")
	fmt.Println("                     KẾT QUẢ KIỂM THỬ TẢI                     ")
	fmt.Println("=============================================================")
	fmt.Printf("Tổng thời gian chạy test : %v\n", totalTime)
	fmt.Printf("Số lượng Bots ảo (CCU)   : %d\n", *numBots)
	fmt.Printf("Tổng số request đã gửi   : %d\n", totalReq)
	fmt.Printf("Số request thành công    : %d (%.2f%%)\n", successReq, successRate)
	fmt.Printf("Số request thất bại      : %d\n", failReq)
	fmt.Printf("Tốc độ xử lý (TPS)       : %.2f req/s\n", tps)
	fmt.Println("-------------------------------------------------------------")
	fmt.Printf("Độ trễ trung bình        : %d ms\n", avgLatencyMs)
	fmt.Printf("Độ trễ thấp nhất (Min)   : %d ms\n", stats.MinLatencyMs)
	fmt.Printf("Độ trễ cao nhất (Max)    : %d ms\n", stats.MaxLatencyMs)
	fmt.Println("=============================================================")

	// In kết quả tài nguyên Server
	monitor.PrintResults()
}

func runRegistrationOnly(ctx context.Context, totalBots int, authCli pb.AuthServiceClient) {
	var wg sync.WaitGroup
	sem := make(chan struct{}, 50) // Giới hạn 50 luồng đồng thời để không làm sập DB
	var successCount uint64
	var failCount uint64

	startTime := time.Now()

	for i := 1; i <= totalBots; i++ {
		wg.Add(1)
		sem <- struct{}{}
		go func(botId int) {
			defer wg.Done()
			defer func() { <-sem }()

			username := fmt.Sprintf("bench_bot_%d", botId)
			password := "BenchPass123!"

			_, err := authCli.Register(ctx, &pb.RegisterRequest{
				Username: username,
				Password: password,
				Email:    fmt.Sprintf("%s@benchmark.com", username),
			})

			if err == nil {
				atomic.AddUint64(&successCount, 1)
			} else {
				atomic.AddUint64(&failCount, 1)
			}

			if botId%500 == 0 || botId == totalBots {
				fmt.Printf("-> Tiến trình đăng ký: %d/%d tài khoản...\n", botId, totalBots)
			}
		}(i)
	}

	wg.Wait()
	fmt.Println("=============================================================")
	fmt.Println("                 HOÀN THÀNH ĐĂNG KÝ TÀI KHOẢN                ")
	fmt.Println("=============================================================")
	fmt.Printf("Tổng thời gian thực hiện : %v\n", time.Since(startTime))
	fmt.Printf("Đăng ký thành công       : %d\n", successCount)
	fmt.Printf("Đăng ký thất bại/đã có   : %d\n", failCount)
	fmt.Println("=============================================================")
}

var errCount uint32

func runBot(ctx context.Context, botId int, authAddr string, gatewayAddr string, stats *Stats) {
	username := fmt.Sprintf("bench_bot_%d", botId)
	password := "BenchPass123!"

	// Connect to Auth Server for this bot specifically
	authConn, err := grpc.Dial(authAddr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		if atomic.AddUint32(&errCount, 1) <= 10 {
			fmt.Printf("[Lỗi Bot %d] Dial Auth failed: %v\n", botId, err)
		}
		return
	}
	defer authConn.Close()
	authCli := pb.NewAuthServiceClient(authConn)

	// 1. Authenticate (Login only)
	var token string
	start := time.Now()
	loginRes, err := authCli.Login(ctx, &pb.LoginRequest{
		Username: username,
		Password: password,
	})
	
	if err == nil && loginRes != nil && loginRes.Token != "" {
		token = loginRes.Token
	} else {
		if atomic.AddUint32(&errCount, 1) <= 10 {
			if err != nil {
				fmt.Printf("[Lỗi Đăng Nhập Bot %d] gRPC error: %v\n", botId, err)
			} else if loginRes != nil {
				fmt.Printf("[Lỗi Đăng Nhập Bot %d] Business error: code=%d, msg=%s\n", botId, loginRes.Code, loginRes.Base.GetMessage())
			}
		}
	}

	stats.Add(time.Since(start), err == nil && token != "")

	if token == "" {
		return // Cannot authenticate, stop bot
	}

	// Connect to Gateway Server for this bot specifically (different port/connection for rate limiting)
	gatewayConn, err := grpc.Dial(gatewayAddr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		if atomic.AddUint32(&errCount, 1) <= 10 {
			fmt.Printf("[Lỗi Bot %d] Dial Gateway failed: %v\n", botId, err)
		}
		return
	}
	defer gatewayConn.Close()
	gatewayCli := pb.NewGatewayServiceClient(gatewayConn)

	// Create request context with metadata (JWT token)
	botCtx := metadata.NewOutgoingContext(ctx, metadata.Pairs("authorization", "Bearer "+token))

	// Ensure player profile/record exists (call CreatePlayer first just in case)
	_, _ = gatewayCli.CreatePlayer(botCtx, &pb.CreatePlayerRequest{
		Nickname: username,
	})

	// 2. Play game loop
	for {
		select {
		case <-ctx.Done():
			return
		default:
			// Action 1: Get Profile
			start = time.Now()
			_, err = gatewayCli.GetPlayerProfile(botCtx, &pb.GetPlayerProfileRequest{})
			if ctx.Err() != nil {
				return
			}
			stats.Add(time.Since(start), err == nil)
			if err != nil && atomic.AddUint32(&errCount, 1) <= 10 {
				fmt.Printf("[Lỗi Gateway Bot %d] GetPlayerProfile failed: %v\n", botId, err)
			}
			time.Sleep(time.Duration(1000+rand.Intn(1500)) * time.Millisecond) // Think time 1-2.5s

			// Action 2: Collect Resources
			start = time.Now()
			_, err = gatewayCli.CollectResources(botCtx, &pb.CollectResourcesRequest{})
			if ctx.Err() != nil {
				return
			}
			stats.Add(time.Since(start), err == nil)
			if err != nil && atomic.AddUint32(&errCount, 1) <= 10 {
				fmt.Printf("[Lỗi Gateway Bot %d] CollectResources failed: %v\n", botId, err)
			}
			time.Sleep(time.Duration(1000+rand.Intn(1500)) * time.Millisecond)

			// Action 3: Validate PvE Combat Result
			start = time.Now()
			_, err = gatewayCli.ValidatePvEResult(botCtx, &pb.ValidatePvEResultRequest{
				EnemyId:     "stage_1_1",
				IsVictory:   rand.Float32() > 0.25, // 75% win rate
				PlayerPower: 2000,
				EnemyPower:  1500,
			})
			if ctx.Err() != nil {
				return
			}
			stats.Add(time.Since(start), err == nil)
			if err != nil && atomic.AddUint32(&errCount, 1) <= 10 {
				fmt.Printf("[Lỗi Gateway Bot %d] ValidatePvEResult failed: %v\n", botId, err)
			}
			time.Sleep(time.Duration(2000+rand.Intn(2000)) * time.Millisecond)

			// Action 4: Get Leaderboard
			start = time.Now()
			_, err = gatewayCli.GetLeaderboard(botCtx, &pb.GetLeaderboardRequest{
				Type: "power",
			})
			if ctx.Err() != nil {
				return
			}
			stats.Add(time.Since(start), err == nil)
			if err != nil && atomic.AddUint32(&errCount, 1) <= 10 {
				fmt.Printf("[Lỗi Gateway Bot %d] GetLeaderboard failed: %v\n", botId, err)
			}
			
			// Sleep before next actions
			time.Sleep(time.Duration(3000+rand.Intn(3000)) * time.Millisecond)
		}
	}
}
