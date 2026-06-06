package main

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strings"
)

func main() {
	rootDirs := []string{
		`c:\Project\TheFirstSectOrigin\server`,
		`c:\Project\TheFirstSectOrigin\client\Assets\Scripts`,
	}
	
	excludeDirs := []string{"Plugins", "AssetKits", "vendor", ".git", ".idea", "Library", "Logs", "Packages", "ProjectSettings"}
	
	count := 0
	
	for _, rootDir := range rootDirs {
		filepath.Walk(rootDir, func(path string, info os.FileInfo, err error) error {
			if err != nil {
				return nil
			}
			
			if info.IsDir() {
				for _, ex := range excludeDirs {
					if info.Name() == ex {
						return filepath.SkipDir
					}
				}
				return nil
			}
			
			ext := filepath.Ext(path)
			if ext == ".go" || ext == ".cs" {
				if processFile(path) {
					count++
					fmt.Println("Deleted comments in:", path)
				}
			}
			
			return nil
		})
	}
	
	fmt.Printf("Total files updated: %d\n", count)
}

func processFile(path string) bool {
	content, err := os.ReadFile(path)
	if err != nil {
		return false
	}
	
	lines := strings.Split(string(content), "\n")
	var newLines []string
	changed := false
	
	inSummaryBlock := false
	
	reComment := regexp.MustCompile(`^\s*//(.*)$`)
	
	for _, line := range lines {
		cleanLine := strings.TrimSuffix(line, "\r")
		trimmed := strings.TrimSpace(cleanLine)
		
		if strings.HasPrefix(trimmed, "/// <summary>") {
			inSummaryBlock = true
			changed = true
			continue
		}
		
		if inSummaryBlock {
			if strings.HasPrefix(trimmed, "/// </summary>") {
				inSummaryBlock = false
			}
			continue
		}
		
		if strings.HasPrefix(trimmed, "///") {
			changed = true
			continue
		}
		
		if matches := reComment.FindStringSubmatch(cleanLine); len(matches) > 0 {
			commentText := matches[1]
			
			if strings.Contains(commentText, "TODO") || strings.Contains(commentText, "FIXME") || strings.Contains(commentText, "http") || strings.Contains(commentText, "https") {
				newLines = append(newLines, line)
				continue
			}
			
			changed = true
			continue
		}
		
		newLines = append(newLines, line)
	}
	
	if changed {
		os.WriteFile(path, []byte(strings.Join(newLines, "\n")), infoToFileMode(path))
		return true
	}
	return false
}

func infoToFileMode(path string) os.FileMode {
	info, _ := os.Stat(path)
	if info != nil {
		return info.Mode()
	}
	return 0644
}
