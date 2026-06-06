package profanity

import (
	"regexp"
	"strings"
)

var (
	badWords = []string{"fuck", "shit", "bitch", "đụ", "địt", "đm", "vcl", "loz"}
	leetspeakMap = map[rune]string{
		'a': "[a@4^]", 'e': "[e3]", 'i': "[i1!|]", 'o': "[o0]", 'u': "[uµ]",
		'c': "[c(k]", 's': "[s5$]", 'đ': "[đd]",
	}
	compiledRegexes []*regexp.Regexp
)

func init() {
	for _, word := range badWords {
		var pattern strings.Builder
		for _, char := range word {
			if replacement, ok := leetspeakMap[char]; ok {
				pattern.WriteString(replacement)
				pattern.WriteString(`\W*`)
			} else {
				pattern.WriteString(regexp.QuoteMeta(string(char)))
				pattern.WriteString(`\W*`)
			}
		}
		re := regexp.MustCompile(`(?i)` + pattern.String())
		compiledRegexes = append(compiledRegexes, re)
	}
}

func Contains(input string) bool {
	for _, re := range compiledRegexes {
		if re.MatchString(input) {
			return true
		}
	}
	return false
}

func Filter(input string) string {
	output := input
	for _, re := range compiledRegexes {
		output = re.ReplaceAllStringFunc(output, func(match string) string {
			return strings.Repeat("*", len([]rune(match)))
		})
	}
	return output
}
