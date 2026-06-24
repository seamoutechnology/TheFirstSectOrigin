package docker

import (
	"context"
	"fmt"
	"net/netip"
	"os"
	"strings"

	"github.com/moby/moby/api/types/container"
	"github.com/moby/moby/api/types/network"
	"github.com/moby/moby/client"
)

// SpawnZoneContainers dynamically spawns the gateway, world, and combat server containers for a given zone.
func SpawnZoneContainers(ctx context.Context, zoneID int) (string, error) {
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		return "", fmt.Errorf("failed to create docker client: %w", err)
	}
	defer cli.Close()

	// 1. Get metadata from self container to know which image and network we should use
	imageName, networkName, err := GetSelfMetadata(ctx, cli)
	if err != nil {
		return "", fmt.Errorf("failed to detect self metadata: %w", err)
	}

	projectName := os.Getenv("COMPOSE_PROJECT_NAME")
	if projectName == "" {
		projectName = "thefirstsect_dev"
	}

	combatName := fmt.Sprintf("%s_combat_server_%d", projectName, zoneID)
	worldName := fmt.Sprintf("%s_world_server_%d", projectName, zoneID)
	gatewayName := fmt.Sprintf("%s_gateway_%d", projectName, zoneID)

	// Ports offsets:
	// Zone 1: 50052, 50053, 50054
	gatewayPort := 50052 + (zoneID-1)*10
	worldPort := 50053 + (zoneID-1)*10
	combatPort := 50054 + (zoneID-1)*10

	// Common environment variables
	appEnv := os.Getenv("APP_ENV")
	if appEnv == "" {
		appEnv = "development"
	}
	redisHost := os.Getenv("REDIS_HOST")
	if redisHost == "" {
		redisHost = "redis"
	}
	redisPort := os.Getenv("REDIS_PORT")
	if redisPort == "" {
		redisPort = "6379"
	}
	redisPassword := os.Getenv("REDIS_PASSWORD")
	jwtSecret := os.Getenv("JWT_SECRET")
	if jwtSecret == "" {
		jwtSecret = "dev_secret_key_not_for_production"
	}
	dbHost := os.Getenv("GAME_DB_HOST")
	if dbHost == "" {
		dbHost = "postgres_game"
	}
	dbPort := os.Getenv("GAME_DB_PORT")
	if dbPort == "" {
		dbPort = "5432"
	}
	dbUser := os.Getenv("GAME_DB_USER")
	if dbUser == "" {
		dbUser = "mmo_game"
	}
	dbPassword := os.Getenv("GAME_DB_PASSWORD")
	if dbPassword == "" {
		dbPassword = "dev_password"
	}
	dbName := os.Getenv("GAME_DB_NAME")
	if dbName == "" {
		dbName = "mmo_game_zone1"
	}

	// ==========================================
	// 1. Spawn Combat Server
	// ==========================================
	combatEnv := []string{
		fmt.Sprintf("APP_ENV=%s", appEnv),
		fmt.Sprintf("REDIS_HOST=%s", redisHost),
		fmt.Sprintf("REDIS_PORT=%s", redisPort),
		fmt.Sprintf("REDIS_PASSWORD=%s", redisPassword),
		fmt.Sprintf("COMBAT_SERVER_PORT=%d", combatPort),
	}
	combatPorts := network.PortMap{
		network.MustParsePort(fmt.Sprintf("%d/tcp", combatPort)): []network.PortBinding{
			{HostIP: netip.MustParseAddr("0.0.0.0"), HostPort: fmt.Sprintf("%d", combatPort)},
		},
	}
	_, err = spawnContainer(ctx, cli, combatName, imageName, networkName, []string{"./bin/combat-server"}, combatEnv, combatPorts)
	if err != nil {
		return "", fmt.Errorf("failed to spawn combat server %d: %w", zoneID, err)
	}

	// ==========================================
	// 2. Spawn World Server
	// ==========================================
	worldEnv := []string{
		fmt.Sprintf("APP_ENV=%s", appEnv),
		fmt.Sprintf("GAME_DB_HOST=%s", dbHost),
		fmt.Sprintf("GAME_DB_PORT=%s", dbPort),
		fmt.Sprintf("GAME_DB_USER=%s", dbUser),
		fmt.Sprintf("GAME_DB_PASSWORD=%s", dbPassword),
		fmt.Sprintf("GAME_DB_NAME=%s", dbName),
		fmt.Sprintf("REDIS_HOST=%s", redisHost),
		fmt.Sprintf("REDIS_PORT=%s", redisPort),
		fmt.Sprintf("REDIS_PASSWORD=%s", redisPassword),
		fmt.Sprintf("SERVER_ID=zone%d", zoneID),
		fmt.Sprintf("SERVER_PORT=%d", worldPort),
		fmt.Sprintf("COMBAT_SERVER_HOST=%s", combatName), // Connects via container network name
		fmt.Sprintf("COMBAT_SERVER_PORT=%d", combatPort),
	}
	worldPorts := network.PortMap{
		network.MustParsePort(fmt.Sprintf("%d/tcp", worldPort)): []network.PortBinding{
			{HostIP: netip.MustParseAddr("0.0.0.0"), HostPort: fmt.Sprintf("%d", worldPort)},
		},
	}
	_, err = spawnContainer(ctx, cli, worldName, imageName, networkName, []string{"./bin/world-server"}, worldEnv, worldPorts)
	if err != nil {
		return "", fmt.Errorf("failed to spawn world server %d: %w", zoneID, err)
	}

	// ==========================================
	// 3. Spawn Gateway
	// ==========================================
	gatewayEnv := []string{
		fmt.Sprintf("APP_ENV=%s", appEnv),
		fmt.Sprintf("REDIS_HOST=%s", redisHost),
		fmt.Sprintf("REDIS_PORT=%s", redisPort),
		fmt.Sprintf("REDIS_PASSWORD=%s", redisPassword),
		fmt.Sprintf("JWT_SECRET=%s", jwtSecret),
		fmt.Sprintf("SERVER_PORT=%d", gatewayPort),
		fmt.Sprintf("WORLD_SERVER_HOST=%s", worldName), // Connects via container network name
		fmt.Sprintf("WORLD_SERVER_PORT=%d", worldPort),
		fmt.Sprintf("COMBAT_SERVER_HOST=%s", combatName), // Connects via container network name
		fmt.Sprintf("COMBAT_SERVER_PORT=%d", combatPort),
		"RATE_LIMIT_ENABLED=false",
	}
	gatewayPorts := network.PortMap{
		network.MustParsePort(fmt.Sprintf("%d/tcp", gatewayPort)): []network.PortBinding{
			{HostIP: netip.MustParseAddr("0.0.0.0"), HostPort: fmt.Sprintf("%d", gatewayPort)},
		},
	}
	_, err = spawnContainer(ctx, cli, gatewayName, imageName, networkName, []string{"./bin/gateway"}, gatewayEnv, gatewayPorts)
	if err != nil {
		return "", fmt.Errorf("failed to spawn gateway %d: %w", zoneID, err)
	}

	return fmt.Sprintf("localhost:%d", gatewayPort), nil
}

