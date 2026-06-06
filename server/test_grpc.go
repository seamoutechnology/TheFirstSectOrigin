package main

import (
	"context"
	"fmt"
	"log"
	"time"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	pb "server/pkg/pb"
)

func main() {
	fmt.Println("Dialing 127.0.0.1:50051...")
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	conn, err := grpc.DialContext(ctx, "127.0.0.1:50051", 
		grpc.WithTransportCredentials(insecure.NewCredentials()),
		grpc.WithBlock(),
	)
	if err != nil {
		log.Fatalf("Failed to connect to gRPC server: %v", err)
	}
	defer conn.Close()

	fmt.Println("Connected! Sending login request...")
	client := pb.NewAuthServiceClient(conn)

	resp, err := client.Login(context.Background(), &pb.LoginRequest{
		Username: "test123",
		Password: "wrongpassword",
	})
	if err != nil {
		log.Fatalf("Login failed with error: %v", err)
	}

	fmt.Printf("Response: Code=%d, Message=%s\n", resp.Code, resp.MessageId)
}
