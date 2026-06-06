package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

func main() {
	rootDirs := []string{
		`c:\Project\TheFirstSectOrigin\server`,
		`c:\Project\TheFirstSectOrigin\client\Assets\Scripts`,
	}

	for _, rootDir := range rootDirs {
		filepath.Walk(rootDir, func(path string, info os.FileInfo, err error) error {
			if err != nil || info.IsDir() {
				return nil
			}

			ext := filepath.Ext(path)
			if ext == ".go" || ext == ".cs" {
				content, err := os.ReadFile(path)
				if err != nil {
					return nil
				}

				text := string(content)
				openBraces := strings.Count(text, "{")
				closeBraces := strings.Count(text, "}")

				if openBraces != closeBraces {
					fmt.Printf("Unbalanced braces: %s (open: %d, close: %d)\n", path, openBraces, closeBraces)
				}
			}
			return nil
		})
	}
}