// GetSelfMetadata retrieves the current container's image name and network name.
func GetSelfMetadata(ctx context.Context, cli *client.Client) (imageName string, networkName string, err error) {
	hn, err := os.Hostname()
	if err != nil {
		return "", "", fmt.Errorf("failed to get hostname: %w", err)
	}

	inspect, err := cli.ContainerInspect(ctx, hn, client.ContainerInspectOptions{})
	if err != nil {
		// Fallback when running outside Docker container (local machine dev)
		networks, netErr := cli.NetworkList(ctx, client.NetworkListOptions{})
		if netErr == nil {
			for _, net := range networks.Items {
				if strings.Contains(net.Name, "thefirstsect") || strings.Contains(net.Name, "tfso") {
					networkName = net.Name
					break
				}
			}
		}
		if networkName == "" {
			networkName = "thefirstsect_dev_default"
		}

		images, imgErr := cli.ImageList(ctx, client.ImageListOptions{})
		if imgErr == nil {
			for _, img := range images.Items {
				for _, tag := range img.RepoTags {
					if strings.Contains(tag, "login-server") || strings.Contains(tag, "server") {
						imageName = tag
						break
					}
				}
				if imageName != "" {
					break
				}
			}
		}
		if imageName == "" {
			imageName = "thefirstsect_dev-login_server"
		}
		return imageName, networkName, nil
	}

	imageName = inspect.Container.Config.Image
	for netName := range inspect.Container.NetworkSettings.Networks {
		networkName = netName
		break
	}

	if networkName == "" {
		networkName = "bridge"
	}

	return imageName, networkName, nil
}

func spawnContainer(
	ctx context.Context,
	cli *client.Client,
	containerName string,
	imageName string,
	networkName string,
	cmd []string,
	env []string,
	portBindings network.PortMap,
) (string, error) {
	// 1. Remove existing container if it exists
	existing, err := cli.ContainerInspect(ctx, containerName, client.ContainerInspectOptions{})
	if err == nil {
		if existing.Container.State.Running {
			timeout := 10
			stopOpts := client.ContainerStopOptions{Timeout: &timeout}
			_, _ = cli.ContainerStop(ctx, existing.Container.ID, stopOpts)
		}
		_, _ = cli.ContainerRemove(ctx, existing.Container.ID, client.ContainerRemoveOptions{Force: true})
	}

	// 2. Create container config
	config := &container.Config{
		Image: imageName,
		Cmd:   cmd,
		Env:   env,
	}

	// 3. Host config with restart policy and port mapping
	hostConfig := &container.HostConfig{
		PortBindings: portBindings,
		RestartPolicy: container.RestartPolicy{
			Name: container.RestartPolicyUnlessStopped,
		},
	}

	// 4. Network config
	networkingConfig := &network.NetworkingConfig{
		EndpointsConfig: map[string]*network.EndpointSettings{
			networkName: {},
		},
	}

	// 5. Create container using the new struct-based API
	resp, err := cli.ContainerCreate(ctx, client.ContainerCreateOptions{
		Config:           config,
		HostConfig:       hostConfig,
		NetworkingConfig: networkingConfig,
		Name:             containerName,
	})
	if err != nil {
		return "", fmt.Errorf("failed to create container %s: %w", containerName, err)
	}

	// 6. Connect container to network explicitly if endpoints config wasn't enough (robust fallback)
	_, _ = cli.NetworkConnect(ctx, networkName, client.NetworkConnectOptions{
		Container:      resp.ID,
		EndpointConfig: &network.EndpointSettings{},
	})

	// 7. Start container using the new API
	_, err = cli.ContainerStart(ctx, resp.ID, client.ContainerStartOptions{})
	if err != nil {
		return "", fmt.Errorf("failed to start container %s: %w", containerName, err)
	}

	return resp.ID, nil
}
