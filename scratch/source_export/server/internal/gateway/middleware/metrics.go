package middleware

import (
	"context"
	"google.golang.org/grpc"
	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promauto"
	"time"
)

var (
	grpcRequestsTotal = promauto.NewCounterVec(
		prometheus.CounterOpts{
			Name: "grpc_requests_total",
			Help: "Total number of gRPC requests handled",
		},
		[]string{"method"},
	)
	grpcRequestDuration = promauto.NewHistogramVec(
		prometheus.HistogramOpts{
			Name:    "grpc_request_duration_seconds",
			Help:    "Duration of gRPC requests in seconds",
			Buckets: prometheus.DefBuckets,
		},
		[]string{"method"},
	)
)

func MetricsInterceptor() grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req interface{}, info *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (interface{}, error) {
		start := time.Now()
		
		resp, err := handler(ctx, req)
		
		duration := time.Since(start).Seconds()
		grpcRequestsTotal.WithLabelValues(info.FullMethod).Inc()
		grpcRequestDuration.WithLabelValues(info.FullMethod).Observe(duration)
		
		return resp, err
	}
}
