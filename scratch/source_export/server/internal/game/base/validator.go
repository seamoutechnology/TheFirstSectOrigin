package base

import "fmt"

func ValidatePlacement(layout BaseLayout) error {
	if layout.GridWidth <= 0 || layout.GridHeight <= 0 {
		return fmt.Errorf("invalid grid size: %dx%d", layout.GridWidth, layout.GridHeight)
	}

	grid := make([][]bool, layout.GridWidth)
	for i := range grid {
		grid[i] = make([]bool, layout.GridHeight)
	}

	for _, b := range layout.Buildings {
		cfg, exists := Database[b.ID]
		if !exists {
			return fmt.Errorf("building ID not found in database: %s", b.ID)
		}

		if b.X < 0 || b.Y < 0 || b.X+cfg.SizeX > layout.GridWidth || b.Y+cfg.SizeY > layout.GridHeight {
			return fmt.Errorf("building %s at (%d, %d) is out of bounds", b.ID, b.X, b.Y)
		}

		for x := b.X; x < b.X+cfg.SizeX; x++ {
			for y := b.Y; y < b.Y+cfg.SizeY; y++ {
				if grid[x][y] {
					return fmt.Errorf("collision detected at (%d, %d) by building %s", x, y, b.ID)
				}
				grid[x][y] = true
			}
		}
	}

	return nil
}
